using System;
using System.Linq;
using System.Xml;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.W65816;

// TODO - add serializer (?)

// http://wiki.superfamicom.org/snes/show/Backgrounds

// TODO
// libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
// wrap dll code around some kind of library-accessing interface so that it doesn't malfunction if the dll is unavailable
namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	[Core(
		CoreNames.Bsnes115,
		"bsnes team",
		isPorted: true,
		isReleased: true,
		portedVersion: "v115",
		portedUrl: "https://bsnes.dev",
		singleInstance: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public unsafe partial class BsnesCore : IEmulator, IVideoProvider, IStatable, IInputPollable, IRegionable, ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>
	{
		private BsnesApi.SNES_REGION? _region;

		// [CoreConstructor("SGB")]
		[CoreConstructor("SNES")]
		public BsnesCore(GameInfo game, byte[] rom, CoreComm comm,
			SnesSettings settings, SnesSyncSettings syncSettings)
			:this(game, rom, null, null, comm, settings, syncSettings)
		{}

		[CoreConstructor("SNES")]
		public BsnesCore(GameInfo game, byte[] romData, byte[] xmlData, string baseRomPath, CoreComm comm,
			SnesSettings settings, SnesSyncSettings syncSettings)
		{
			_baseRomPath = baseRomPath;
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			_tracer = new TraceBuffer
			{
				Header = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, DB, flags (NVMXDIZC), V, H)"
			};

			ser.Register<IDisassemblable>(new W65816_DisassemblerService());

			_game = game;
			CoreComm = comm;
			byte[] sgbRomData = null;

			if (game.System == "SGB")
			{
				if ((romData[0x143] & 0xc0) == 0xc0)
				{
					throw new CGBNotSupportedException();
				}

				sgbRomData = CoreComm.CoreFileProvider.GetFirmware("SNES", "Rom_SGB", true, "SGB Rom is required for SGB emulation.");
				game.FirmwareHash = sgbRomData.HashSHA1();
			}

			_settings = settings ?? new SnesSettings();
			_syncSettings = syncSettings ?? new SnesSyncSettings();

			_videocb = snes_video_refresh;
			_audiocb = snes_audio_sample;
			_inputpollcb = snes_input_poll;
			_inputstatecb = snes_input_state;
			_nolagcb = snes_no_lag;
			_scanlineStartCb = snes_scanlineStart;
			_tracecb = snes_trace;
			_pathrequestcb = snes_path_request;

			// TODO: pass profile here
			Api = new BsnesApi(this, CoreComm.CoreFileProvider.DllPath(), CoreComm, new Delegate[]
			{
				_videocb,
				_audiocb,
				_inputpollcb,
				_inputstatecb,
				_nolagcb,
				_scanlineStartCb,
				_tracecb,
				_pathrequestcb
			});
			// {
				// ReadHook = u =>  ReadHook,
				// ExecHook = ExecHook,
				// WriteHook = WriteHook,
				// ReadHook_SMP = ReadHook_SMP,
				// ExecHook_SMP = ExecHook_SMP,
				// WriteHook_SMP = WriteHook_SMP,
			// };

			// ScanlineHookManager = new MyScanlineHookManager(this);

			_controllers = new BsnesControllers(_syncSettings);
			_controllers.NativeInit(Api);

			Api.CMD_init(_syncSettings.Entropy);
			Api._core.snes_set_callbacks(_inputpollcb, _inputstatecb, _nolagcb, _videocb, _audiocb, _pathrequestcb);

			Api.QUERY_set_path_request(_pathrequestcb);

			// start up audio resampler
			InitAudio();
			ser.Register<ISoundProvider>(_resampler);

			// strip header
			// if ((romData?.Length & 0x7FFF) == 512)
			// {
			// 	var newData = new byte[romData.Length - 512];
			// 	Array.Copy(romData, 512, newData, 0, newData.Length);
			// 	romData = newData;
			// }

			if (game.System == "SGB")
			{
				IsSGB = true;
				SystemId = "SNES";
				ser.Register<IBoardInfo>(new SGBBoardInfo());

				_currLoadParams = new LoadParams
				{
					type = LoadParamType.SuperGameBoy,
					baseRomPath = baseRomPath,
					rom_data = romData,
					sgb_rom_data = sgbRomData
				};

				if (!LoadCurrent())
				{
					throw new Exception("snes_load_cartridge_super_gameboy() failed");
				}
			}
			else
			{
				// we may need to get some information out of the cart, even during the following bootup/load process
				if (xmlData != null)
				{
					_romxml = new XmlDocument();
					_romxml.Load(new MemoryStream(xmlData));

					// bsnes wont inspect the xml to load the necessary sfc file.
					// so, we have to do that here and pass it in as the romData :/

					// TODO: uhh i have no idea what the xml is or whether this below code is needed
					if (_romxml["cartridge"]?["rom"] != null)
					{
						romData = File.ReadAllBytes(PathSubfile(_romxml["cartridge"]["rom"].Attributes["name"].Value));
					}
					else
					{
						throw new Exception("Could not find rom file specification in xml file. Please check the integrity of your xml file");
					}
				}

				SystemId = "SNES";
				_currLoadParams = new LoadParams
				{
					type = LoadParamType.Normal,
					baseRomPath = baseRomPath,
					rom_data = romData
				};

				if (!LoadCurrent())
				{
					throw new Exception("snes_load_cartridge_normal() failed");
				}
			}

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

			Api.CMD_power();

			// SetupMemoryDomains(romData, sgbRomData);

			ser.Register<ITraceable>(_tracer);

			Api.QUERY_set_path_request(null);
			Api.QUERY_set_video_refresh(_videocb);
			Api.QUERY_set_input_poll(_inputpollcb);
			Api.QUERY_set_input_state(_inputstatecb);
			Api.QUERY_set_no_lag(_nolagcb);
			Api.Seal();
			// RefreshPalette();
		}

		private readonly BsnesApi.snes_video_frame_t _videocb;
		private readonly BsnesApi.snes_audio_sample_t _audiocb;
		private readonly BsnesApi.snes_input_poll_t _inputpollcb;
		private readonly BsnesApi.snes_input_state_t _inputstatecb;
		private readonly BsnesApi.snes_no_lag_t _nolagcb;
		private readonly BsnesApi.snes_path_request_t _pathrequestcb;

		internal CoreComm CoreComm { get; }

		private readonly string _baseRomPath = "";

		private string PathSubfile(string fname) => Path.Combine(_baseRomPath, fname);

		private readonly GameInfo _game;
		private readonly BsnesControllers _controllers;
		private readonly ITraceable _tracer;
		private readonly XmlDocument _romxml;
		private readonly BsnesApi.snes_scanlineStart_t _scanlineStartCb;
		private readonly BsnesApi.snes_trace_t _tracecb;

		private IController _controller;
		private readonly LoadParams _currLoadParams;
		private SpeexResampler _resampler;
		private int _timeFrameCounter;
		private bool _disposed;

		public bool IsSGB { get; }

		private class SGBBoardInfo : IBoardInfo
		{
			public string BoardName => "SGB";
		}

		public BsnesApi Api { get; }

		public MyScanlineHookManager ScanlineHookManager { get; }

		public class MyScanlineHookManager : ScanlineHookManager
		{
			private readonly BsnesCore _core;

			public MyScanlineHookManager(BsnesCore core)
			{
				_core = core;
			}

			// protected override void OnHooksChanged()
			// {
				// _core.OnScanlineHooksChanged();
			// }
		}


		private void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		private string snes_path_request(int slot, string hint)
		{
			// every rom requests msu1.rom... why? who knows.
			// also handle msu-1 pcm files here
			bool isMsu1Rom = hint == "msu1/data.rom";
			bool isMsu1Pcm = Path.GetExtension(hint).ToLower() == ".pcm";
			if (isMsu1Rom || isMsu1Pcm)
			{
				// well, check if we have an msu-1 xml
				if (_romxml?["cartridge"]?["msu1"] != null)
				{
					var msu1 = _romxml["cartridge"]["msu1"];
					if (isMsu1Rom && msu1["rom"]?.Attributes["name"] != null)
					{
						return PathSubfile(msu1["rom"].Attributes["name"].Value);
					}

					if (isMsu1Pcm)
					{
						// return @"D:\roms\snes\SuperRoadBlaster\SuperRoadBlaster-1.pcm";
						// return "";
						int wantsTrackNumber = int.Parse(hint.Replace("track-", "").Replace(".pcm", ""));
						wantsTrackNumber++;
						string wantsTrackString = wantsTrackNumber.ToString();
						foreach (var child in msu1.ChildNodes.Cast<XmlNode>())
						{
							if (child.Name == "track" && child.Attributes["number"].Value == wantsTrackString)
							{
								return PathSubfile(child.Attributes["name"].Value);
							}
						}
					}
				}

				// not found.. what to do? (every rom will get here when msu1.rom is requested)
				return "";
			}

			// not MSU-1.  ok.
			if (hint == "save.ram")
			{

			}

			string firmwareId;

			switch (hint)
			{
				case "cx4.rom": firmwareId = "CX4"; break;
				case "dsp1.rom": firmwareId = "DSP1"; break;
				case "dsp1b.rom": firmwareId = "DSP1b"; break;
				case "dsp2.rom": firmwareId = "DSP2"; break;
				case "dsp3.rom": firmwareId = "DSP3"; break;
				case "dsp4.rom": firmwareId = "DSP4"; break;
				case "st010.rom": firmwareId = "ST010"; break;
				case "st011.rom": firmwareId = "ST011"; break;
				case "st018.rom": firmwareId = "ST018"; break;
				default:
					CoreComm.ShowMessage($"Unrecognized SNES firmware request \"{hint}\".");
					return "";
			}

			string ret;
			var data = CoreComm.CoreFileProvider.GetFirmware("SNES", firmwareId, false, "Game may function incorrectly without the requested firmware.");
			if (data != null)
			{
				ret = hint;
				Api.AddReadonlyFile(data, hint);
			}
			else
			{
				ret = "";
			}

			Console.WriteLine("Served bsnescore request for firmware \"{0}\"", hint);

			// return the path we built
			return ret;
		}

		private void snes_trace(uint which, string msg)
		{
			// no idea what this is but it has to go
			// TODO: get them out of the core split up and remove this hackery
			const string splitStr = "A:";

			if (which == (uint)BsnesApi.eTRACE.CPU)
			{
				var split = msg.Split(new[] { splitStr }, 2, StringSplitOptions.None);

				_tracer.Put(new TraceInfo
				{
					Disassembly = split[0].PadRight(34),
					RegisterInfo = splitStr + split[1]
				});
			}
			else if (which == (uint)BsnesApi.eTRACE.SMP)
			{
				int idx = msg.IndexOf("YA:");
				string dis = msg.Substring(0,idx).TrimEnd();
				string regs = msg.Substring(idx);
				_tracer.Put(new TraceInfo
				{
					Disassembly = dis,
					RegisterInfo = regs
				});
			}
			else if (which == (uint)BsnesApi.eTRACE.GB)
			{
				int idx = msg.IndexOf("AF:");
				string dis = msg.Substring(0,idx).TrimEnd();
				string regs = msg.Substring(idx);
				_tracer.Put(new TraceInfo
				{
					Disassembly = dis,
					RegisterInfo = regs
				});
			}
		}

		// private void ReadHook(uint addr)
		// {
		// 	if (MemoryCallbacks.HasReads)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessRead;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		// 		// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
		// 		// EDIT: for now, theres some IPC re-entrancy problem
		// 		// RefreshMemoryCallbacks();
		// 	}
		// }

		// private void ExecHook(uint addr)
		// {
		// 	if (MemoryCallbacks.HasExecutes)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessExecute;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		// 		// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
		// 		// EDIT: for now, theres some IPC re-entrancy problem
		// 		// RefreshMemoryCallbacks();
		// 	}
		// }

		// private void WriteHook(uint addr, byte val)
		// {
		// 	if (MemoryCallbacks.HasWrites)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessWrite;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "System Bus");
		// 		// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
		// 		// EDIT: for now, theres some IPC re-entrancy problem
		// 		// RefreshMemoryCallbacks();
		// 	}
		// }
		//
		// private void ReadHook_SMP(uint addr)
		// {
		// 	if (MemoryCallbacks.HasReads)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessRead;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "SMP");
		// 	}
		// }
		//
		// private void ExecHook_SMP(uint addr)
		// {
		// 	if (MemoryCallbacks.HasExecutes)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessExecute;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "SMP");
		// 	}
		// }
		//
		// private void WriteHook_SMP(uint addr, byte val)
		// {
		// 	if (MemoryCallbacks.HasWrites)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessWrite;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "SMP");
		// 	}
		// }

		private enum LoadParamType
		{
			Normal, SuperGameBoy
		}

		private struct LoadParams
		{
			public LoadParamType type;
			public string baseRomPath;
			public byte[] rom_data;
			public byte[] sgb_rom_data;
		}

		private bool LoadCurrent()
		{
			bool result = _currLoadParams.type == LoadParamType.Normal
				? Api.CMD_load_cartridge_normal(_currLoadParams.baseRomPath, _currLoadParams.rom_data)
				: Api.CMD_load_cartridge_super_game_boy(_currLoadParams.baseRomPath, _currLoadParams.rom_data, _currLoadParams.sgb_rom_data);

			// _mapper = Api.Mapper;
			_region = Api.Region;

			return result;
		}

		// poll which updates the controller state
		private void snes_input_poll()
		{
			_controllers.CoreInputPoll(_controller);
		}

		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="device">LibsnesApi.SNES_DEVICE enum index specifying type of device</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		private short snes_input_state(int port, int device, int index, int id)
		{
			// we're not using device here... should we?
			return _controllers.CoreInputState(port, index, id);
		}

		private void snes_no_lag()
		{
			// gets called whenever there was input polled, aka no lag
			IsLagFrame = false;
		}

		private void snes_video_refresh(int* data, int width, int height)
		{
			// bool doubleSize = _settings.AlwaysDoubleSize;
			bool doubleSize = false;
			bool lineDouble = doubleSize, dotDouble = doubleSize;

			_videoWidth = width;
			_videoHeight = height;

			int yskip = 1, xskip = 1;

			// if we are in high-res mode, we get double width. so, lets double the height here to keep it square.
			if (width == 512)
			{
				_videoHeight *= 2;
				yskip = 2;

				lineDouble = true;

				// we don't dot double here because the user wanted double res and the game provided double res
				dotDouble = false;
			}
			else if (lineDouble)
			{
				_videoHeight *= 2;
				yskip = 2;
			}

			int srcPitch = 1024;
			int srcStart = 0;

			bool interlaced = height == 478 || height == 448;
			if (interlaced)
			{
				// from bsnes in interlaced mode we have each field side by side
				// so we will come in with a dimension of 512x448, say
				// but the fields are side by side, so it's actually 1024x224.
				// copy the first scanline from row 0, then the 2nd scanline from row 0 (offset 512)
				// EXAMPLE: yu yu hakushu legal screens
				// EXAMPLE: World Class Service Super Nintendo Tester (double resolution vertically but not horizontally, in character test the stars should shrink)
				lineDouble = false;
				srcPitch = 512;
				yskip = 1;
				_videoHeight = height;
			}

			if (dotDouble)
			{
				_videoWidth *= 2;
				xskip = 2;
			}

			// if (_settings.CropSGBFrame && IsSGB)
			// {
				// _videoWidth = 160;
				// _videoHeight = 144;
			// }

			int size = _videoWidth * _videoHeight;
			if (_videoBuffer.Length != size)
			{
				_videoBuffer = new int[size];
			}

			// if (_settings.CropSGBFrame && IsSGB)
			// {
			// 	int di = 0;
			// 	for (int y = 0; y < 144; y++)
			// 	{
			// 		int si = ((y+39) * srcPitch) + 48;
			// 		for(int x=0;x<160;x++)
			// 			_videoBuffer[di++] = data[si++];
			// 	}
			// 	return;
			// }

			for (int j = 0; j < 2; j++)
			{
				if (j == 1 && !dotDouble)
				{
					break;
				}

				int xbonus = j;
				for (int i = 0; i < 2; i++)
				{
					// potentially do this twice, if we need to line double
					if (i == 1 && !lineDouble)
					{
						break;
					}

					int bonus = (i * _videoWidth) + xbonus;
					for (int y = 0; y < height; y++)
					{
						for (int x = 0; x < width; x++)
						{
							int si = (y * srcPitch) + x + srcStart;
							int di = y * _videoWidth * yskip + x * xskip + bonus;
							int rgb = data[si];
							_videoBuffer[di] = rgb;
						}
					}
				}
			}

			VirtualHeight = BufferHeight;
			VirtualWidth = BufferWidth;
			if (VirtualHeight * 2 < VirtualWidth)
				VirtualHeight *= 2;
			if (VirtualHeight > 240)
				VirtualWidth = 512;
			VirtualWidth = (int)Math.Round(VirtualWidth * 1.146);
		}

		// private void RefreshMemoryCallbacks(bool suppress)
		// {
			// var mcs = MemoryCallbacks;
			// Api.QUERY_set_state_hook_exec(!suppress && mcs.HasExecutesForScope("System Bus"));
			// Api.QUERY_set_state_hook_read(!suppress && mcs.HasReadsForScope("System Bus"));
			// Api.QUERY_set_state_hook_write(!suppress && mcs.HasWritesForScope("System Bus"));
		// }

		//public byte[] snes_get_memory_data_read(LibsnesApi.SNES_MEMORY id)
		//{
		//  var size = (int)api.snes_get_memory_size(id);
		//  if (size == 0) return new byte[0];
		//  var ret = api.snes_get_memory_data(id);
		//  return ret;
		//}

		private void InitAudio()
		{
			_resampler = new SpeexResampler((SpeexResampler.Quality)6, 64081, 88200, 32041, 44100);
		}

		public void snes_audio_sample(ushort left, ushort right)
		{
			_resampler.EnqueueSample((short)left, (short)right);
		}

		// private void RefreshPalette()
		// {
			// SetPalette((SnesColors.ColorType)Enum.Parse(typeof(SnesColors.ColorType), _settings.Palette, false));
		// }
	}
}
