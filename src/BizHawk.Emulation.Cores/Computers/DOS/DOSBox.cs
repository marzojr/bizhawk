using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	[PortedCore(
		name: CoreNames.DOSBox,
		author: "Jonathan Campbell et al.",
		portedVersion: "2025.02.01 (324193b)",
		portedUrl: "https://github.com/TASEmulators/dosbox-x",
		isReleased: true)]
	public partial class DOSBox : WaterboxCore
	{
		private static readonly Configuration ConfigPAL = new Configuration
		{
			SystemId = VSystemID.Raw.DOS,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibDOSBox.PAL_WIDTH,
			DefaultHeight = LibDOSBox.PAL_HEIGHT,
			MaxWidth = LibDOSBox.PAL_WIDTH,
			MaxHeight = LibDOSBox.PAL_HEIGHT,
			DefaultFpsNumerator = LibDOSBox.VIDEO_NUMERATOR_PAL,
			DefaultFpsDenominator = LibDOSBox.VIDEO_DENOMINATOR_PAL
		};

		private static readonly Configuration ConfigNTSC = new Configuration
		{
			SystemId = VSystemID.Raw.DOS,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibDOSBox.NTSC_WIDTH,
			DefaultHeight = LibDOSBox.NTSC_HEIGHT,
			// games never switch region, and video dumping won't be happy, but amiga can still do it
			MaxWidth = LibDOSBox.PAL_WIDTH,
			MaxHeight = LibDOSBox.PAL_HEIGHT,
			DefaultFpsNumerator = LibDOSBox.VIDEO_NUMERATOR_NTSC,
			DefaultFpsDenominator = LibDOSBox.VIDEO_DENOMINATOR_NTSC
		};
		
		private readonly LibWaterboxCore.EmptyCallback _ledCallback;
		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;
		private const int _driveNullOrEmpty = -1;
		private int[] _driveSlots;
		private List<string> _args;
		private int _currentDrive;
		private int _currentSlot;
		private bool _ejectPressed;
		private bool _insertPressed;
		private bool _nextSlotPressed;
		private bool _nextDrivePressed;
		private int _correctedWidth;
		private string _chipsetCompatible = "";
		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		public override int VirtualWidth => _correctedWidth;

		private void LEDCallback()
		{
			DriveLightOn = true;
		}

		[CoreConstructor(VSystemID.Raw.DOS)]
		public DOSBox(CoreLoadParameters<object, UAESyncSettings> lp)
			: base(lp.Comm, lp.SyncSettings?.Region is VideoStandard.NTSC ? ConfigNTSC : ConfigPAL)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();
			_syncSettings.FloppyDrives = Math.Min(LibDOSBox.MAX_FLOPPIES, _syncSettings.FloppyDrives);
			DeterministicEmulation = lp.DeterministicEmulationRequested || _syncSettings.FloppySpeed is FloppySpeed._100;
			var filesToRemove = new List<string>();

			_ports = [
				_syncSettings.ControllerPort1,
				_syncSettings.ControllerPort2
			];
			_driveSlots = Enumerable.Repeat(_driveNullOrEmpty, LibDOSBox.MAX_FLOPPIES).ToArray();
			DriveLightEnabled = _syncSettings.FloppyDrives > 0;

			UpdateVideoStandard(true);
			CreateArguments(_syncSettings);
			ControllerDefinition = CreateControllerDefinition(_syncSettings);
			_ledCallback = LEDCallback;

			var dosbox = PreInit<LibDOSBox>(new WaterboxOptions
			{
				Filename = "dosbox.wbx",
				SbrkHeapSizeKB = 4 * 1024 * 32,
				SealedHeapSizeKB = 32 * 512,
				InvisibleHeapSizeKB = 32 * 512,
				PlainHeapSizeKB = 4 * 1024 * 32,
				MmapHeapSizeKB = 4 * 1024 * 32,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _ledCallback });

			// Adding default config file
			var baseConfFile = Zstd.DecompressZstdStream(new MemoryStream(Resources.DOSBOX_BASE_CONF.Value)).ToArray();
			_exe.AddReadonlyFile(baseConfFile, FileNames.DOSBOX_BASE_CONF);

			// Adding default HDD file
			var hddImageFile = Zstd.DecompressZstdStream(new MemoryStream(Resources.DOSBOX_HDD_IMAGE_FAT16_21MB.Value)).ToArray();
			_exe.AddReadonlyFile(hddImageFile, FileNames.HD);

			for (var index = 0; index < lp.Roms.Count; index++)
			{
				var rom = lp.Roms[index];

				Console.WriteLine("Adding ROM File: {0}", rom.RomPath);

				_exe.AddReadonlyFile(rom.FileData, FileNames.FD + index + ".ima");
				if (index < _syncSettings.FloppyDrives)
				{
					_driveSlots[index] = index;
					AppendSetting($"floppy{index}={FileNames.FD}{index}");
					AppendSetting($"floppy{index}type={(int) DriveType.DRV_35_DD}");
					AppendSetting("floppy_write_protect=true");
				}
			}

			//var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
			//	new(VSystemID.Raw.DOS, _chipsetCompatible),
			//	"Firmware files are required!");
			//_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			//filesToRemove.Add(kickstartInfo.Name);
			//_args.AddRange(
			//[
			//	"-r", kickstartInfo.Name
			//]);

			Console.WriteLine();
			Console.WriteLine(string.Join(" ", _args));
			Console.WriteLine();

			if (!dosbox.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();

			//dosbox.SetLEDCallback(_syncSettings.FloppyDrives > 0 ? _ledCallback : null);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var fi = new LibDOSBox.FrameInfo
			{
				Port1 = new LibDOSBox.ControllerState
				{
					Type = _ports[0],
					Buttons = 0
				},
				Port2 = new LibDOSBox.ControllerState
				{
					Type = _ports[1],
					Buttons = 0
				},
				Action = LibDOSBox.DriveAction.None
			};

			for (int port = 1; port <= 2; port++)
			{
				ref var currentPort = ref (port is 1 ? ref fi.Port1 : ref fi.Port2);

				switch (_ports[port - 1])
				{
					case LibDOSBox.ControllerType.DJoy:
						{
							foreach (var (name, button) in _joystickMap)
							{
								if (controller.IsPressed($"P{port} {Inputs.Joystick} {name}"))
								{
									currentPort.Buttons |= button;
								}
							}
							break;
						}
					case LibDOSBox.ControllerType.CD32Joy:
						{
							foreach (var (name, button) in _cd32padMap)
							{
								if (controller.IsPressed($"P{port} {Inputs.Cd32Pad} {name}"))
								{
									currentPort.Buttons |= button;
								}
							}
							break;
						}
					case LibDOSBox.ControllerType.Mouse:
						{
							if (controller.IsPressed($"P{port} {Inputs.MouseLeftButton}"))
							{
								currentPort.Buttons |= LibDOSBox.AllButtons.Button_1;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseRightButton}"))
							{
								currentPort.Buttons |= LibDOSBox.AllButtons.Button_2;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}"))
							{
								currentPort.Buttons |= LibDOSBox.AllButtons.Button_3;
							}

							currentPort.MouseX = controller.AxisValue($"P{port} {Inputs.MouseX}");
							currentPort.MouseY = controller.AxisValue($"P{port} {Inputs.MouseY}");
							break;
						}
				}
			}

			if (controller.IsPressed(Inputs.EjectDisk))
			{
				if (!_ejectPressed)
				{
					fi.Action = LibDOSBox.DriveAction.EjectDisk;
					if (_driveSlots[_currentDrive] == _driveNullOrEmpty)
					{
						CoreComm.Notify($"Drive FD{_currentDrive} is already empty!", _messageDuration);
					}
					else
					{
						CoreComm.Notify($"Ejected drive FD{_currentDrive}: {GetFullName(_roms[_driveSlots[_currentDrive]])}", _messageDuration);
						_driveSlots[_currentDrive] = _driveNullOrEmpty;
					}
				}
			}
			else if (controller.IsPressed(Inputs.InsertDisk))
			{
				if (!_insertPressed)
				{
					fi.Action = LibDOSBox.DriveAction.InsertDisk;
					unsafe
					{
						var str = FileNames.FD + _currentSlot;
						fixed (char* filename = str)
						{
							fixed (byte* buffer = fi.Name.Buffer)
							{
								Encoding.ASCII.GetBytes(filename, str.Length, buffer, LibDOSBox.FILENAME_MAXLENGTH);
							}
						}
					}
					_driveSlots[_currentDrive] = _currentSlot;
					CoreComm.Notify($"Insterted drive FD{_currentDrive}: {GetFullName(_roms[_driveSlots[_currentDrive]])}", _messageDuration);
				}
			}

			if (controller.IsPressed(Inputs.NextSlot))
			{
				if (!_nextSlotPressed)
				{
					_currentSlot++;
					_currentSlot %= _roms.Count;
					var selectedFile = _roms[_currentSlot];
					CoreComm.Notify($"Selected slot {_currentSlot}: {GetFullName(selectedFile)}", _messageDuration);
				}
			}

			if (controller.IsPressed(Inputs.NextDrive))
			{
				if (!_nextDrivePressed)
				{
					_currentDrive++;
					_currentDrive %= _syncSettings.FloppyDrives;
					string name = "";
					if (_driveSlots[_currentDrive] == _driveNullOrEmpty)
					{
						name = "empty";
					}
					else
					{
						name = GetFullName(_roms[_driveSlots[_currentDrive]]);
					}
					CoreComm.Notify($"Selected drive FD{_currentDrive}: {name}", _messageDuration);
				}
			}

			_ejectPressed = controller.IsPressed(Inputs.EjectDisk);
			_insertPressed = controller.IsPressed(Inputs.InsertDisk);
			_nextSlotPressed = controller.IsPressed(Inputs.NextSlot);
			_nextDrivePressed = controller.IsPressed(Inputs.NextDrive);			
			fi.CurrentDrive = _currentDrive;

			foreach (var (name, key) in _keyboardMap)
			{
				if (controller.IsPressed(name))
				{
					unsafe
					{
						fi.Keys.Buffer[(int)key] = 1;
					}
				}
			}

			return fi;
		}

		protected override void FrameAdvancePost()
		{
			UpdateVideoStandard(false);
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_ejectPressed);
			writer.Write(_insertPressed);
			writer.Write(_nextSlotPressed);
			writer.Write(_nextDrivePressed);
			writer.Write(_currentDrive);
			writer.Write(_currentSlot);
			writer.Write(_driveSlots[0]);
			writer.Write(_driveSlots[1]);
			writer.Write(_driveSlots[2]);
			writer.Write(_driveSlots[3]);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_ejectPressed = reader.ReadBoolean();
			_insertPressed = reader.ReadBoolean();
			_nextSlotPressed = reader.ReadBoolean();
			_nextDrivePressed = reader.ReadBoolean();
			_currentDrive = reader.ReadInt32();
			_currentSlot = reader.ReadInt32();
			_driveSlots[0] = reader.ReadInt32();
			_driveSlots[1] = reader.ReadInt32();
			_driveSlots[2] = reader.ReadInt32();
			_driveSlots[3] = reader.ReadInt32();
		}

		private void UpdateVideoStandard(bool initial)
		{
			var ntsc = initial
				? _syncSettings.Region is VideoStandard.NTSC
				: BufferHeight == LibDOSBox.NTSC_HEIGHT;

			if (ntsc)
			{
				_correctedWidth = LibDOSBox.PAL_WIDTH * 6 / 7;
				VsyncNumerator = LibDOSBox.VIDEO_NUMERATOR_NTSC;
				VsyncDenominator = LibDOSBox.VIDEO_DENOMINATOR_NTSC;
			}
			else
			{
				_correctedWidth = LibDOSBox.PAL_WIDTH;
				VsyncNumerator = LibDOSBox.VIDEO_NUMERATOR_PAL;
				VsyncDenominator = LibDOSBox.VIDEO_DENOMINATOR_PAL;
			}
		}

		private static class FileNames
		{
			public const string DOSBOX_BASE_CONF = "dosbox-x.conf";
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string HD = "HardDiskDrive";
		}
	}
}