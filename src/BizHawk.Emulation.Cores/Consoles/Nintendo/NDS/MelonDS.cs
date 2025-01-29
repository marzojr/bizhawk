using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.5", "https://melonds.kuribo64.net/")]
	[ServiceNotApplicable(typeof(IRegionable))]
	public sealed partial class NDS : WaterboxCore
	{
		private readonly LibMelonDS _core;
		private readonly IntPtr _console;
		private readonly NDSDisassembler _disassembler;

		private readonly LibMelonDS.LogCallback _logCallback;

		private static void LogCallback(LibMelonDS.LogLevel level, string message)
			=> Console.Write($"[{level}] {message}");

		private readonly MelonDSGLTextureProvider _glTextureProvider;
		private readonly IOpenGLProvider _openGLProvider;
		private readonly LibMelonDS.GetGLProcAddressCallback _getGLProcAddressCallback;
		private object _glContext;

		private IntPtr GetGLProcAddressCallback(string proc)
			=> _openGLProvider.GetGLProcAddress(proc);

		// TODO: Probably can make these into an interface (ITouchScreen with UntransformPoint/TransformPoint methods?)
		// Which case the hackiness of the current screen controls wouldn't be as bad
		public Vector2 GetTouchCoords(int x, int y)
		{
			if (_glContext != null)
			{
				_core.GetTouchCoords(ref x, ref y);
			}
			else
			{
				// no GL context, so nothing fancy can be applied
				y -= 192;
			}

			return new(x, y);
		}

		public Vector2 GetScreenCoords(float x, float y)
		{
			if (_glContext != null)
			{
				_core.GetScreenCoords(ref x, ref y);
			}
			else
			{
				// no GL context, so nothing fancy can be applied
				y += 192;
			}

			return new(x, y);
		}

		[CoreConstructor(VSystemID.Raw.NDS)]
		public NDS(CoreLoadParameters<NDSSettings, NDSSyncSettings> lp)
			: base(lp.Comm, new()
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256 * 3 + (128 * 4 / 3) + 1,
				MaxHeight = (384 / 2) * 2 + 128,
				MaxSamples = 4096, // rather large max samples is intentional, see comment in ThreadStartCallback
				DefaultFpsNumerator = 33513982,
				DefaultFpsDenominator = 560190,
				SystemId = VSystemID.Raw.NDS,
			})
		{
			try
			{
				_syncSettings = lp.SyncSettings ?? new();
				_settings = lp.Settings ?? new();

				_activeSyncSettings = _syncSettings.Clone();

				IsDSi = _activeSyncSettings.UseDSi;

				var roms = lp.Roms.Select(r => r.FileData).ToList();

				DSiTitleId = GetDSiTitleId(roms[0]);
				IsDSi |= IsDSiWare;

				if (roms.Count > (IsDSi ? 1 : 2))
				{
					throw new InvalidOperationException("Wrong number of ROMs!");
				}

				InitMemoryCallbacks();

				_traceCallback = MakeTrace;
				_threadStartCallback = ThreadStartCallback;

				_logCallback = LogCallback;

				_openGLProvider = CoreComm.OpenGLProvider;
				_getGLProcAddressCallback = GetGLProcAddressCallback;

				if (lp.DeterministicEmulationRequested)
				{
					_activeSyncSettings.ThreeDeeRenderer = NDSSyncSettings.ThreeDeeRendererType.Software;
				}

				if (_activeSyncSettings.ThreeDeeRenderer != NDSSyncSettings.ThreeDeeRendererType.Software)
				{
					// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
					var (majorGlVersion, minorGlVersion) = _activeSyncSettings.ThreeDeeRenderer switch
					{
						NDSSyncSettings.ThreeDeeRendererType.OpenGL_Classic => (3, 2),
						NDSSyncSettings.ThreeDeeRendererType.OpenGL_Compute => (4, 3),
						_ => throw new InvalidOperationException($"Invalid {nameof(NDSSyncSettings.ThreeDeeRenderer)}")
					};

					if (!_openGLProvider.SupportsGLVersion(majorGlVersion, minorGlVersion))
					{
						lp.Comm.Notify($"OpenGL {majorGlVersion}.{minorGlVersion} is not supported on this machine, falling back to software renderer", null);
						_activeSyncSettings.ThreeDeeRenderer = NDSSyncSettings.ThreeDeeRendererType.Software;
					}
					else
					{
						_glContext = _openGLProvider.RequestGLContext(majorGlVersion, minorGlVersion, true);
						// reallocate video buffer for scaling
						if (_activeSyncSettings.GLScaleFactor > 1)
						{
							var maxWidth = (256 * _activeSyncSettings.GLScaleFactor) * 3 + ((128 * _activeSyncSettings.GLScaleFactor) * 4 / 3) + 1;
							var maxHeight = (384 / 2 * _activeSyncSettings.GLScaleFactor) * 2 + (128 * _activeSyncSettings.GLScaleFactor);
							_videoBuffer = new int[maxWidth * maxHeight];
						}
					}
				}

				if (_activeSyncSettings.ThreeDeeRenderer == NDSSyncSettings.ThreeDeeRendererType.Software)
				{
					if (!_openGLProvider.SupportsGLVersion(3, 1))
					{
						lp.Comm.Notify("OpenGL 3.1 is not supported on this machine, screen control options will not work.", null);
					}
					else
					{
						_glContext = _openGLProvider.RequestGLContext(3, 1, true);
					}
				}

				_core = PreInit<LibMelonDS>(new()
				{
					Filename = "melonDS.wbx",
					SbrkHeapSizeKB = 2 * 1024,
					SealedHeapSizeKB = 4,
					InvisibleHeapSizeKB = 48 * 1024,
					PlainHeapSizeKB = 4,
					MmapHeapSizeKB = 1024 * 1024,
					SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
					SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
				}, new Delegate[]
				{
					_readCallback, _writeCallback, _execCallback, _traceCallback,
					_threadStartCallback, _logCallback, _getGLProcAddressCallback
				});

				_core.SetLogCallback(_logCallback);

				if (_glContext != null)
				{
					var error = _core.InitGL(_getGLProcAddressCallback, _activeSyncSettings.ThreeDeeRenderer, _activeSyncSettings.GLScaleFactor, !OSTailoredCode.IsUnixHost);
					if (error != IntPtr.Zero)
					{
						using (_exe.EnterExit())
						{
							throw new InvalidOperationException(Marshal.PtrToStringAnsi(error));
						}
					}
				}

				_activeSyncSettings.UseRealBIOS |= IsDSi;

				if (!_activeSyncSettings.UseRealBIOS)
				{
					var arm9RomOffset = BinaryPrimitives.ReadInt32LittleEndian(roms[0].AsSpan(0x20, 4));
					if (arm9RomOffset is >= 0x4000 and < 0x8000)
					{
						// check if the user is using an encrypted rom
						// if they are, they need to be using real bios files
						var secureAreaId = BinaryPrimitives.ReadUInt64LittleEndian(roms[0].AsSpan(arm9RomOffset, 8));
						_activeSyncSettings.UseRealBIOS = secureAreaId != 0xE7FFDEFF_E7FFDEFF;
					}
				}

				byte[] bios9 = null, bios7 = null, firmware = null;
				if (_activeSyncSettings.UseRealBIOS)
				{
					bios9 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"));
					bios7 = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"));
					firmware = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", IsDSi ? "firmwarei" : "firmware"));

					if (firmware.Length is not (0x20000 or 0x40000 or 0x80000))
					{
						throw new InvalidOperationException("Invalid firmware length");
					}

					NDSFirmware.MaybeWarnIfBadFw(firmware, CoreComm.ShowMessage);
				}

				byte[] bios9i = null, bios7i = null, nand = null;
				if (IsDSi)
				{
					bios9i = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9i"));
					bios7i = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7i"));
					nand = DecideNAND(CoreComm.CoreFileProvider, (DSiTitleId.Upper & ~0xFF) == 0x00030000, roms[0][0x1B0]);
				}

				byte[] ndsRom = null, gbaRom = null, dsiWare = null, tmd = null;
				if (IsDSiWare)
				{
					tmd = GetTMDData(DSiTitleId.Full);
					dsiWare = roms[0];
				}
				else
				{
					ndsRom = roms[0];
					if (roms.Count == 2)
					{
						gbaRom = roms[1];
					}
				}

				_activeSyncSettings.FirmwareOverride |= !_activeSyncSettings.UseRealBIOS || lp.DeterministicEmulationRequested;

				// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
				if (!IsDSi && _activeSyncSettings.FirmwareStartUp == NDSSyncSettings.StartUp.AutoBoot)
				{
					_activeSyncSettings.FirmwareLanguage |= (NDSSyncSettings.Language)0x40;
				}

				_activeSyncSettings.UseRealTime &= !lp.DeterministicEmulationRequested;
				var startTime = _activeSyncSettings.UseRealTime ? DateTime.Now : _activeSyncSettings.InitialTime;

				LibMelonDS.ConsoleCreationArgs consoleCreationArgs;

				consoleCreationArgs.NdsRomLength = ndsRom?.Length ?? 0;
				consoleCreationArgs.GbaRomLength = gbaRom?.Length ?? 0;
				consoleCreationArgs.Arm9BiosLength = bios9?.Length ?? 0;
				consoleCreationArgs.Arm7BiosLength = bios7?.Length ?? 0;
				consoleCreationArgs.FirmwareLength = firmware?.Length ?? 0;
				consoleCreationArgs.Arm9iBiosLength = bios9i?.Length ?? 0;
				consoleCreationArgs.Arm7iBiosLength = bios7i?.Length ?? 0;
				consoleCreationArgs.NandLength = nand?.Length ?? 0;
				consoleCreationArgs.DsiWareLength = dsiWare?.Length ?? 0;
				consoleCreationArgs.TmdLength = tmd?.Length ?? 0;

				consoleCreationArgs.DSi = IsDSi;
				consoleCreationArgs.ClearNAND = _activeSyncSettings.ClearNAND || lp.DeterministicEmulationRequested;
				consoleCreationArgs.SkipFW = _activeSyncSettings.SkipFirmware;

				consoleCreationArgs.BitDepth = _settings.AudioBitDepth;
				consoleCreationArgs.Interpolation = _settings.AudioInterpolation;

				consoleCreationArgs.ThreeDeeRenderer = _activeSyncSettings.ThreeDeeRenderer;
				consoleCreationArgs.Threaded3D = _activeSyncSettings.ThreadedRendering;
				consoleCreationArgs.ScaleFactor = _activeSyncSettings.GLScaleFactor;
				consoleCreationArgs.BetterPolygons = _activeSyncSettings.GLBetterPolygons;
				consoleCreationArgs.HiResCoordinates = _activeSyncSettings.GLHiResCoordinates;

				consoleCreationArgs.StartYear = startTime.Year % 100;
				consoleCreationArgs.StartMonth = startTime.Month;
				consoleCreationArgs.StartDay = startTime.Day;
				consoleCreationArgs.StartHour = startTime.Hour;
				consoleCreationArgs.StartMinute = startTime.Minute;
				consoleCreationArgs.StartSecond = startTime.Second;

				_activeSyncSettings.GetFirmwareSettings(out consoleCreationArgs.FwSettings);

				var errorBuffer = new byte[1024];
				unsafe
				{
					fixed (byte*
						ndsRomPtr = ndsRom,
						gbaRomPtr = gbaRom,
						bios9Ptr = bios9,
						bios7Ptr = bios7,
						firmwarePtr = firmware,
						bios9iPtr = bios9i,
						bios7iPtr = bios7i,
						nandPtr = nand,
						dsiWarePtr = dsiWare,
						tmdPtr = tmd)
					{
						consoleCreationArgs.NdsRomData = (IntPtr)ndsRomPtr;
						consoleCreationArgs.GbaRomData = (IntPtr)gbaRomPtr;
						consoleCreationArgs.Arm9BiosData = (IntPtr)bios9Ptr;
						consoleCreationArgs.Arm7BiosData = (IntPtr)bios7Ptr;
						consoleCreationArgs.FirmwareData = (IntPtr)firmwarePtr;
						consoleCreationArgs.Arm9iBiosData = (IntPtr)bios9iPtr;
						consoleCreationArgs.Arm7iBiosData = (IntPtr)bios7iPtr;
						consoleCreationArgs.NandData = (IntPtr)nandPtr;
						consoleCreationArgs.DsiWareData = (IntPtr)dsiWarePtr;
						consoleCreationArgs.TmdData = (IntPtr)tmdPtr;
						_console = _core.CreateConsole(ref consoleCreationArgs, errorBuffer);
					}
				}

				if (_console == IntPtr.Zero)
				{
					var errorStr = Encoding.ASCII.GetString(errorBuffer).TrimEnd('\0');
					throw new InvalidOperationException(errorStr);
				}

				if (IsDSiWare)
				{
					_core.DSiWareSavsLength(_console, DSiTitleId.Lower, out PublicSavSize, out PrivateSavSize, out BannerSavSize);
					DSiWareSaveLength = PublicSavSize + PrivateSavSize + BannerSavSize;
				}

				PostInit();

				((MemoryDomainList)this.AsMemoryDomains()).SystemBus = new NDSSystemBus(this.AsMemoryDomains()["ARM9 System Bus"], this.AsMemoryDomains()["ARM7 System Bus"]);

				DeterministicEmulation = lp.DeterministicEmulationRequested || !_activeSyncSettings.UseRealTime;

				_frameThreadPtr = _core.GetFrameThreadProc();
				if (_frameThreadPtr != IntPtr.Zero)
				{
					Console.WriteLine($"Setting up waterbox thread for 0x{(ulong)_frameThreadPtr:X16}");
					_frameThread = new(FrameThreadProc) { IsBackground = true };
					_frameThread.Start();
					_frameThreadAction = CallingConventionAdapters
						.GetWaterboxUnsafeUnwrapped()
						.GetDelegateForFunctionPointer<Action>(_frameThreadPtr);
					_core.SetThreadStartCallback(_threadStartCallback);
				}

				_disassembler = new(_core);
				_serviceProvider.Register<IDisassemblable>(_disassembler);

				const string TRACE_HEADER = "ARM9+ARM7: Opcode address, opcode, registers (r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, SP, LR, PC, Cy, CpuMode)";
				Tracer = new TraceBuffer(TRACE_HEADER);
				_serviceProvider.Register(Tracer);

				if (_glContext != null)
				{
					_glTextureProvider = new(this, _core, () => _openGLProvider.ActivateGLContext(_glContext));
					_serviceProvider.Register<IVideoProvider>(_glTextureProvider);
					RefreshScreenSettings(_settings);
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private static (ulong Full, uint Upper, uint Lower) GetDSiTitleId(IReadOnlyList<byte> file)
		{
			ulong titleId = 0;
			for (var i = 0; i < 8; i++)
			{
				titleId <<= 8;
				titleId |= file[0x237 - i];
			}
			return (titleId, (uint)(titleId >> 32), (uint)(titleId & 0xFFFFFFFFU));
		}

		private static byte[] DecideNAND(ICoreFileProvider cfp, bool isDSiEnhanced, byte regionFlags)
		{
			// TODO: priority settings?
			var nandOptions = new List<string> { "JPN", "USA", "EUR", "AUS", "CHN", "KOR" };
			if (isDSiEnhanced) // NB: Core makes cartridges region free regardless, DSiWare must follow DSi region locking however (we'll enforce it regardless)
			{
				nandOptions.Clear();
				if (regionFlags.Bit(0)) nandOptions.Add("JPN");
				if (regionFlags.Bit(1)) nandOptions.Add("USA");
				if (regionFlags.Bit(2)) nandOptions.Add("EUR");
				if (regionFlags.Bit(3)) nandOptions.Add("AUS");
				if (regionFlags.Bit(4)) nandOptions.Add("CHN");
				if (regionFlags.Bit(5)) nandOptions.Add("KOR");
			}

			foreach (var option in nandOptions)
			{
				var ret = cfp.GetFirmware(new("NDS", $"NAND_{option}"));
				if (ret is not null) return ret;
			}

			throw new MissingFirmwareException("Suitable NAND file not found!");
		}

		private static byte[] GetTMDData(ulong titleId)
		{
			using var zip = new ZipArchive(Zstd.DecompressZstdStream(new MemoryStream(Resources.TMDS.Value)), ZipArchiveMode.Read, false);
			using var tmd = zip.GetEntry($"{titleId:x16}.tmd")?.Open() ?? throw new Exception($"Cannot find TMD for title ID {titleId:x16}, please report");
			return tmd.ReadAllBytes();
		}

		// todo: wire this up w/ frontend
		public byte[] GetNAND()
		{
			var length = _core.GetNANDSize(_console);

			if (length > 0)
			{
				var ret = new byte[length];
				_core.GetNANDData(_console, ret);
				return ret;
			}

			return null;
		}

		public bool IsDSi { get; }

		public bool IsDSiWare => DSiTitleId.Upper == 0x00030004;

		private (ulong Full, uint Upper, uint Lower) DSiTitleId { get; }

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition("NDS Controller")
		{
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Microphone", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("Mic Volume", 0.RangeTo(100), 100)
			.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0)
			.MakeImmutable();

		private static LibMelonDS.Buttons GetButtons(IController c)
		{
			LibMelonDS.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibMelonDS.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibMelonDS.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibMelonDS.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibMelonDS.Buttons.RIGHT;
			if (c.IsPressed("Start"))
				b |= LibMelonDS.Buttons.START;
			if (c.IsPressed("Select"))
				b |= LibMelonDS.Buttons.SELECT;
			if (c.IsPressed("B"))
				b |= LibMelonDS.Buttons.B;
			if (c.IsPressed("A"))
				b |= LibMelonDS.Buttons.A;
			if (c.IsPressed("Y"))
				b |= LibMelonDS.Buttons.Y;
			if (c.IsPressed("X"))
				b |= LibMelonDS.Buttons.X;
			if (c.IsPressed("L"))
				b |= LibMelonDS.Buttons.L;
			if (c.IsPressed("R"))
				b |= LibMelonDS.Buttons.R;
			if (c.IsPressed("LidOpen"))
				b |= LibMelonDS.Buttons.LIDOPEN;
			if (c.IsPressed("LidClose"))
				b |= LibMelonDS.Buttons.LIDCLOSE;
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;

			return b;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (_glContext != null)
			{
				_openGLProvider.ActivateGLContext(_glContext);
			}

			_core.SetTraceCallback(Tracer.IsEnabled() ? _traceCallback : null, _settings.GetTraceMask());

			if (controller.IsPressed("Power"))
			{
				_core.ResetConsole(_console, _activeSyncSettings.SkipFirmware, DSiTitleId.Full);
			}

			return new LibMelonDS.FrameInfo
			{
				Console = _console,
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("Touch X"),
				TouchY = (byte)controller.AxisValue("Touch Y"),
				MicVolume = (byte)(controller.IsPressed("Microphone") ? controller.AxisValue("Mic Volume") : 0),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
				ConsiderAltLag = (byte)(_settings.ConsiderAltLag ? 1 : 0),
			};
		}

		private readonly IntPtr _frameThreadPtr;
		private readonly Action _frameThreadAction;
		private readonly LibMelonDS.ThreadStartCallback _threadStartCallback;

		private readonly Thread _frameThread;
		private readonly SemaphoreSlim _frameThreadStartEvent = new(0, 1);
		private readonly SemaphoreSlim _frameThreadEndEvent = new(0, 1);
		private bool _isDisposing;
		private bool _renderThreadRanThisFrame;

		public override void Dispose()
		{
			_isDisposing = true;
			_frameThreadStartEvent.Release();

			if (_frameThread != null)
			{
				while (_frameThread.IsAlive)
				{
					Thread.Sleep(1);
				}
			}

			_frameThreadStartEvent.Dispose();
			_frameThreadEndEvent.Dispose();

			if (_glContext != null)
			{
				_openGLProvider.ReleaseContext(_glContext);
				_glContext = null;
			}

			base.Dispose();
		}

		private void FrameThreadProc()
		{
			while (true)
			{
				_frameThreadStartEvent.Wait();
				if (_isDisposing) break;
				_frameThreadAction();
				_frameThreadEndEvent.Release();
			}
		}

		private void ThreadStartCallback()
		{
			if (_renderThreadRanThisFrame)
			{
				// This is technically possible due to the game able to force another frame to be rendered by touching vcount
				// (ALSO MEANS VSYNC NUMBERS ARE KIND OF A LIE)
				_frameThreadEndEvent.Wait();
			}

			_renderThreadRanThisFrame = true;
			_frameThreadStartEvent.Release();
		}

		protected override void FrameAdvancePost()
		{
			if (_renderThreadRanThisFrame)
			{
				_frameThreadEndEvent.Wait();
				_renderThreadRanThisFrame = false;
			}

			if (_glTextureProvider != null)
			{
				_glTextureProvider.VideoDirty = true;
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetMemoryCallbacks();
			_core.SetThreadStartCallback(_threadStartCallback);
			if (_frameThreadPtr != _core.GetFrameThreadProc())
			{
				throw new InvalidOperationException("_frameThreadPtr mismatch");
			}

			_core.SetSoundConfig(_console, _settings.AudioBitDepth, _settings.AudioInterpolation);
		}

		// omega hack
		public class NDSSystemBus : MemoryDomain
		{
			private readonly MemoryDomain Arm9Bus;
			private readonly MemoryDomain Arm7Bus;
			private bool _useArm9;

			public NDSSystemBus(MemoryDomain arm9, MemoryDomain arm7)
			{
				Size = 1L << 32;
				WordSize = 4;
				EndianType = Endian.Little;
				Writable = true;

				Arm9Bus = arm9;
				Arm7Bus = arm7;

				UseArm9 = true; // important to set the initial name correctly
			}

			public bool UseArm9
			{
				get => _useArm9;
				set
				{
					_useArm9 = value;
					Name = _useArm9 ? "ARM9 System Bus" : "ARM7 System Bus";
				}
			}

			public override byte PeekByte(long addr) => UseArm9 ? Arm9Bus.PeekByte(addr) : Arm7Bus.PeekByte(addr);

			public override void PokeByte(long addr, byte val)
			{
				if (UseArm9)
				{
					Arm9Bus.PokeByte(addr, val);
				}
				else
				{
					Arm7Bus.PokeByte(addr, val);
				}
			}
		}
	}
}
