//TODO hook up newer file ID stuff, think about how to combine it with the disc ID
//TODO change display manager to not require 0xFF alpha channel set on videoproviders. check gdi+ and opengl! this will get us a speedup in some places
//TODO Disc.Structure.Sessions[0].length_aba was 0
//TODO mednafen 0.9.37 changed some disc region detection heuristics. analyze and apply in c# side. also the SCEX id handling changed, maybe simplified

//TODO - ok, think about this. we MUST load a state with the CDC completely intact. no quickly changing discs. that's madness.
//well, I could savestate the disc index and validate the disc collection when loading a state.
//the big problem is, it's completely at odds with the slider-based disc changing model. 
//but, maybe it can be reconciled with that model by using the disc ejection to our advantage. 
//perhaps moving the slider is meaningless if the disc is ejected--it only affects what disc is inserted when the disc gets inserted!! yeah! this might could save us!
//not exactly user friendly but maybe we can build it from there with a custom UI.. a disk-changer? dunno if that would help

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using BizHawk.Emulation.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.DiscSystem;

#pragma warning disable 649 //adelikat: Disable dumb warnings until this file is complete

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	[PortedCore(CoreNames.Octoshock, "Mednafen Team")]
	public unsafe partial class Octoshock : IEmulator, IInputPollable, IRegionable, ISaveRam,
		ISettable<Octoshock.Settings, Octoshock.SyncSettings>, ISoundProvider, IStatable, IVideoProvider,
		IDriveLight, IRedumpDiscChecksumInfo
	{
		public Octoshock(CoreComm comm, PSF psf, Octoshock.Settings settings, Octoshock.SyncSettings syncSettings)
		{
			string romDetails = "It's a PSF, what do you want. Oh, tags maybe?";
			Load(comm, null, null, null, settings, syncSettings, psf, romDetails);
			OctoshockDll.shock_PowerOn(psx);
		}

		//note: its annoying that we have to have a disc before constructing this.
		//might want to change that later. HOWEVER - we need to definitely have a region, at least
		[CoreConstructor(VSystemID.Raw.PSX)]
		public Octoshock(CoreLoadParameters<Octoshock.Settings, Octoshock.SyncSettings> lp)
		{
			Load(
				lp.Comm,
				lp.Discs.Select(d => d.DiscData).ToList(),
				lp.Discs.Select(d => d.DiscName).ToList(),
				lp.Roms.FirstOrDefault()?.RomData,
				lp.Settings,
				lp.SyncSettings,
				null,
				DiscChecksumUtils.GenQuickRomDetails(lp.Discs));
			OctoshockDll.shock_PowerOn(psx);
		}

		private void Load(
			CoreComm comm, List<Disc> discs, List<string> discNames, byte[] exe,
			Octoshock.Settings settings, Octoshock.SyncSettings syncSettings, PSF psf, string romDetails)
		{
			RomDetails = romDetails;
			ConnectTracer();
			DriveLightEnabled = true;

			_Settings = settings ?? new Settings();
			_SyncSettings = syncSettings ?? new SyncSettings();

			Discs = discs;

			Attach();

			//assume this region for EXE and PSF, maybe not correct though
			string firmwareRegion = "U";
			SystemRegion = OctoshockDll.eRegion.NA;

			if (discs != null)
			{
				HackyDiscButtons.AddRange(discNames);

				foreach (var disc in discs)
				{
					var discInterface = new DiscInterface(disc,
						di =>
						{
							//if current disc this delegate disc, activity is happening
							if (di == currentDiscInterface)
								DriveLightOn = true;
						});

					discInterfaces.Add(discInterface);
				}
			}
			else
			{
				//assume its NA region for test programs, for now. could it be read out of the ps-exe header?
			}

			if (discInterfaces.Count != 0)
			{
				//determine region of one of the discs
				OctoshockDll.shock_AnalyzeDisc(discInterfaces[0].OctoshockHandle, out var discInfo);

				//try to acquire the appropriate firmware
				if (discInfo.region == OctoshockDll.eRegion.EU) firmwareRegion = "E";
				if (discInfo.region == OctoshockDll.eRegion.JP) firmwareRegion = "J";
				SystemRegion = discInfo.region;
			}

			bool use_nocash_specs = false;

			if (use_nocash_specs)
			{
				//see http://problemkaputt.de/psx-spx.htm
				int CpuClock_n = 44100 * 768;
				int CpuClock_d = 1;
				int VidClock_n = CpuClock_n * 11;
				int VidClock_d = CpuClock_d * 7;
				if (SystemRegion == OctoshockDll.eRegion.EU)
				{
					VsyncNumerator = VidClock_n;
					VsyncDenominator = VidClock_d * 314 * 3406;
					SystemVidStandard = OctoshockDll.eVidStandard.PAL;
				}
				else
				{
					VsyncNumerator = VidClock_n;
					VsyncDenominator = VidClock_d * 263 * 3413;
					SystemVidStandard = OctoshockDll.eVidStandard.NTSC;
				}
			}
			else
			{
				//use mednafen specs
				if (SystemRegion == OctoshockDll.eRegion.EU)
				{
					//https://github.com/TASEmulators/mednafen/blob/740d63996fc7cebffd39ee253a29ee434965db21/src/psx/gpu.cpp#L175
					// -> 838865530 / 65536 / 256 -> reduced
					VsyncNumerator = 419432765;
					VsyncDenominator = 8388608;
					SystemVidStandard = OctoshockDll.eVidStandard.PAL;
				}
				else
				{
					//https://github.com/TASEmulators/mednafen/blob/740d63996fc7cebffd39ee253a29ee434965db21/src/psx/gpu.cpp#L183
					//-> 1005627336 / 65536 / 256 -> reduced
					VsyncNumerator = 502813668;
					VsyncDenominator = 8388608;
					SystemVidStandard = OctoshockDll.eVidStandard.NTSC;
				}
			}

			//TODO - known bad firmwares are a no-go. we should refuse to boot them. (that's the mednafen policy)
			var firmware = comm.CoreFileProvider.GetFirmwareOrThrow(new("PSX", firmwareRegion), $"A PSX `{firmwareRegion}` region bios file is required");

			//create the instance
			fixed (byte* pFirmware = firmware)
				OctoshockDll.shock_Create(out psx, SystemRegion, pFirmware);

			SetMemoryDomains();
			InitMemCallbacks();

			//set a default framebuffer based on the first frame of emulation, to cut down on flickering or whatever
			//this is probably quixotic, but we have to pick something
			{
				BufferWidth = 280;
				BufferHeight = 240;
				if (SystemVidStandard == OctoshockDll.eVidStandard.PAL)
				{
					BufferWidth = 280;
					BufferHeight = 288;
				}
				CurrentVideoSize = new System.Drawing.Size(BufferWidth, BufferHeight);
				var ri = CalculateResolution(SystemVidStandard, _Settings, BufferWidth, BufferHeight);
				BufferWidth = VirtualWidth = ri.Resolution.Width;
				BufferHeight = VirtualHeight = ri.Resolution.Height;
				//VideoProvider_Padding = new System.Drawing.Size(50,50);
				frameBuffer = new int[BufferWidth * BufferHeight];
			}

			if (discInterfaces.Count != 0)
			{
				//start with first disc inserted and tray closed. it's a sensible default.
				//it will be possible for the user to specify a different initial configuration, but this will inform the UI
				CurrentTrayOpen = false;
				CurrentDiscIndexMounted = 1;
			}
			else if (psf == null)
			{
				//must be an exe
				fixed (byte* pExeBuffer = exe)
					OctoshockDll.shock_MountEXE(psx, pExeBuffer, exe.Length, false);

				//start with no disc inserted and tray closed
				CurrentTrayOpen = false;
				CurrentDiscIndexMounted = 0;
				OctoshockDll.shock_CloseTray(psx);
			}
			else
			{
				//must be a psf
				if (psf.LibData != null)
					fixed (byte* pBuf = psf.LibData)
						OctoshockDll.shock_MountEXE(psx, pBuf, psf.LibData.Length, true);
				fixed (byte* pBuf = psf.Data)
					OctoshockDll.shock_MountEXE(psx, pBuf, psf.Data.Length, false);

				//start with no disc inserted and tray closed
				CurrentTrayOpen = false;
				CurrentDiscIndexMounted = 0;
				OctoshockDll.shock_CloseTray(psx);
			}

			//setup the controller based on sync settings
			SetControllerButtons();

			var fioCfg = _SyncSettings.FIOConfig;
			if (fioCfg.Multitaps[0])
			{
				OctoshockDll.shock_Peripheral_Connect(psx, 0x01, OctoshockDll.ePeripheralType.Multitap);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x11, fioCfg.Devices8[0]);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x21, fioCfg.Devices8[1]);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x31, fioCfg.Devices8[2]);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x41, fioCfg.Devices8[3]);
			}
			else
				OctoshockDll.shock_Peripheral_Connect(psx, 0x01, fioCfg.Devices8[0]);

			if (fioCfg.Multitaps[1])
			{
				OctoshockDll.shock_Peripheral_Connect(psx, 0x02, OctoshockDll.ePeripheralType.Multitap);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x12, fioCfg.Devices8[4]);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x22, fioCfg.Devices8[5]);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x32, fioCfg.Devices8[6]);
				OctoshockDll.shock_Peripheral_Connect(psx, 0x42, fioCfg.Devices8[7]);
			}
			else
				OctoshockDll.shock_Peripheral_Connect(psx, 0x02, fioCfg.Devices8[4]);

			var memcardTransaction = new OctoshockDll.ShockMemcardTransaction()
			{
				transaction = OctoshockDll.eShockMemcardTransaction.Connect
			};
			if (fioCfg.Memcards[0]) OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x01, ref memcardTransaction);
			if (fioCfg.Memcards[1]) OctoshockDll.shock_Peripheral_MemcardTransact(psx, 0x02, ref memcardTransaction);

			//do this after framebuffers and peripherals and whatever crap are setup. kind of lame, but that's how it is for now
			StudySaveBufferSize();
		}

		public string RomDetails { get; private set; }

		public string SystemId => VSystemID.Raw.PSX;

		public static ControllerDefinition CreateControllerDefinition(SyncSettings syncSettings)
		{
			ControllerDefinition definition = new("PSX Front Panel");

			var cfg = syncSettings.FIOConfig.ToLogical();

			for (int i = 0; i < cfg.NumPlayers; i++)
			{
				int pnum = i + 1;

				var type = cfg.DevicesPlayer[i];
				if (type == OctoshockDll.ePeripheralType.NegCon)
				{
					definition.BoolButtons.AddRange(new[]
					{
							"P" + pnum + " Up",
							"P" + pnum + " Down",
							"P" + pnum + " Left",
							"P" + pnum + " Right",
							"P" + pnum + " Start",
							"P" + pnum + " R",
							"P" + pnum + " B",
							"P" + pnum + " A",
					});

					foreach (var axisName in new[] { $"P{pnum} Twist", $"P{pnum} 1", $"P{pnum} 2", $"P{pnum} L" })
					{
						definition.AddAxis(axisName, 0.RangeTo(255), 128);
					}
				}
				else
				{
					definition.BoolButtons.AddRange(new[]
					{
							"P" + pnum + " Up",
							"P" + pnum + " Down",
							"P" + pnum + " Left",
							"P" + pnum + " Right",
							"P" + pnum + " Select",
							"P" + pnum + " Start",
							"P" + pnum + " Square",
							"P" + pnum + " Triangle",
							"P" + pnum + " Circle",
							"P" + pnum + " Cross",
							"P" + pnum + " L1",
							"P" + pnum + " R1",
							"P" + pnum + " L2",
							"P" + pnum + " R2",
						});


					if (type == OctoshockDll.ePeripheralType.DualShock || type == OctoshockDll.ePeripheralType.DualAnalog)
					{
						definition.BoolButtons.Add("P" + pnum + " L3");
						definition.BoolButtons.Add("P" + pnum + " R3");
						definition.BoolButtons.Add("P" + pnum + " MODE");
						definition.AddXYPair($"P{pnum} LStick {{0}}", AxisPairOrientation.RightAndDown, 0.RangeTo(255), 128);
						definition.AddXYPair($"P{pnum} RStick {{0}}", AxisPairOrientation.RightAndDown, 0.RangeTo(255), 128);
					}
				}
			}

			definition.BoolButtons.AddRange(new[]
			{
				"Open",
				"Close",
				"Reset"
			});

			definition.AddAxis("Disc Select", 0.RangeTo(1), 1);

			return definition.MakeImmutable();
		}

		private void SetControllerButtons()
		{
			ControllerDefinition = CreateControllerDefinition(_SyncSettings);
		}

		private int[] frameBuffer = new int[0];
		private Random rand = new Random();

		//we can only have one active core at a time, due to the lib being so static.
		//so we'll track the current one here and detach the previous one whenever a new one is booted up.
		private static Octoshock CurrOctoshockCore;

		private IntPtr psx;

		private bool disposed = false;
		public void Dispose()
		{
			if (disposed) return;

			disposed = true;

			//discs arent bound to shock core instances, but they may be mounted. kill the core instance first to effectively dereference the disc
			OctoshockDll.shock_Destroy(psx);
			psx = IntPtr.Zero;

			//destroy all discs we're managing (and the unmanaged octoshock resources)
			foreach (var di in discInterfaces)
			{
				di.Disc.Dispose();
				di.Dispose();
			}
			discInterfaces.Clear();
		}

		/// <summary>
		/// Wraps the ShockDiscRef returned from the DLL and acts as a bridge between it and a DiscSystem disc
		/// </summary>
		private class DiscInterface : IDisposable
		{
			public DiscInterface(Disc disc, Action<DiscInterface> cbActivity)
			{
				this.Disc = disc;
				cbReadTOC = ShockDisc_ReadTOC;
				cbReadLBA = ShockDisc_ReadLBA2448;
				this.cbActivity = cbActivity;
				OctoshockDll.shock_CreateDisc(out OctoshockHandle, IntPtr.Zero, disc.Session1.LeadoutLBA, cbReadTOC, cbReadLBA, true);
			}

			private OctoshockDll.ShockDisc_ReadTOC cbReadTOC;
			private OctoshockDll.ShockDisc_ReadLBA cbReadLBA;
			private readonly Action<DiscInterface> cbActivity;

			public readonly Disc Disc;
			public IntPtr OctoshockHandle;

			public void Dispose()
			{
				OctoshockDll.shock_DestroyDisc(OctoshockHandle);
				OctoshockHandle = IntPtr.Zero;
			}

			private int ShockDisc_ReadTOC(IntPtr opaque, OctoshockDll.ShockTOC* read_target, OctoshockDll.ShockTOCTrack* tracks101)
			{
				read_target->disc_type = (byte)Disc.TOC.SessionFormat;
				read_target->first_track = (byte)Disc.TOC.FirstRecordedTrackNumber; //i _think_ that's what is meant here
				read_target->last_track = (byte)Disc.TOC.LastRecordedTrackNumber; //i _think_ that's what is meant here

				tracks101[0].lba = tracks101[0].adr = tracks101[0].control = 0;

				for (int i = 1; i < 100; i++)
				{
					var item = Disc.TOC.TOCItems[i];
					tracks101[i].adr = (byte)(item.Exists ? 1 : 0);
					tracks101[i].lba = (uint)item.LBA;
					tracks101[i].control = (byte)item.Control;
				}

				////the lead-out track is to be synthesized
				tracks101[read_target->last_track + 1].adr = 1;
				tracks101[read_target->last_track + 1].control = 0;
				tracks101[read_target->last_track + 1].lba = (uint)Disc.TOC.LeadoutLBA;

				//element 100 is to be copied as the lead-out track
				tracks101[100] = tracks101[read_target->last_track + 1];

				return OctoshockDll.SHOCK_OK;
			}

			private readonly byte[] SectorBuffer = new byte[2448];

			private int ShockDisc_ReadLBA2448(IntPtr opaque, int lba, void* dst)
			{
				cbActivity(this);

				//todo - cache reader
				var dsr = new DiscSectorReader(Disc);
				int readed = dsr.ReadLBA_2448(lba, SectorBuffer, 0);
				if (readed == 2448)
				{
					Marshal.Copy(SectorBuffer, 0, new IntPtr(dst), 2448);
					return OctoshockDll.SHOCK_OK;
				}

				return OctoshockDll.SHOCK_ERROR;
			}
		}

		public List<Disc> Discs;
		private readonly List<DiscInterface> discInterfaces = new List<DiscInterface>();
		private DiscInterface currentDiscInterface;

		public DisplayType Region => SystemVidStandard == OctoshockDll.eVidStandard.PAL ? DisplayType.PAL : DisplayType.NTSC;

		public OctoshockDll.eRegion SystemRegion { get; private set; }
		public OctoshockDll.eVidStandard SystemVidStandard { get; private set; }
		public System.Drawing.Size CurrentVideoSize { get; private set; }

		public bool CurrentTrayOpen { get; private set; }
		public int CurrentDiscIndexMounted { get; private set; }

		public readonly IList<string> HackyDiscButtons = new List<string>();

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }
		public string DriveLightIconDescription => "CD Drive Activity";

		private void Attach()
		{
			//attach this core as the current
			CurrOctoshockCore?.Dispose();
			CurrOctoshockCore = this;

			//the psx instance cant be created until the desired region is known, which needs a disc, so we need the dll static attached first
		}

		static Octoshock()
		{
		}

		public string CalculateDiscHashes()
			=> DiscChecksumUtils.CalculateDiscHashesImpl(Discs);

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		private void SetInput()
		{
			var fioCfg = _SyncSettings.FIOConfig.ToLogical();

			for (int port = 0; port < 2; port++)
			{
				for (int multiport = 0; multiport < 4; multiport++)
				{
					//note: I would not say this port addressing scheme has been completely successful
					//however, it may be because i was constantly constrained by having to adapt it to mednafen.. i don't know.

					int portNum = (port + 1) + ((multiport + 1) << 4);
					int slot = port * 4 + multiport;
					
					//no input to set
					if (fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.None)
						continue;

					//address differently if it isn't multitap
					if (!fioCfg.Multitaps[port])
						portNum = port + 1;

					uint buttons = 0;
					string pstring = "P" + fioCfg.PlayerAssignments[slot] + " ";

					if (fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.NegCon)
					{
						//1,2,4 skipped (would be Select, L3, R3 on other pads)
						if (_controller.IsPressed(pstring + "Start")) buttons |= 8;
						if (_controller.IsPressed(pstring + "Up")) buttons |= 16;
						if (_controller.IsPressed(pstring + "Right")) buttons |= 32;
						if (_controller.IsPressed(pstring + "Down")) buttons |= 64;
						if (_controller.IsPressed(pstring + "Left")) buttons |= 128;
						//256,512,1024 skipped (would be L2, R2, L1 on other pads)
						if (_controller.IsPressed(pstring + "R")) buttons |= 2048;
						if (_controller.IsPressed(pstring + "B")) buttons |= 4096;
						if (_controller.IsPressed(pstring + "A")) buttons |= 8192;

						byte twist = (byte)_controller.AxisValue(pstring + "Twist");
						byte analog1 = (byte)_controller.AxisValue(pstring + "1");
						byte analog2 = (byte)_controller.AxisValue(pstring + "2");
						byte analogL = (byte)_controller.AxisValue(pstring + "L");

						OctoshockDll.shock_Peripheral_SetPadInput(psx, portNum, buttons, twist, analog1, analog2, analogL);
					}
					else
					{
						if (_controller.IsPressed(pstring + "Select")) buttons |= 1;
						if (_controller.IsPressed(pstring + "Start")) buttons |= 8;
						if (_controller.IsPressed(pstring + "Up")) buttons |= 16;
						if (_controller.IsPressed(pstring + "Right")) buttons |= 32;
						if (_controller.IsPressed(pstring + "Down")) buttons |= 64;
						if (_controller.IsPressed(pstring + "Left")) buttons |= 128;
						if (_controller.IsPressed(pstring + "L2")) buttons |= 256;
						if (_controller.IsPressed(pstring + "R2")) buttons |= 512;
						if (_controller.IsPressed(pstring + "L1")) buttons |= 1024;
						if (_controller.IsPressed(pstring + "R1")) buttons |= 2048;
						if (_controller.IsPressed(pstring + "Triangle")) buttons |= 4096;
						if (_controller.IsPressed(pstring + "Circle")) buttons |= 8192;
						if (_controller.IsPressed(pstring + "Cross")) buttons |= 16384;
						if (_controller.IsPressed(pstring + "Square")) buttons |= 32768;

						byte left_x = 0, left_y = 0, right_x = 0, right_y = 0;
						if (fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.DualShock || fioCfg.Devices8[slot] == OctoshockDll.ePeripheralType.DualAnalog)
						{
							if (_controller.IsPressed(pstring + "L3")) buttons |= 2;
							if (_controller.IsPressed(pstring + "R3")) buttons |= 4;
							if (_controller.IsPressed(pstring + "MODE")) buttons |= 65536;

							left_x = (byte)_controller.AxisValue(pstring + "LStick X");
							left_y = (byte)_controller.AxisValue(pstring + "LStick Y");
							right_x = (byte)_controller.AxisValue(pstring + "RStick X");
							right_y = (byte)_controller.AxisValue(pstring + "RStick Y");
						}

						OctoshockDll.shock_Peripheral_SetPadInput(psx, portNum, buttons, left_x, left_y, right_x, right_y);
					}
				}
			}
		}

		public class ResolutionInfo
		{
			public System.Drawing.Size Resolution, Padding;
			public System.Drawing.Size Total => System.Drawing.Size.Add(Resolution, Padding);
		}

		/// <summary>
		/// Calculates what the output resolution would be for the given input resolution and settings
		/// </summary>
		public static ResolutionInfo CalculateResolution(OctoshockDll.eVidStandard standard, Settings settings, int w, int h)
		{
			ResolutionInfo ret = new ResolutionInfo();

			//some of this logic is duplicated in the c++ side, be sure to check there
			//TODO - scanline control + framebuffer mode is majorly broken

			int virtual_width = 800;
			if (settings.HorizontalClipping == eHorizontalClipping.Basic) virtual_width = 768;
			if (settings.HorizontalClipping == eHorizontalClipping.Framebuffer) virtual_width = 736;

			int scanline_start = standard == OctoshockDll.eVidStandard.NTSC ? settings.ScanlineStart_NTSC : settings.ScanlineStart_PAL;
			int scanline_end = standard == OctoshockDll.eVidStandard.NTSC ? settings.ScanlineEnd_NTSC : settings.ScanlineEnd_PAL;
			int scanline_num = scanline_end - scanline_start + 1;
			//int scanline_num = h; // I wanted to do this, but our logic for mednafen modes here is based on un-doubled resolution. i could do a hack to divide it by 2 though
			int real_scanline_num = standard == OctoshockDll.eVidStandard.NTSC ? 240 : 288;

			int VirtualWidth=-1, VirtualHeight=-1;
			switch (settings.ResolutionMode)
			{
				case eResolutionMode.Mednafen:

					//mednafen uses 320xScanlines as the 1x size
					//it does change the 1x width when doing basic clipping.
					//and it does easily change the height when doing scanline removal.
					//now, our framebuffer cropping mode is more complex...
					VirtualWidth = (standard == OctoshockDll.eVidStandard.NTSC) ? 320 : 363;
					VirtualHeight = scanline_num;

					if (settings.HorizontalClipping == eHorizontalClipping.Basic)
						VirtualWidth = (standard == OctoshockDll.eVidStandard.NTSC) ? 302 : 384;

					if (settings.HorizontalClipping == eHorizontalClipping.Framebuffer)
					{
						//mednafen typically sends us a framebuffer with overscan. 350x240 is a nominal example here. it's squished inward to 320x240 for correct PAR.
						//ok: here we have a framebuffer without overscan. 320x240 nominal. So the VirtualWidth of what we got is off by a factor of 109.375%
						//so a beginning approach would be this:
						//VirtualWidth = (int)(VirtualWidth * 320.0f / 350);
						//but that will shrink things which are already annoyingly shrunken. 
						//therefore, lets do that, but then scale the whole window by the same factor so the width becomes unscaled and now the height is scaled up!
						//weird, huh?
						VirtualHeight = (int)(VirtualHeight * 350.0f / 320);

						//now unfortunately we may have lost vertical pixels. common in the case of PAL (rendering 256 on a field of 288)
						//therefore we'll be stretching way too much vertically here. 
						//lets add those pixels back with a new hack
						if (standard == OctoshockDll.eVidStandard.PAL)
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 576 - h);
							else ret.Padding = new System.Drawing.Size(0, 288 - h);
						}
						else
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 480 - h);
							else ret.Padding = new System.Drawing.Size(0, 240 - h);
						}
					}
					break;

				//384 / 288 = 1.3333333333333333333333333333333

				case eResolutionMode.TweakedMednafen:

					if (standard == OctoshockDll.eVidStandard.NTSC)
					{
						//don't make this 430, it's already been turned into 400 from 368+30 and then some fudge factor
						VirtualWidth = 400;
						VirtualHeight = (int)(scanline_num * 300.0f / 240);
						if (settings.HorizontalClipping == eHorizontalClipping.Basic)
							VirtualWidth = 378;
					}
					else
					{
						//this is a bit tricky. we know we want 400 for the virtualwidth. 
						VirtualWidth = 400;
						if (settings.HorizontalClipping == eHorizontalClipping.Basic)
							VirtualWidth = 378;
						//I'll be honest, I was just guessing here mostly
						//I need the AR to basically work out to be 363/288 (that's what it was in mednafen mode) so...
						VirtualHeight = (int)(scanline_num * (400.0f/363*288) / 288);
					}

					if (settings.HorizontalClipping == eHorizontalClipping.Framebuffer)
					{
						//see discussion above
						VirtualHeight = (int)(VirtualHeight * 350.0f / 320);

						if (standard == OctoshockDll.eVidStandard.PAL)
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 576 - h);
							else ret.Padding = new System.Drawing.Size(0, 288 - h);
						}
						else
						{
							if (h > 288) ret.Padding = new System.Drawing.Size(0, 480 - h);
							else ret.Padding = new System.Drawing.Size(0, 240 - h);
						}
					}
					break;

				case eResolutionMode.PixelPro:
					VirtualWidth = virtual_width;
					VirtualHeight = scanline_num * 2;
					break;

				case eResolutionMode.Debug:
					VirtualWidth = w;
					VirtualHeight = h;
					break;
			}

			ret.Resolution = new System.Drawing.Size(VirtualWidth, VirtualHeight);
			return ret;
		}

		private void PokeDisc()
		{
			if (CurrentDiscIndexMounted == 0)
			{
				currentDiscInterface = null;
				OctoshockDll.shock_PokeDisc(psx, IntPtr.Zero);
			}
			else
			{
				currentDiscInterface = discInterfaces[CurrentDiscIndexMounted - 1];
				OctoshockDll.shock_PokeDisc(psx, currentDiscInterface.OctoshockHandle);
			}
		}

		private void FrameAdvance_PrepDiscState()
		{
			//reminder: if this is the beginning of time, we can begin with the disc ejected or inserted.

			//if tray open is requested, and valid, apply it
			//in the first frame, go ahead and open it up so we have a chance to put a disc in it
			if (_controller.IsPressed("Open") && !CurrentTrayOpen || Frame == 0)
			{
				OctoshockDll.shock_OpenTray(psx);
				CurrentTrayOpen = true;
			}

			//change the disc if needed, and valid
			//also if frame is 0, we need to set a disc no matter what
			int requestedDisc = _controller.AxisValue("Disc Select");
			if (requestedDisc != CurrentDiscIndexMounted && CurrentTrayOpen
				|| Frame == 0
				)
			{
				//don't replace default disc with the leave-default placeholder!
				if (requestedDisc == -1)
				{

				}
				else
				{
					CurrentDiscIndexMounted = requestedDisc;
				}

				if (CurrentDiscIndexMounted == 0 || discInterfaces.Count == 0)
				{
					currentDiscInterface = null;
					OctoshockDll.shock_SetDisc(psx, IntPtr.Zero);
				}
				else
				{
					currentDiscInterface = discInterfaces[CurrentDiscIndexMounted - 1];
					OctoshockDll.shock_SetDisc(psx, currentDiscInterface.OctoshockHandle);
				}
			}

			//if tray close is requested, and valid, apply it.
			if (_controller.IsPressed("Close") && CurrentTrayOpen)
			{
				OctoshockDll.shock_CloseTray(psx);
				CurrentTrayOpen = false;
			}

			//if frame is 0 and user has made no preference, close the tray
			if (!_controller.IsPressed("Close") && !_controller.IsPressed("Open") && Frame == 0 && CurrentTrayOpen)
			{
				OctoshockDll.shock_CloseTray(psx);
				CurrentTrayOpen = false;
			}
		}

		private IController _controller;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;
			FrameAdvance_PrepDiscState();

			//clear drive light. itll get set to light up by sector-reading callbacks
			//TODO - debounce this by a frame or so perhaps?
			//TODO - actually, make this feedback from the core. there should be a register or status which effectively corresponds to whether it's reading.
			DriveLightOn = false;

			SetInput();

			OctoshockDll.shock_SetLEC(psx, _SyncSettings.EnableLEC);

			var ropts = new OctoshockDll.ShockRenderOptions()
			{
				scanline_start = SystemVidStandard == OctoshockDll.eVidStandard.NTSC ? _Settings.ScanlineStart_NTSC : _Settings.ScanlineStart_PAL,
				scanline_end = SystemVidStandard == OctoshockDll.eVidStandard.NTSC ? _Settings.ScanlineEnd_NTSC : _Settings.ScanlineEnd_PAL,
			};
			if (_Settings.HorizontalClipping == eHorizontalClipping.Basic)
				ropts.renderType = OctoshockDll.eShockRenderType.ClipOverscan;
			if (_Settings.HorizontalClipping == eHorizontalClipping.Framebuffer)
				ropts.renderType = OctoshockDll.eShockRenderType.Framebuffer;

			if (_Settings.DeinterlaceMode == eDeinterlaceMode.Weave) ropts.deinterlaceMode = OctoshockDll.eShockDeinterlaceMode.Weave;
			if (_Settings.DeinterlaceMode == eDeinterlaceMode.Bob) ropts.deinterlaceMode = OctoshockDll.eShockDeinterlaceMode.Bob;
			if (_Settings.DeinterlaceMode == eDeinterlaceMode.BobOffset) ropts.deinterlaceMode = OctoshockDll.eShockDeinterlaceMode.BobOffset;

			OctoshockDll.shock_SetRenderOptions(psx, ref ropts);

			//prep tracer
			if (Tracer.IsEnabled())
				OctoshockDll.shock_SetTraceCallback(psx, IntPtr.Zero, trace_cb);
			else
				OctoshockDll.shock_SetTraceCallback(psx, IntPtr.Zero, null);

			//apply soft reset if needed
			if (_controller.IsPressed("Reset"))
				OctoshockDll.shock_SoftReset(psx);

			//------------------------
			OctoshockDll.shock_Step(psx, OctoshockDll.eShockStep.Frame);
			//------------------------

			//lag maintenance:
			int pad1 = OctoshockDll.shock_Peripheral_PollActive(psx, 0x01, true);
			int pad2 = OctoshockDll.shock_Peripheral_PollActive(psx, 0x02, true);
			IsLagFrame = true;
			if (pad1 == OctoshockDll.SHOCK_TRUE) IsLagFrame = false;
			if (pad2 == OctoshockDll.SHOCK_TRUE) IsLagFrame = false;
			if (_Settings.GPULag)
				IsLagFrame = OctoshockDll.shock_GetGPUUnlagged(psx) != OctoshockDll.SHOCK_TRUE;
			if (IsLagFrame)
				LagCount++;

			//what happens to sound in this case?
			if (!render)
			{
				Frame++;
				return true;
			}

			OctoshockDll.ShockFramebufferInfo fb = new OctoshockDll.ShockFramebufferInfo();

			//run this once to get current logical size
			OctoshockDll.shock_GetFramebuffer(psx, ref fb);
			CurrentVideoSize = new System.Drawing.Size(fb.width, fb.height);

			if (_Settings.ResolutionMode == eResolutionMode.PixelPro)
				fb.flags = OctoshockDll.eShockFramebufferFlags.Normalize;

			OctoshockDll.shock_GetFramebuffer(psx, ref fb);

			int w = fb.width;
			int h = fb.height;
			BufferWidth = w;
			BufferHeight = h;

			var ri = CalculateResolution(this.SystemVidStandard, _Settings, w, h);
			VirtualWidth = ri.Resolution.Width;
			VirtualHeight = ri.Resolution.Height;
			VideoProvider_Padding = ri.Padding;

			int len = w * h;
			if (frameBuffer.Length != len)
			{
				Console.WriteLine("PSX FB size: {0},{1}", fb.width, fb.height);
				frameBuffer = new int[len];
			}

			fixed (int* ptr = frameBuffer)
			{
				fb.ptr = ptr;
				OctoshockDll.shock_GetFramebuffer(psx, ref fb);
			}

			fixed (short* samples = sbuff)
			{
				sbuffcontains = OctoshockDll.shock_GetSamples(psx, null);
				if (sbuffcontains * 2 > sbuff.Length) throw new InvalidOperationException($"{nameof(OctoshockDll.shock_GetSamples)} returned too many samples: {sbuffcontains}");
				OctoshockDll.shock_GetSamples(psx, samples);
			}

			Frame++;

			return true;
		}

		public ControllerDefinition ControllerDefinition { get; private set; }

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get => throw new NotImplementedException();
		}

		public bool DeterministicEmulation => true;

		public int[] GetVideoBuffer() => frameBuffer;
		public int VirtualWidth { get; private set; }
		public int VirtualHeight { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => 0;
		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }

		public System.Drawing.Size VideoProvider_Padding { get; private set; }

		//private short[] sbuff = new short[1454 * 2]; //this is the most ive ever seen.. don't know why. two frames worth i guess
		private readonly short[] sbuff = new short[1611 * 2]; //need this for pal
		private int sbuffcontains = 0;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = sbuff;
			nsamp = sbuffcontains;
		}

		public void DiscardSamples()
		{
			sbuffcontains = 0;
		}

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public byte[] CloneSaveRam()
		{
			var cfg = _SyncSettings.FIOConfig.ToLogical();
			int nMemcards = cfg.NumMemcards;
			var buf = new byte[128 * 1024 * nMemcards];
			for (int i = 0, idx = 0, addr=0x01; i < 2; i++, addr<<=1)
			{
				if (cfg.Memcards[i])
				{
					fixed (byte* pbuf = buf)
					{
						var transaction = new OctoshockDll.ShockMemcardTransaction();
						transaction.buffer128k = pbuf + idx * 128 * 1024;
						transaction.transaction = OctoshockDll.eShockMemcardTransaction.Read;
						OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction);
						idx++;
					}
				}
			}
			return buf;
		}

		public void StoreSaveRam(byte[] data)
		{
			var cfg = _SyncSettings.FIOConfig.ToLogical();
			for (int i = 0, idx = 0, addr = 0x01; i < 2; i++, addr <<= 1)
			{
				if (cfg.Memcards[i])
				{
					fixed (byte* pbuf = data)
					{
						var transaction = new OctoshockDll.ShockMemcardTransaction();
						transaction.buffer128k = pbuf + idx * 128 * 1024;
						transaction.transaction = OctoshockDll.eShockMemcardTransaction.Write;
						OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction);
						idx++;
					}
				}
			}
		}

		public bool SaveRamModified
		{
			get
			{
				var cfg = _SyncSettings.FIOConfig.ToLogical();
				for (int i = 0, addr = 0x01; i < 2; i++, addr <<= 1)
				{
					if (cfg.Memcards[i])
					{
						var transaction = new OctoshockDll.ShockMemcardTransaction();
						transaction.transaction = OctoshockDll.eShockMemcardTransaction.CheckDirty;
						OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction);
						if (OctoshockDll.shock_Peripheral_MemcardTransact(psx, addr, ref transaction) == OctoshockDll.SHOCK_TRUE)
							return true;
					}
				}

				return false;
			}
		}


		//THIS IS STILL AWFUL

		private byte[] savebuff;

		private void StudySaveBufferSize()
		{
			var transaction = new OctoshockDll.ShockStateTransaction();
			transaction.transaction = OctoshockDll.eShockStateTransaction.BinarySize;
			int size = OctoshockDll.shock_StateTransaction(psx, ref transaction);
			savebuff = new byte[size];
		}

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			fixed (byte* psavebuff = savebuff)
			{
				var transaction = new OctoshockDll.ShockStateTransaction()
				{
					transaction = OctoshockDll.eShockStateTransaction.BinarySave,
					buffer = psavebuff,
					bufferLength = savebuff.Length
				};

				int result = OctoshockDll.shock_StateTransaction(psx, ref transaction);
				if (result != OctoshockDll.SHOCK_OK)
					throw new InvalidOperationException($"{nameof(OctoshockDll.eShockStateTransaction)}.{nameof(OctoshockDll.eShockStateTransaction.BinarySave)} returned error!");
				writer.Write(savebuff.Length);
				writer.Write(savebuff);

				// other variables
				writer.Write(IsLagFrame);
				writer.Write(LagCount);
				writer.Write(Frame);
				writer.Write(CurrentTrayOpen);
				writer.Write(CurrentDiscIndexMounted);
			}
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			fixed (byte* psavebuff = savebuff)
			{
				var transaction = new OctoshockDll.ShockStateTransaction()
				{
					transaction = OctoshockDll.eShockStateTransaction.BinaryLoad,
					buffer = psavebuff,
					bufferLength = savebuff.Length
				};

				int length = reader.ReadInt32();
				if (length != savebuff.Length)
					throw new InvalidOperationException("Save buffer size mismatch!");
				reader.Read(savebuff, 0, length);
				int ret = OctoshockDll.shock_StateTransaction(psx, ref transaction);
				if (ret != OctoshockDll.SHOCK_OK)
					throw new InvalidOperationException($"{nameof(OctoshockDll.eShockStateTransaction)}.{nameof(OctoshockDll.eShockStateTransaction.BinaryLoad)} returned error!");

				// other variables
				IsLagFrame = reader.ReadBoolean();
				LagCount = reader.ReadInt32();
				Frame = reader.ReadInt32();
				CurrentTrayOpen = reader.ReadBoolean();
				CurrentDiscIndexMounted = reader.ReadInt32();
				PokeDisc();
			}
		}

		private Settings _Settings = new Settings();
		private SyncSettings _SyncSettings;

		public enum eResolutionMode
		{
			PixelPro, Debug,
			Mednafen, TweakedMednafen
		}

		public class SyncSettings
		{
			public SyncSettings Clone()
			{
				return JsonSerializer.Deserialize<SyncSettings>(JsonSerializer.Serialize(this)); // what am I witnessing
			}

			public bool EnableLEC;

			public SyncSettings()
			{
				//initialize with single controller and memcard
				var user = new OctoshockFIOConfigUser();
				user.Memcards[0] = true;
				user.Memcards[1] = false;
				user.Multitaps[0] = user.Multitaps[0] = false;
				user.Devices8[0] = OctoshockDll.ePeripheralType.DualShock;
				user.Devices8[4] = OctoshockDll.ePeripheralType.None;
				FIOConfig = user;
			}

			public OctoshockFIOConfigUser FIOConfig;
		}

		public enum eHorizontalClipping
		{
			None,
			Basic,
			Framebuffer
		}

		public enum eDeinterlaceMode
		{
			Weave,
			Bob,
			BobOffset
		}

		[CoreSettings]
		public class Settings
		{
			[DisplayName("Determine Lag from GPU Frames")]
			[DefaultValue(false)]
			public bool GPULag { get; set; }

			[DisplayName("Resolution Mode")]
			[DefaultValue(eResolutionMode.PixelPro)]
			public eResolutionMode ResolutionMode { get; set; }

			[DisplayName("Horizontal Clipping")]
			[DefaultValue(eHorizontalClipping.None)]
			public eHorizontalClipping HorizontalClipping { get; set; }

			[DisplayName("ScanlineStart_NTSC")]
			[DefaultValue(0)]
			public int ScanlineStart_NTSC { get; set; }

			[DisplayName("ScanlineEnd_NTSC")]
			[DefaultValue(239)]
			public int ScanlineEnd_NTSC { get; set; }

			[DisplayName("ScanlineStart_PAL")]
			[DefaultValue(0)]
			public int ScanlineStart_PAL { get; set; }

			[DisplayName("ScanlineEnd_PAL")]
			[DefaultValue(287)]
			public int ScanlineEnd_PAL { get; set; }

			[DisplayName("DeinterlaceMode")]
			[DefaultValue(eDeinterlaceMode.Weave)]
			public eDeinterlaceMode DeinterlaceMode { get; set; }

			public void Validate()
			{
				if (ScanlineStart_NTSC < 0) ScanlineStart_NTSC = 0;
				if (ScanlineStart_PAL < 0) ScanlineStart_PAL = 0;
				if (ScanlineEnd_NTSC > 239) ScanlineEnd_NTSC = 239;
				if (ScanlineEnd_PAL > 287) ScanlineEnd_PAL = 287;

				//make sure theyre not in the wrong order
				if (ScanlineEnd_NTSC < ScanlineStart_NTSC)
				{
					int temp = ScanlineEnd_NTSC;
					ScanlineEnd_NTSC = ScanlineStart_NTSC;
					ScanlineStart_NTSC = temp;
				}
				if (ScanlineEnd_PAL < ScanlineStart_PAL)
				{
					int temp = ScanlineEnd_PAL;
					ScanlineEnd_PAL = ScanlineStart_PAL;
					ScanlineStart_PAL = temp;
				}
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public Settings GetSettings()
		{
			return _Settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _SyncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_Settings.Validate();
			_Settings = o;

			//TODO - store settings into core? or we can just keep doing it before frameadvance

			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			//currently LEC and pad settings changes both require reboot
			bool reboot = true;

			//we could do it this way roughly if we need to
			//if(JsonConvert.SerializeObject(o.FIOConfig) != JsonConvert.SerializeObject(_SyncSettings.FIOConfig)


			_SyncSettings = o;


			return reboot ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}
	}
}
