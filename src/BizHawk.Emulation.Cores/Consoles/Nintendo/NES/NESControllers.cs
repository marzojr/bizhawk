using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
	 * This file covers all NES and Famicom controller related stuff.
	 * It supports (or could be easily made to support by adding a new class) every existing
	 * controller device I know about.  It does not support some things that were theoretically
	 * possible with the electronic interface available, but never used.
	 */

	/// <summary>
	/// callback type for PPU to tell if there's light for a lightgun to detect
	/// </summary>
	/// <param name="x">x coordinate on screen</param>
	/// <param name="y">y coordinate on screen</param>
	/// <returns>true if there is light</returns>
	public delegate bool LightgunDelegate(int x, int y);

	/// <summary>
	/// stores information about the strobe lines controlled by $4016
	/// </summary>
	public struct StrobeInfo
	{
		/// <summary>
		/// the current value of $4016.0; strobes regular controller ports
		/// </summary>
		public readonly int OUT0;
		/// <summary>
		/// the current value of $4016.1; strobes expansion port
		/// </summary>
		public readonly int OUT1;
		/// <summary>
		/// the current value of $4016.2; strobes expansion port
		/// </summary>
		public readonly int OUT2;
		/// <summary>
		/// the previous value or $4016.0 (for edge sensitive equipment)
		/// </summary>
		public readonly int OUT0old;
		/// <summary>
		/// the previous value or $4016.1 (for edge sensitive equipment)
		/// </summary>
		public readonly int OUT1old;
		/// <summary>
		/// the previous value or $4016.2 (for edge sensitive equipment)
		/// </summary>
		public readonly int OUT2old;

		/// <param name="oldValue">the old latched $4016 byte</param>
		/// <param name="newValue">the new latched $4016 byte</param>
		public StrobeInfo(byte oldValue, byte newValue)
		{
			OUT0old = oldValue & 1;
			OUT1old = oldValue >> 1 & 1;
			OUT2old = oldValue >> 2 & 1;
			OUT0 = newValue & 1;
			OUT1 = newValue >> 1 & 1;
			OUT2 = newValue >> 2 & 1;
		}
	}

	/// <summary>
	/// the main system deck, handling all $4016 writes and $4016/$4017 reads
	/// </summary>
	public interface IControllerDeck
	{
		/// <remarks>
		/// implementations create a single <see cref="ControllerDefinition"/> in their ctors and will always return a reference to it;
		/// caller may mutate it
		/// </remarks>
		ControllerDefinition ControllerDef { get; }

		/// <summary>
		/// call whenever $4016 is written
		/// </summary>
		void Strobe(StrobeInfo s, IController c);
		/// <summary>
		/// call whenever $4016 is read
		/// </summary>
		/// <returns>bits 0-4 are valid</returns>
		byte ReadA(IController c); // D0:D4
		/// <summary>
		/// call whenever $4017 is read
		/// </summary>
		/// <returns>bits 0-4 are valid</returns>
		byte ReadB(IController c); // D0:D4

		void SyncState(Serializer ser);
	}

	/// <summary>
	/// a peripheral that plugs into the famicom expansion port
	/// </summary>
	public interface IFamicomExpansion
	{
		ControllerDefinition ControllerDefFragment { get; }

		void Strobe(StrobeInfo s, IController c);
		/// <summary>
		/// read data from $4016
		/// </summary>
		/// <returns>only bit 1 is valid</returns>
		byte ReadA(IController c);
		/// <summary>
		/// read data from $4017
		/// </summary>
		/// <returns>bits 1-4 are valid</returns>
		byte ReadB(IController c);

		void SyncState(Serializer ser);
	}

	/// <summary>
	/// a peripheral that plugs into either of the two NES controller ports
	/// </summary>
	public interface INesPort
	{
		ControllerDefinition ControllerDefFragment { get; }

		void Strobe(StrobeInfo s, IController c); // only uses OUT0
		byte Read(IController c); // only uses D0, D3, D4

		void SyncState(Serializer ser);
	}

	/// <summary>
	/// a NES or AV famicom, with two attached devices
	/// </summary>
	public class NesDeck : IControllerDeck
	{
		private readonly INesPort _left;
		private readonly INesPort _right;
		private readonly ControlDefUnMerger _leftU;
		private readonly ControlDefUnMerger _rightU;

		public ControllerDefinition ControllerDef { get; }

		public NesDeck(INesPort left, INesPort right, LightgunDelegate ppuCallback)
		{
			_left = left;
			_right = right;
			ControllerDef = ControllerDefinitionMerger.GetMerged(
				"NES Controller",
				new[] { left.ControllerDefFragment, right.ControllerDefFragment },
				out var cdum);
			_leftU = cdum[0];
			_rightU = cdum[1];

			// apply hacks
			// if this list gets very long, then something should be changed
			// if it stays short, then no problem
			if (left is FourScore leftScore)
				leftScore.RightPort = false;
			if (right is FourScore rightScore)
				rightScore.RightPort = true;
			if (left is IZapper leftZapper)
				leftZapper.PPUCallback = ppuCallback;
			if (right is IZapper rightZapper)
				rightZapper.PPUCallback = ppuCallback;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_left.Strobe(s, _leftU.UnMerge(c));
			_right.Strobe(s, _rightU.UnMerge(c));
		}

		public byte ReadA(IController c)
		{
			return (byte)(_left.Read(_leftU.UnMerge(c)) & 0x19);
		}

		public byte ReadB(IController c)
		{
			return (byte)(_right.Read(_rightU.UnMerge(c)) & 0x19);
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(_left));
			_left.SyncState(ser);
			ser.EndSection();
			ser.BeginSection(nameof(_right));
			_right.SyncState(ser);
			ser.EndSection();
		}
	}

	public class UnpluggedNES : INesPort
	{
		public ControllerDefinition ControllerDefFragment { get; } = new("(NES Controller fragment)");

		public void Strobe(StrobeInfo s, IController c)
		{
		}

		public byte Read(IController c)
		{
			return 0;
		}

		public void SyncState(Serializer ser)
		{
		}
	}

	/// <summary>
	/// a NES controller; also used internally to represent the two famicom controllers
	/// </summary>
	public class ControllerNES : INesPort
	{
		private bool _resetting;
		private int _latchedValue;

		private static readonly string[] Buttons =
		{
			"0A", "0B", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right"
		};

		private static readonly string[] FamicomP2Buttons =
		{
			"0A", "0B", null, null, "0Up", "0Down", "0Left", "0Right"
		};

		private readonly bool _famicomP2Hack;

		public ControllerDefinition ControllerDefFragment { get; }

		public ControllerNES()
		{
			ControllerDefFragment = new("(NES Controller fragment)") { BoolButtons = Buttons.OrderBy(x => _buttonOrdinals[x]).ToList() };
		}


		private readonly Dictionary<string, int> _buttonOrdinals = new Dictionary<string, int>
		{
			{ "0Up", 1 },
			{ "0Down", 2 },
			{ "0Left", 3 },
			{ "0Right", 4 },
			{ "0Start", 5 },
			{ "0Select", 6 },
			{ "0B", 7 },
			{ "0A", 8 },
		};

		public ControllerNES(bool famicomP2)
		{
			if (famicomP2)
			{
				ControllerDefFragment = new("(NES Controller fragment)") { BoolButtons = FamicomP2Buttons.Where(static s => s is not null).OrderBy(x => _buttonOrdinals[x]).ToList() };
			}
			else
			{
				ControllerDefFragment = new("(NES Controller fragment)") { BoolButtons = Buttons.OrderBy(x => _buttonOrdinals[x]).ToList() };
			}

			_famicomP2Hack = famicomP2;
		}

		// reset is not edge triggered; so long as it's high, the latch is continuously reloading
		// so we need to latch in two places:
		// 1. when OUT0 goes low, to get the last set
		// 2. when even reading with OUT0 high, since new data for controller is always loading

		private void Latch(IController c)
		{
			_latchedValue = SerialUtil.Latch(_famicomP2Hack ? FamicomP2Buttons : Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (_resetting)
				Latch(c);
			byte ret = (byte)(_latchedValue & 1);
			if (!_resetting)
				_latchedValue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}
	}

	/// <summary>
	/// a SNES controller plugged into a NES? heresy
	/// </summary>
	public class ControllerSNES : INesPort
	{
		private bool _resetting;
		private int _latchedValue;

		private static readonly string[] Buttons =
		{
			"0B", "0Y", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right",
			"0A", "0X", "0L", "0R", null, null, null, null // 4 0s at end
		};

		public ControllerDefinition ControllerDefFragment { get; }
			= new("(NES Controller fragment)") { BoolButtons = Buttons.Where(static s => s is not null).ToList() };

		// reset is not edge triggered; so long as it's high, the latch is continuously reloading
		// so we need to latch in two places:
		// 1. when OUT0 goes low, to get the last set
		// 2. when even reading with OUT0 high, since new data for controller is always loading

		private void Latch(IController c)
		{
			_latchedValue = SerialUtil.Latch(Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (_resetting)
				Latch(c);
			byte ret = (byte)(_latchedValue & 1);
			if (!_resetting)
				_latchedValue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}
	}

	/// <summary>
	/// vaus paddle, the NES (not famicom) version
	/// </summary>
	public class ArkanoidNES : INesPort
	{
		private int _shiftidx;
		private bool _resetting;
		private byte _latchedValue = 0x54 ^ 0xff;

		public ControllerDefinition ControllerDefFragment { get; }
			= new ControllerDefinition("(NES Controller fragment)") { BoolButtons = { "0Fire" } }
				.AddAxis("0Paddle", 0.RangeTo(160), 80);

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (_resetting)
				_shiftidx = 0;
			if (s.OUT0 > s.OUT0old)
			{
				_latchedValue = (byte)(0x54 + c.AxisValue("0Paddle"));
				_latchedValue ^= 0xff;
			}
		}

		public byte Read(IController c)
		{
			byte ret = c.IsPressed("0Fire") ? (byte)0x08 : (byte)0x00;
			if (_resetting)
				return ret;

			byte value = _latchedValue;
			value <<= _shiftidx;
			ret |= (byte)(value >> 3 & 0x10);
			_shiftidx++;
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_shiftidx), ref _shiftidx);
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}
	}

	public class FourScore : INesPort
	{
		// fourscore is actually one two port thing
		// we emulate it as two separate halves
		// each one behaves slightly differently
		public bool RightPort;

		private static readonly string[] Buttons =
		{
			"0A", "0B", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right",
			"1A", "1B", "1Select", "1Start", "1Up", "1Down", "1Left", "1Right",
		};

		public ControllerDefinition ControllerDefFragment { get; }
			= new("(NES Controller fragment)") { BoolButtons = Buttons.ToList() };

		private bool _resetting;
		private int _latchedValue;

		private void Latch(IController c)
		{
			_latchedValue = SerialUtil.Latch(Buttons, c);
			// set signatures
			_latchedValue &= ~0xff0000;
			if (RightPort) // signatures
				_latchedValue |= 0x040000;
			else
				_latchedValue |= 0x080000;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (_resetting)
				Latch(c);
			byte ret = (byte)(_latchedValue & 1);
			if (!_resetting)
				_latchedValue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}
	}

	public class PowerPad : INesPort
	{
		private static readonly string[] D3Buttons = { "0PP2", "0PP1", "0PP5", "0PP9", "0PP6", "0PP10", "0PP11", "0PP7" };
		private static readonly string[] D4Buttons = { "0PP4", "0PP3", "0PP12", "0PP8" };

		public ControllerDefinition ControllerDefFragment { get; }
			= new("(NES Controller fragment)") { BoolButtons = D3Buttons.Concat(D4Buttons).ToList() };

		private bool _resetting;
		private int _latched3;
		private int _latched4;

		private void Latch(IController c)
		{
			_latched3 = SerialUtil.Latch(D3Buttons, c);
			_latched4 = SerialUtil.Latch(D4Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (_resetting)
				Latch(c);
			int d3 = _latched3 & 1;
			int d4 = _latched4 & 1;
			if (!_resetting)
			{
				_latched3 >>= 1; // ASR not LSR, so endless stream of 1s after data
				_latched4 >>= 1;
			}
			return (byte)(d3 << 3 | d4 << 4);
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latched3), ref _latched3);
			ser.Sync(nameof(_latched4), ref _latched4);
		}
	}

	internal static class NESControllerDefExtensions
	{
		public static ControllerDefinition AddZapper(this ControllerDefinition def, string nameFormat)
			=> def.AddXYPair(nameFormat, AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(239), 120); //TODO verify direction against hardware
	}

	// Dummy interface to indicate zapper behavior, used as a means of type checking for zapper functionality
	public interface IZapper
	{
		LightgunDelegate PPUCallback { get; set; }
	}

	public class Zapper : INesPort, IFamicomExpansion, IZapper
	{
		/// <summary>
		/// returns true if light was detected at the ppu coordinates specified
		/// </summary>
		public LightgunDelegate PPUCallback { get; set; }

		public ControllerDefinition ControllerDefFragment { get; }
			= new ControllerDefinition("(NES Controller fragment)") { BoolButtons = { "0Fire" } }
				.AddZapper("0Zapper {0}");

		public void Strobe(StrobeInfo s, IController c)
		{
		}

		// NES controller port interface
		public byte Read(IController c)
		{
			byte ret = 0;
			if (c.IsPressed("0Fire"))
				ret |= 0x10;
			if (!PPUCallback(c.AxisValue("0Zapper X"), c.AxisValue("0Zapper Y")))
				ret |= 0x08;
			return ret;
		}

		public void SyncState(Serializer ser)
		{
		}

		// famicom expansion hookups
		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			return Read(c);
		}
	}

	public class VSZapper : INesPort, IZapper
	{
		/// <summary>
		/// returns true if light was detected at the ppu coordinates specified
		/// </summary>
		public LightgunDelegate PPUCallback { get; set; }

		private bool _resetting;
		private uint _latchedValue;

		public ControllerDefinition ControllerDefFragment { get; }
			= new ControllerDefinition("(NES Controller fragment)") { BoolButtons = { "0Fire" } }
				.AddZapper("0Zapper {0}");

		private void Latch(IController c)
		{
			byte ret = 0;
			if (c.IsPressed("0Fire"))
				ret |= 0x80;
			if (PPUCallback(c.AxisValue("0Zapper X"), c.AxisValue("0Zapper Y")))
				ret |= 0x40;

			ret |= 0x10; // always 1
			_latchedValue = ret;
			_latchedValue |= 0xFFFFFF00;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		// NES controller port interface
		public byte Read(IController c)
		{
			if (_resetting)
				Latch(c);
			byte ret = (byte)(_latchedValue & 1);
			if (!_resetting)
				_latchedValue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}

		// famicom expansion hookups
		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			return Read(c);
		}
	}

	public class FamicomDeck : IControllerDeck
	{
		// two NES controllers are maintained internally
		private readonly INesPort _player1 = new ControllerNES(false);
		private readonly INesPort _player2 = new ControllerNES(true);
		private readonly IFamicomExpansion _player3;

		private readonly ControlDefUnMerger _player1U;
		private readonly ControlDefUnMerger _player2U;
		private readonly ControlDefUnMerger _player3U;

		public ControllerDefinition ControllerDef { get; }

		public FamicomDeck(IFamicomExpansion expSlot, LightgunDelegate ppuCallback)
		{
			_player3 = expSlot;
			ControllerDef = ControllerDefinitionMerger.GetMerged(
				"NES Controller",
				new[] { _player1.ControllerDefFragment, _player2.ControllerDefFragment, _player3.ControllerDefFragment },
				out var cdum);
			ControllerDef.BoolButtons.Add("P2 Microphone");
			_player1U = cdum[0];
			_player2U = cdum[1];
			_player3U = cdum[2];

			// hack
			if (_player3 is Zapper zapper)
				zapper.PPUCallback = ppuCallback;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_player1.Strobe(s, _player1U.UnMerge(c));
			_player2.Strobe(s, _player2U.UnMerge(c));
			_player3.Strobe(s, _player3U.UnMerge(c));
		}

		public byte ReadA(IController c)
		{
			byte ret = 0;
			ret |= (byte)(_player1.Read(_player1U.UnMerge(c)) & 1);
			ret |= (byte)(_player3.ReadA(_player3U.UnMerge(c)) & 2);
			if (c.IsPressed("P2 Microphone"))
				ret |= 4;
			return ret;
		}

		public byte ReadB(IController c)
		{
			byte ret = 0;
			ret |= (byte)(_player2.Read(_player2U.UnMerge(c)) & 1);
			ret |= (byte)(_player3.ReadB(_player3U.UnMerge(c)) & 30);
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Left");
			_player1.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Right");
			_player2.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Expansion");
			_player3.SyncState(ser);
			ser.EndSection();
		}
	}

	/// <summary>
	/// vaus controller that plugs into a famicom's expansion port
	/// </summary>
	public class ArkanoidFam : IFamicomExpansion
	{
		private int _shiftidx;
		private bool _resetting;
		private byte _latchedValue = 0x54 ^ 0xff;

		public ControllerDefinition ControllerDefFragment { get; }
			= new ControllerDefinition("(NES Controller fragment)") { BoolButtons = { "0Fire" } }
				.AddAxis("0Paddle", 0.RangeTo(160), 80);

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (_resetting)
				_shiftidx = 0;
			if (s.OUT0 > s.OUT0old)
			{
				_latchedValue = (byte)(0x54 + c.AxisValue("0Paddle"));
				_latchedValue ^= 0xff;
			}
		}

		public byte ReadA(IController c)
		{
			return c.IsPressed("0Fire") ? (byte)0x02 : (byte)0x00;
		}

		public byte ReadB(IController c)
		{
			byte ret = 0;
			if (_resetting)
				return ret;

			byte value = _latchedValue;
			value <<= _shiftidx;
			ret |= (byte)(value >> 6 & 0x02);
			_shiftidx++;
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_shiftidx), ref _shiftidx);
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}
	}

	public class FamilyBasicKeyboard : IFamicomExpansion
	{
		private static readonly string[] Buttons =
		{
			"0]",
			"0[",
			"0RETURN",
			"0F8",
			"0STOP",
			"0¥",
			"0RSHIFT",
			"0カナ",

			"0;",
			"0:",
			"0@",
			"0F7",
			"0^",
			"0-",
			"0/",
			"0_",

			"0K",
			"0L",
			"0O",
			"0F6",
			"00",
			"0P",
			"0,",
			"0.",

			"0J",
			"0U",
			"0I",
			"0F5",
			"08",
			"09",
			"0N",
			"0M",

			"0H",
			"0G",
			"0Y",
			"0F4",
			"06",
			"07",
			"0V",
			"0B",

			"0D",
			"0R",
			"0T",
			"0F3",
			"04",
			"05",
			"0C",
			"0F",

			"0A",
			"0S",
			"0W",
			"0F2",
			"03",
			"0E",
			"0Z",
			"0X",

			"0CTR",
			"0Q",
			"0ESC",
			"0F1",
			"02",
			"01",
			"0GRPH",
			"0LSHIFT",

			"0LEFT",
			"0RIGHT",
			"0UP",
			"0CLR",
			"0INS",
			"0DEL",
			"0SPACE",
			"0DOWN",

		};

		public ControllerDefinition ControllerDefFragment { get; }
			= new("(NES Controller fragment)") { BoolButtons = Buttons.ToList() };

		private bool _active;
		private int _column;
		private int _row;

		public void Strobe(StrobeInfo s, IController c)
		{
			_active = s.OUT2 != 0;
			_column = s.OUT1;
			if (s.OUT1 < s.OUT1old)
			{
				_row++;
				if (_row == 10)
					_row = 0;
			}
			if (s.OUT0 != 0)
				_row = 0;
		}

		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			if (!_active)
				return 0;
			if (_row == 9) // empty last row
				return 0;
			int idx = _row * 8 + _column * 4;

			byte ret = 0x1E;

			if (c.IsPressed(Buttons[idx])) ret &= 0x0E;
			if (c.IsPressed(Buttons[idx + 1])) ret &= 0x16;
			if (c.IsPressed(Buttons[idx + 2])) ret &= 0x1A;
			if (c.IsPressed(Buttons[idx + 3])) ret &= 0x1C;

			// nothing is clocked here
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_active), ref _active);
			ser.Sync(nameof(_column), ref _column);
			ser.Sync(nameof(_row), ref _row);
		}
	}

	public class Famicom4P : IFamicomExpansion
	{
		private static readonly string[] P1Buttons =
		{
			"0A", "0B", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right"
		};

		private static readonly string[] P2Buttons =
		{
			"1A", "1B", "1Select", "1Start", "1Up", "1Down", "1Left", "1Right",
		};

		public ControllerDefinition ControllerDefFragment { get; }
			= new("(NES Controller fragment)") { BoolButtons = P1Buttons.Concat(P2Buttons).ToList() };

		private bool _resetting;
		private int _latchedP1;
		private int _latchedP2;

		private void Latch(IController c)
		{
			_latchedP1 = SerialUtil.Latch(P1Buttons, c);
			_latchedP2 = SerialUtil.Latch(P2Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte ReadA(IController c)
		{
			if (_resetting)
				Latch(c);
			byte ret = (byte)(_latchedP1 << 1 & 2);
			if (!_resetting)
				_latchedP1 >>= 1;
			return ret;
		}

		public byte ReadB(IController c)
		{
			if (_resetting)
				Latch(c);
			byte ret = (byte)(_latchedP2 << 1 & 2);
			if (!_resetting)
				_latchedP2 >>= 1;
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_latchedP1), ref _latchedP1);
			ser.Sync(nameof(_latchedP2), ref _latchedP2);
		}
	}

	public class OekaKids : IFamicomExpansion
	{
		public ControllerDefinition ControllerDefFragment { get; }
			= new ControllerDefinition("(NES Controller fragment)") { BoolButtons = { "0Click", "0Touch" } }
				.AddZapper("0Pen {0}"); // why would a tablet have the same resolution as a CRT monitor? --yoshi

		private bool _resetting;
		private int _shiftidx;
		private int _latchedValue;

		public void Strobe(StrobeInfo s, IController c)
		{
			_resetting = s.OUT0 == 0;
			if (s.OUT0 < s.OUT0old) // H->L: latch
			{
				int x = c.AxisValue("0Pen X");
				int y = c.AxisValue("0Pen Y");
				// http://forums.nesdev.com/viewtopic.php?p=19454#19454
				// it almost feels like the hardware guys got the request for 
				// a tablet that returned x in [0, 255] and y in [0, 239] and then
				// accidentally flipped the whole thing sideways
				x = (x + 8) * 240 / 256;
				y = (y - 14) * 256 / 240;
				x &= 255;
				y &= 255;
				_latchedValue = x << 10 | y << 2;
				if (c.IsPressed("0Touch"))
					_latchedValue |= 2;
				if (c.IsPressed("0Click"))
					_latchedValue |= 1;
			}
			if (s.OUT0 > s.OUT0old) // L->H: reset shift
				_shiftidx = 0;
			if (s.OUT1 > s.OUT1old) // L->H: increment shift
				_shiftidx++;
		}

		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			byte ret = (byte)(_resetting ? 2 : 0);
			if (_resetting)
				return ret;

			// the shiftidx = 0 read is one off the end
			int bit = _latchedValue >> (16 - _shiftidx);
			bit &= 4;
			bit ^= 4; // inverted data
			ret |= (byte)(bit);
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_resetting), ref _resetting);
			ser.Sync(nameof(_shiftidx), ref _shiftidx);
			ser.Sync(nameof(_latchedValue), ref _latchedValue);
		}
	}

	public class UnpluggedFam : IFamicomExpansion
	{
		public ControllerDefinition ControllerDefFragment => new("(NES Controller fragment)");

		public void Strobe(StrobeInfo s, IController c)
		{
		}

		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			return 0;
		}

		public void SyncState(Serializer ser)
		{
		}
	}

	public static class SerialUtil
	{
		public static int Latch(string[] values, IController c)
		{
			int ret = 0;
			for (int i = 0; i < 32; i++)
			{
				if (values.Length > i)
				{
					if (values[i] != null && c.IsPressed(values[i]))
						ret |= 1 << i;
				}
				else
				{
					// 1 in all other bits
					ret |= 1 << i;
				}
			}
			return ret;
		}
	}

	public class NESControlSettings
	{
		private static readonly Dictionary<string, Type> FamicomExpansions;
		private static readonly Dictionary<string, Type> NesPortDevices;

		private static Dictionary<string, Type> Implementors<T>() => ReflectionCache.Types
			.Where(c => typeof(T).IsAssignableFrom(c) && !c.IsAbstract && !c.IsInterface)
			.ToDictionary(t => t.Name, t => t);

		static NESControlSettings()
		{
			FamicomExpansions = Implementors<IFamicomExpansion>();
			NesPortDevices = Implementors<INesPort>();
		}

		public static IList<string> GetFamicomExpansionValues()
		{
			return new List<string>(FamicomExpansions.Keys).AsReadOnly();
		}
		public static IList<string> GetNesPortValues()
		{
			return new List<string>(NesPortDevices.Keys).AsReadOnly();
		}

		private bool _Famicom;
		public bool Famicom
		{
			get => _Famicom;
			set => _Famicom = value;
		}
		private string _NesLeftPort;
		private string _NesRightPort;
		public string NesLeftPort
		{
			get => _NesLeftPort;
			set
			{
				if (NesPortDevices.ContainsKey(value))
					_NesLeftPort = value;
				else
					throw new InvalidOperationException();
			}
		}
		public string NesRightPort
		{
			get => _NesRightPort;
			set
			{
				if (NesPortDevices.ContainsKey(value))
					_NesRightPort = value;
				else
					throw new InvalidOperationException();
			}
		}
		private string _FamicomExpPort;
		public string FamicomExpPort
		{
			get => _FamicomExpPort;
			set
			{
				if (FamicomExpansions.ContainsKey(value))
					_FamicomExpPort = value;
				else
					throw new InvalidOperationException();
			}
		}

		public NESControlSettings()
		{
			Famicom = false;
			FamicomExpPort = nameof(UnpluggedFam);
			NesLeftPort = nameof(ControllerNES);
			NesRightPort = nameof(UnpluggedNES);
		}

		public static bool NeedsReboot(NESControlSettings x, NESControlSettings y)
		{
			return !DeepEquality.DeepEquals(x, y);
		}

		public NESControlSettings Clone()
		{
			return (NESControlSettings)MemberwiseClone();
		}

		public IControllerDeck Instantiate(LightgunDelegate ppuCallback)
		{
			if (Famicom)
			{
				IFamicomExpansion exp = (IFamicomExpansion)Activator.CreateInstance(FamicomExpansions[FamicomExpPort]);
				IControllerDeck ret = new FamicomDeck(exp, ppuCallback);
				return ret;
			}
			else
			{
				INesPort left = (INesPort)Activator.CreateInstance(NesPortDevices[NesLeftPort]);
				INesPort right = (INesPort)Activator.CreateInstance(NesPortDevices[NesRightPort]);
				IControllerDeck ret = new NesDeck(left, right, ppuCallback);
				return ret;
			}
		}
	}
}
