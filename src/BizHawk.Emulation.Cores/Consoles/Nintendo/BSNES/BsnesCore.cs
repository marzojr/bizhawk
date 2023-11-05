using System;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Base_Implementations;
using BizHawk.Emulation.Cores.Components.W65816;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Waterbox;

// http://wiki.superfamicom.org/snes/show/Backgrounds

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	[PortedCore(CoreNames.Bsnes115, "bsnes team", "v115+", "https://github.com/bsnes-emu/bsnes")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class BsnesCore : IEmulator, IDebuggable, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>, IBSNESForGfxDebugger, IBoardInfo
	{
		[CoreConstructor(VSystemID.Raw.Satellaview)]
		[CoreConstructor(VSystemID.Raw.SGB)]
		[CoreConstructor(VSystemID.Raw.SNES)]
		public BsnesCore(CoreLoadParameters<SnesSettings, SnesSyncSettings> loadParameters) : this(loadParameters, false) { }
		public BsnesCore(CoreLoadParameters<SnesSettings, SnesSyncSettings> loadParameters, bool subframe = false)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			this._romPath = Path.ChangeExtension(loadParameters.Roms[0].RomPath, null);
			CoreComm = loadParameters.Comm;
			_syncSettings = loadParameters.SyncSettings ?? new SnesSyncSettings();
			SystemId = loadParameters.Game.System;
			_isSGB = SystemId == VSystemID.Raw.SGB;
			_currentMsuTrack = new ProxiedFile();

			byte[] sgbRomData = null;
			if (_isSGB)
			{
				if ((loadParameters.Roms[0].RomData[0x143] & 0xc0) == 0xc0)
				{
					throw new CGBNotSupportedException();
				}

				sgbRomData = _syncSettings.UseSGB2
					? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("SNES", "Rom_SGB2"), "SGB2 Rom is required for SGB2 emulation.")
					: CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("SNES", "Rom_SGB"), "SGB1 Rom is required for SGB1 emulation.");

				loadParameters.Game.FirmwareHash = SHA1Checksum.ComputeDigestHex(sgbRomData);
			}

			BsnesApi.SnesCallbacks callbacks = new()
			{
				inputPollCb = snes_input_poll,
				noLagCb = snes_no_lag,
				controllerLatchCb = snes_controller_latch,
				videoFrameCb = snes_video_refresh,
				pathRequestCb = snes_path_request,
				traceCb = snes_trace,
				readHookCb = ReadHook,
				writeHookCb = WriteHook,
				execHookCb = ExecHook,
				timeCb = snes_time,
				msuOpenCb = MsuOpenAudio,
				msuSeekCb = _currentMsuTrack.Seek,
				msuReadCb = _currentMsuTrack.ReadByte,
				msuEndCb = _currentMsuTrack.AtEnd
			};

			Api = new BsnesApi(CoreComm.CoreFileProvider.DllPath(), CoreComm, callbacks.AllDelegatesInMemoryOrder());

			_controllers = new BsnesControllers(_syncSettings, subframe);

			DeterministicEmulation = !_syncSettings.UseRealTime || loadParameters.DeterministicEmulationRequested;
			InitializeRtc(_syncSettings.InitialTime);

			generate_palette();
			BsnesApi.SnesInitData snesInitData = new()
			{
				entropy = _syncSettings.Entropy,
				left_port = _syncSettings.LeftPort,
				right_port = _syncSettings.RightPort,
				hotfixes = _syncSettings.Hotfixes,
				fast_ppu = _syncSettings.FastPPU,
				fast_dsp = _syncSettings.FastDSP,
				fast_coprocessors = _syncSettings.FastCoprocessors,
				region_override = _syncSettings.RegionOverride,
			};
			Api.core.snes_init(ref snesInitData);
			Api.SetCallbacks(callbacks);
			PutSettings(loadParameters.Settings ?? new SnesSettings());

			// start up audio resampler
			InitAudio();
			ser.Register<ISoundProvider>(_soundProvider);

			if (_isSGB)
			{
				Api.core.snes_load_cartridge_super_gameboy(sgbRomData, loadParameters.Roms[0].RomData,
					sgbRomData!.Length, loadParameters.Roms[0].RomData.Length);
			}
			else if (SystemId is VSystemID.Raw.Satellaview)
			{
				SATELLAVIEW_CARTRIDGE slottedCartridge = _syncSettings.SatellaviewCartridge;
				if (slottedCartridge == SATELLAVIEW_CARTRIDGE.Autodetect)
				{
					if (loadParameters.Game.NotInDatabase)
					{
						CoreComm.ShowMessage("Unable to autodetect satellaview base cartridge for unknown game. Falling back to BS-X cartridge.");
						slottedCartridge = SATELLAVIEW_CARTRIDGE.Rom_BSX;
					}
					else
					{
						// query gamedb for slotted cartridge id, assume BS-X cartridge if not otherwise specified in gamedb
						slottedCartridge = (SATELLAVIEW_CARTRIDGE)loadParameters.Game.GetInt("baseCartridge", (int)SATELLAVIEW_CARTRIDGE.Rom_BSX);
					}
				}
				byte[] cartridgeData = CoreComm.CoreFileProvider.GetFirmwareOrThrow(new FirmwareID("BSX", slottedCartridge.ToString()));
				Api.core.snes_load_cartridge_bsmemory(cartridgeData, loadParameters.Roms[0].RomData,
					cartridgeData.Length, loadParameters.Roms[0].RomData.Length);
			}
			else
			{
				Api.core.snes_load_cartridge_normal(loadParameters.Roms[0].RomData, loadParameters.Roms[0].RomData.Length);
			}

			using (Api.EnterExit()) this.BoardName = Marshal.PtrToStringAnsi(Api.core.snes_get_board());
			_region = Api.core.snes_get_region();
			if (_region == BsnesApi.SNES_REGION.NTSC)
			{
				// taken from bsnes source
				VsyncNumerator = 21477272;
				VsyncDenominator = 357366;
			}
			else
			{
				// http://forums.nesdev.com/viewtopic.php?t=5367&start=19
				VsyncNumerator = 21281370;
				VsyncDenominator = 4 * 341 * 312;
			}

			SetMemoryDomains();

			const string TRACE_HEADER = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, B, flags (NVMXDIZC), V, H)";
			_tracer = new TraceBuffer(TRACE_HEADER);
			ser.Register<IDisassemblable>(new W65816_DisassemblerService());
			ser.Register(_tracer);

			Api.Seal();
		}

		private CoreComm CoreComm { get; }

		private readonly BsnesControllers _controllers;
		private readonly ITraceable _tracer;
		private readonly ProxiedFile _currentMsuTrack;

		private IController _controller;
		private SimpleSyncSoundProvider _soundProvider;
		private readonly string _romPath;
		private readonly bool _isSGB;
		private bool _disposed;

		public string BoardName { get; }

		internal BsnesApi Api { get; }

		private string snes_path_request(int slot, string hint, bool required)
		{
			switch (hint)
			{
				case "manifest.bml":
					Api.AddReadonlyFile($"{_romPath}.bml", hint);
					return hint;
				case "msu1/data.rom":
					Api.AddReadonlyFile($"{_romPath}.msu", hint);
					return hint;
				case "save.ram":
					// core asked for saveram, but the interface isn't designed to be able to handle this.
					// so, we'll just return nothing and the frontend will set the saveram itself later
					return null;
			}

			string firmwareId;
			string firmwareSystem = "SNES";

			switch (hint)
			{
				case "cx4": firmwareId = "CX4"; break;
				case "dsp1": firmwareId = "DSP1"; break;
				case "dsp1b": firmwareId = "DSP1b"; break;
				case "dsp2": firmwareId = "DSP2"; break;
				case "dsp3": firmwareId = "DSP3"; break;
				case "dsp4": firmwareId = "DSP4"; break;
				case "st010": firmwareId = "ST010"; break;
				case "st011": firmwareId = "ST011"; break;
				case "st018": firmwareId = "ST018"; break;
				case "sgb": firmwareId = "SGB"; firmwareSystem = "GB"; break;
				case "sgb2": firmwareId = "SGB2"; firmwareSystem = "GB"; break;
				default:
					CoreComm.ShowMessage($"Unrecognized SNES firmware request \"{hint}\".");
					return "";
			}

			string ret = "";
			FirmwareID fwid = new(firmwareSystem, firmwareId);
			const string MISSING_FIRMWARE_MSG = "Game may function incorrectly without the requested firmware.";
			byte[] data = required
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(fwid, MISSING_FIRMWARE_MSG)
				: CoreComm.CoreFileProvider.GetFirmware(fwid, MISSING_FIRMWARE_MSG);
			if (data != null)
			{
				ret = hint;
				Api.AddReadonlyFile(data, hint);
			}

			Console.WriteLine("Served bsnescore request for firmware \"{0}\"", hint);

			// return the path we built
			return ret;
		}

		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		private short snes_input_poll(int port, int index, int id)
		{
			return _controllers.CoreInputPoll(_controller, port, index, id);
		}

		private void snes_no_lag(bool sgbPoll)
		{
			// gets called whenever there was input read in the core
			if (!_isSGB || sgbPoll)
			{
				IsLagFrame = false;
			}
		}

		private void snes_controller_latch()
		{
			InputCallbacks.Call();
		}

		private readonly int[] palette = new int[0x8000];

		private void generate_palette()
		{
			for (int color = 0; color < 32768; color++) {
				int r = (color >> 10) & 31;
				int g = (color >>  5) & 31;
				int b = (color >>  0) & 31;

				r = r << 3 | r >> 2; r = r << 8 | r << 0;
				g = g << 3 | g >> 2; g = g << 8 | g << 0;
				b = b << 3 | b >> 2; b = b << 8 | b << 0;

				palette[color] = r >> 8 << 16 | g >> 8 <<  8 | b >> 8 << 0;
			}
		}

		public SnesColors.ColorType CurrPalette => SnesColors.ColorType.BSNES;

		public void SetPalette(SnesColors.ColorType colorType)
		{
			if (colorType != SnesColors.ColorType.BSNES)
				throw new NotImplementedException("This core does not currently support different palettes.");
		}

		public ISNESGraphicsDecoder CreateGraphicsDecoder() => new SNESGraphicsDecoder(Api);

		public ScanlineHookManager ScanlineHookManager => null;

		private unsafe void snes_video_refresh(IntPtr data, int width, int height, int pitch)
		{
			ushort* vp = (ushort*)data;
			if (_settings.CropSGBFrame && _isSGB)
			{
				BufferWidth = 160;
				BufferHeight = 144;
			}
			else
			{
				BufferWidth = width;
				BufferHeight = height;
			}

			int size = BufferWidth * BufferHeight;
			if (_videoBuffer.Length != size)
			{
				_videoBuffer = new int[size];
			}

			int di = 0;
			if (_settings.CropSGBFrame && _isSGB)
			{
				int initialY = _settings.ShowOverscan ? 47 : 39;
				for (int y = initialY; y < initialY + 144; y++)
				{
					ushort* sp = vp + y * pitch + 48;
					for (int x = 0; x < 160; x++)
					{
						_videoBuffer[di++] = palette[*sp++ & 0x7FFF];
					}
				}
				return;
			}

			for (int y = 0; y < height; y++)
			{
				int si = y * pitch;
				for (int x = 0; x < width; x++)
				{
					_videoBuffer[di++] = palette[vp![si++] & 0x7FFF];
				}
			}
		}

		private void InitAudio()
		{
			_soundProvider = new SimpleSyncSoundProvider();
		}

		private void snes_trace(string disassembly, string registerInfo)
			=> _tracer.Put(new(disassembly: disassembly, registerInfo: registerInfo));

		private void ReadHook(uint addr, byte value)
		{
			if (MemoryCallbacks.HasReads)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, value, (uint) MemoryCallbackFlags.AccessRead, "System Bus");
			}
		}

		private void WriteHook(uint addr, byte value)
		{
			if (MemoryCallbacks.HasWrites)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, value, (uint) MemoryCallbackFlags.AccessWrite, "System Bus");
			}
		}

		private void ExecHook(uint addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, (uint) MemoryCallbackFlags.AccessExecute, "System Bus");
			}
		}

		private static readonly DateTime _epoch = new(1970, 1, 1, 0, 0, 0);
		private long _clockTime;
		private int _clockRemainder;

		private void InitializeRtc(DateTime start)
			=> _clockTime = (long)(start - _epoch).TotalSeconds;

		internal void AdvanceRtc()
		{
			_clockRemainder += VsyncDenominator;
			if (_clockRemainder >= VsyncNumerator)
			{
				_clockRemainder -= VsyncNumerator;
				_clockTime++;
			}
		}

		private bool MsuOpenAudio(ushort trackId) => _currentMsuTrack.OpenMsuTrack(_romPath, trackId);

		private long snes_time() => DeterministicEmulation ? _clockTime : (long)(DateTime.Now - _epoch).TotalSeconds;
	}
}
