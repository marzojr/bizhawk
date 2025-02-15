using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	[PortedCore(
		name: CoreNames.Encore,
		author: "Tropic Haze and Citra contributors; port by CasualPokePlayer",
		portedVersion: "nightly-2104",
		portedUrl: "https://github.com/CasualPokePlayer/encore",
		singleInstance: true)]
	[ServiceNotApplicable(typeof(IRegionable))]
	public partial class Encore : IRomInfo
	{
		private static DynamicLibraryImportResolver _resolver;
		private static LibEncore _core;

		// This is a hack, largely just so we can forcefully evict file handles Encore keeps open even after shutdown.
		// While keeping these file handles open is mostly harmless, this ends up being bad when recording a movie.
		// These file handles would be in the user folder, and the user folder must be cleared out when recording a movie!
		private static void ResetEncoreResolver()
		{
			_resolver?.Dispose();
			_resolver = new(OSTailoredCode.IsUnixHost ? "libencore.so" : "encore.dll", hasLimitedLifetime: true);
			_core = BizInvoker.GetInvoker<LibEncore>(_resolver, CallingConventionAdapters.Native);
		}

		private static Encore CurrentCore;

		private readonly IOpenGLProvider _openGLProvider;
		private readonly bool _supportsOpenGL43;
		private readonly List<object> _glContexts = new();
		private readonly LibEncore.ConfigCallbackInterface _configCallbackInterface;
		private readonly LibEncore.GLCallbackInterface _glCallbackInterface;
		private readonly LibEncore.InputCallbackInterface _inputCallbackInterface;
		private readonly IntPtr _context;
		private readonly EncoreVideoProvider _encoreVideoProvider;

		public Rectangle TouchScreenRectangle { get; private set; }
		public bool TouchScreenRotated { get; private set; }
		public bool TouchScreenEnabled { get; private set; }

		[CoreConstructor(VSystemID.Raw.N3DS)]
		public Encore(CoreLoadParameters<EncoreSettings, EncoreSyncSettings> lp)
		{
			if (lp.Roms.Exists(static r => HawkFile.PathContainsPipe(r.RomPath)))
			{
				throw new InvalidOperationException("3DS does not support compressed ROMs");
			}

			CurrentCore?.Dispose();
			ResetEncoreResolver();

			CurrentCore = this;

			_serviceProvider = new(this);
			_settings = lp.Settings ?? new();
			_syncSettings = lp.SyncSettings ?? new();

			DeterministicEmulation = lp.DeterministicEmulationRequested;
			_userPath = lp.Comm.CoreFileProvider.GetUserPath(SystemId, temp: DeterministicEmulation && _syncSettings.TempUserFolder) + Path.DirectorySeparatorChar;
			_userPath = _userPath.Replace('\\', '/'); // Encore doesn't like backslashes in the user folder, for whatever reason

			// copy firmware over to the user folder
			// this must be done before Encore_CreateContext is called!
			var sysDataDir = Path.Combine(_userPath, "sysdata");
			if (!Directory.Exists(sysDataDir))
			{
				Directory.CreateDirectory(sysDataDir);
			}

			var aesKeys = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "aes_keys"));
			if (aesKeys is not null)
			{
				File.WriteAllBytes(Path.Combine(sysDataDir, "aes_keys.txt"), aesKeys);
			}

			var seeddb = lp.Comm.CoreFileProvider.GetFirmware(new("3DS", "seeddb"));
			if (seeddb is not null)
			{
				File.WriteAllBytes(Path.Combine(sysDataDir, "seeddb.bin"), seeddb);
			}

			_configCallbackInterface.GetBoolean = GetBooleanSettingCallback;
			_configCallbackInterface.GetInteger = GetIntegerSettingCallback;
			_configCallbackInterface.GetFloat = GetFloatSettingCallback;
			_configCallbackInterface.GetString = GetStringSettingCallback;

			_openGLProvider = lp.Comm.OpenGLProvider;
			_supportsOpenGL43 = _openGLProvider.SupportsGLVersion(4, 3);
			if (!_supportsOpenGL43/* && _syncSettings.GraphicsApi == EncoreSyncSettings.EGraphicsApi.OpenGL*/)
			{
				throw new("OpenGL 4.3 is required, but it is not supported on this machine");
				//lp.Comm.Notify("OpenGL 4.3 is not supported on this machine, falling back to software renderer", null);
			}

			_glCallbackInterface.RequestGLContext = RequestGLContextCallback;
			_glCallbackInterface.ReleaseGLContext = ReleaseGLContextCallback;
			_glCallbackInterface.ActivateGLContext = ActivateGLContextCallback;
			_glCallbackInterface.GetGLProcAddress = GetGLProcAddressCallback;

			_inputCallbackInterface.GetButton = GetButtonCallback;
			_inputCallbackInterface.GetAxis = GetAxisCallback;
			_inputCallbackInterface.GetTouch = GetTouchCallback;
			_inputCallbackInterface.GetMotion = GetMotionCallback;

			_context = _core.Encore_CreateContext(ref _configCallbackInterface, ref _glCallbackInterface, ref _inputCallbackInterface);

			if (_supportsOpenGL43/* && _syncSettings.GraphicsApi == EncoreSyncSettings.EGraphicsApi.OpenGL*/)
			{
				_encoreVideoProvider = new EncoreGLTextureProvider(_core, _context);
			}
			else
			{
				_encoreVideoProvider = new(_core, _context);
			}

			_serviceProvider.Register<IVideoProvider>(_encoreVideoProvider);

			var romPath = lp.Roms[0].RomPath;
			if (".cia".EqualsIgnoreCase(lp.Roms[0].Extension))
			{
				var message = new byte[1024];
				var res = _core.Encore_InstallCIA(_context, romPath, message, message.Length);
				var outMsg = Encoding.UTF8.GetString(message).TrimEnd('\0');
				if (res)
				{
					romPath = outMsg;
				}
				else
				{
					Dispose();
					throw new(outMsg);
				}
			}

			// user could have other CIAs after the first ROM (e.g. DLCs, updates)
			// they need to installed at once in the case of recording
			// as the temp folder is cleaned for each session
			var dummyBuffer = new byte[1];
			for (var i = 1; i < lp.Roms.Count; i++)
			{
				// doesn't make sense if not a CIA
				if (".cia".EqualsIgnoreCase(lp.Roms[i].Extension))
				{
					Dispose();
					throw new("ROMs after the index 0 should be CIAs");
				}

				_core.Encore_InstallCIA(_context, lp.Roms[i].RomPath, dummyBuffer, dummyBuffer.Length);
			}

			var errorMessage = new byte[1024];
			if (!_core.Encore_LoadROM(_context, romPath, errorMessage, errorMessage.Length))
			{
				Dispose();
				throw new($"{Encoding.UTF8.GetString(errorMessage).TrimEnd('\0')}");
			}

			InitMemoryDomains();
			// for some reason, if a savestate is created on frame 0, Encore will crash if another savestate is made after loading that state
			// advance one frame to avoid that issue
			_core.Encore_RunFrame(_context);
			OnVideoRefresh();

			var n3dsHasher = new N3DSHasher(aesKeys, seeddb);
			lp.Game.Hash = n3dsHasher.HashROM(romPath) ?? "N/A";
			var gi = Database.CheckDatabase(lp.Game.Hash);
			if (gi != null)
			{
				lp.Game.Name = gi.Name;
				lp.Game.Hash = gi.Hash;
				lp.Game.Region = gi.Region;
				lp.Game.Status = gi.Status;
				lp.Game.NotInDatabase = gi.NotInDatabase;
			}

			RomDetails = $"{lp.Game.Name}\r\n{MD5Checksum.PREFIX}:{lp.Game.Hash}";
		}

		public string RomDetails { get; }

		private IntPtr RequestGLContextCallback()
		{
			var context = _openGLProvider.RequestGLContext(4, 3, true);
			_glContexts.Add(context);
			var handle = GCHandle.Alloc(context, GCHandleType.Weak);
			return GCHandle.ToIntPtr(handle);
		}

		private void ReleaseGLContextCallback(IntPtr context)
		{
			var handle = GCHandle.FromIntPtr(context);
			_openGLProvider.ReleaseGLContext(handle.Target);
			_glContexts.Remove(handle.Target);
			handle.Free();
		}

		private void ActivateGLContextCallback(IntPtr context)
		{
			var handle = GCHandle.FromIntPtr(context);
			_openGLProvider.ActivateGLContext(handle.Target);
		}

		private IntPtr GetGLProcAddressCallback(string proc)
			=> _openGLProvider.GetGLProcAddress(proc);
	}
}
