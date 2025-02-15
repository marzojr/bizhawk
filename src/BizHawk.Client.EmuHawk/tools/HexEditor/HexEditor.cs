using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common.CollectionExtensions;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	// int to long TODO: 32 bit domains have more digits than the hex editor can account for and the address covers up the 0 column
	public partial class HexEditor : ToolFormBase, IToolFormAutoConfig
	{
		private sealed class N64MatrixDisplayDialog : Form
		{
			public N64MatrixDisplayDialog(IReadOnlyList<IReadOnlyList<string>> strings)
			{
				TableLayoutPanel tlp = new() { Size = new(352, 104) };
				const int SIZE = 4;
				for (var y = 0; y < SIZE; y++) tlp.RowStyles.Add(new());
				for (var x = 0; x < SIZE; x++) tlp.ColumnStyles.Add(new());
				for (var y = 0; y < SIZE; y++) for (var x = 0; x < SIZE; x++) tlp.Controls.Add(
					new SzTextBoxEx { ReadOnly = true, Size = new(80, 23), Text = strings[y][x] },
					row: y,
					column: x);
				SzButtonEx btnCopyTSV = new() { Size = new(128, 23), Text = ".tsv --> Clipboard" };
				btnCopyTSV.Click += (_, _) => Clipboard.SetText(string.Join(
					"\n",
					strings.Select(static l => string.Join("\t", l))));
				ClientSize = new(352, 144);
				SuspendLayout();
				Controls.Add(new SingleColumnFLP { Controls = { tlp, btnCopyTSV } });
				ResumeLayout();
			}
		}

		private class NullMemoryDomain : MemoryDomain
		{
			public override byte PeekByte(long addr) => 0;

			public override void PokeByte(long addr, byte val)
			{
			}

			public NullMemoryDomain()
			{
				EndianType = Endian.Unknown;
				Name = "Null";
				Size = 1024;
				Writable = true;
				WordSize = 1;
			}
		}

		private const string ROM_DOMAIN_NAME = "File on Disk";

		private static readonly FilesystemFilterSet BinFilesFSFilterSet = CreateBinaryDumpFSFilterSet("bin");

		private static readonly FilesystemFilterSet HexDumpsFSFilterSet = new(FilesystemFilter.TextFiles);

		private static readonly FilesystemFilterSet ImportableFSFilterSet = new(
			BinFilesFSFilterSet.Filters[0],
			new FilesystemFilter("Save Files", new[] { "sav" }));

		private static readonly FilesystemFilterSet TextTablesFSFilterSet = new(new FilesystemFilter("Text Table Files", new[] { "tbl" }));

		public static Icon ToolIcon
			=> Resources.PokeIcon;

		private static FilesystemFilterSet CreateBinaryDumpFSFilterSet(string ext)
			=> new(new FilesystemFilter("Binary", new[] { ext }));

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private readonly int _fontWidth;
		private readonly int _fontHeight;

		private readonly List<char> _nibbles = new List<char>();

		private long? _highlightedAddress;
		private readonly List<long> _secondaryHighlightedAddresses = new List<long>();

		private IEnumerable<long> AllHighlightedAddresses
			=> _highlightedAddress is long l
				? _secondaryHighlightedAddresses.Prepend(l)
				: _secondaryHighlightedAddresses;

		private bool AreAnyHighlighted
			=> _highlightedAddress is not null || _secondaryHighlightedAddresses.Count is not 0;

		private readonly Dictionary<int, char> _textTable = new Dictionary<int, char>();

		private int _rowsVisible;
		private int _numDigits = 4;
		private string _numDigitsStr = "{0:X4}";
		private string _digitFormatString = "{0:X2}";
		private long _addressOver = -1;
		
		private long _maxRow;

		private MemoryDomain _domain = new NullMemoryDomain();

		private string _findStr = "";
		private bool _mouseIsDown;
		private MemoryDomain _romDomain;
		private HexFind _hexFind;
		private string _lastRom = "";

		[ConfigPersist]
		private string LastDomain { get; set; }

		[ConfigPersist]
		private bool BigEndian { get; set; }

		[ConfigPersist]
		private int DataSize { get; set; }

		[ConfigPersist]
		private RecentFiles RecentTables { get; set; }

		internal class ColorConfig
		{
			public Color Background { get; set; } = SystemColors.Control;
			public Color Foreground { get; set; } = SystemColors.ControlText;
			public Color MenuBar { get; set; } = SystemColors.Control;
			public Color Freeze { get; set;  }= Color.LightBlue;
			public Color Highlight { get; set; } = Color.Pink;
			public Color HighlightFreeze { get; set; } = Color.Violet;
			public Color Foreground00 { get; set; } = Color.SlateGray;
		}

		[ConfigPersist]
		internal ColorConfig Colors { get; set; } = new ColorConfig();

		private WatchSize WatchSize => (WatchSize)DataSize;

		private readonly Pen _blackPen = Pens.Black;

		private SolidBrush _freezeBrush;
		private SolidBrush _freezeHighlightBrush;
		private SolidBrush _highlightBrush;
		private SolidBrush _secondaryHighlightBrush;

		private string _windowTitle = "Hex Editor";

		protected override string WindowTitle => _windowTitle;

		protected override string WindowTitleStatic => "Hex Editor";

		public HexEditor()
		{
			_hexFind = new HexFind(this);
			RecentTables = new RecentFiles(8);
			DataSize = 1;

			Font font = OSTailoredCode.IsUnixHost
				? new("Liberation Mono", 9)
				: new("Courier New", 8);

			// Measure the font. There seems to be some extra horizontal padding on the first
			// character so we'll see how much the width increases on the second character.
			var fontSize1 = TextRenderer.MeasureText("0", font);
			var fontSize2 = TextRenderer.MeasureText("00", font);
			_fontWidth = fontSize2.Width - fontSize1.Width;
			_fontHeight = fontSize1.Height;
			const int MAGIC_FIX_NUMBER_H = 4; // don't wanna know
			if (OSTailoredCode.IsUnixHost) _fontHeight -= MAGIC_FIX_NUMBER_H;

			InitializeComponent();
			Icon = ToolIcon;
			SaveMenuItem.Image = Resources.SaveAs;
			CopyMenuItem.Image = Resources.Duplicate;
			PasteMenuItem.Image = Resources.Paste;
			AddToRamWatchMenuItem.Image = Resources.Find;
			FreezeAddressMenuItem.Image = Resources.Freeze;
			UnfreezeAllMenuItem.Image = Resources.Unfreeze;
			PokeAddressMenuItem.Image = Resources.Poke;
			CopyContextItem.Image = Resources.Duplicate;
			PasteContextItem.Image = Resources.Paste;
			FreezeContextItem.Image = Resources.Freeze;
			AddToRamWatchContextItem.Image = Resources.Find;
			UnfreezeAllContextItem.Image = Resources.Unfreeze;
			PokeContextItem.Image = Resources.Poke;

			AddressesLabel.BackColor = Color.Transparent;
			SetHeader();
			Closing += (o, e) => CloseHexFind();

			Header.Font = font;
			AddressesLabel.Font = font;
			AddressLabel.Font = font;
		}

		private void HexEditor_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			DataSize = _domain.WordSize;
			SetDataSize(DataSize);

			if (!string.IsNullOrWhiteSpace(LastDomain)
				&& MemoryDomains.Any(m => m.Name == LastDomain))
			{
				SetMemoryDomain(LastDomain);
			}

			if (RecentTables.AutoLoad)
			{
				LoadFileFromRecent(RecentTables[0]);
			}

			GeneralUpdate();
		}

		protected override void UpdateAfter()
		{
			if (AddressesLabel.ZeroColor != Colors.Foreground00)
			{
				AddressesLabel.ZeroColor = Colors.Foreground00;
			}

			AddressesLabel.Text = GenerateMemoryViewString(true);
		}

		protected override void GeneralUpdate()
		{
			if (AddressesLabel.ZeroColor != Colors.Foreground00)
			{
				AddressesLabel.ZeroColor = Colors.Foreground00;
			}

			AddressesLabel.Text = GenerateMemoryViewString(true);
			AddressLabel.Text = GenerateAddressString();
		}

		public override void Restart()
		{
			_romDomain = null;
			if (Emulator.SystemId is not (VSystemID.Raw.Arcade or VSystemID.Raw.N3DS))
			{
				var rom = GetRomBytes();
				if (rom is not null)
				{
					_romDomain = new MemoryDomainByteArray(ROM_DOMAIN_NAME, MemoryDomain.Endian.Little, rom, writable: true, wordSize: 1);
				}
			}

			if (_domain.Name == ROM_DOMAIN_NAME && _romDomain is not null)
			{
				_domain = _romDomain;
			}
			else
			{ 
				_domain = MemoryDomains.Any(x => x.Name == _domain.Name)
					? MemoryDomains[_domain.Name]
					: MemoryDomains.MainMemory;
			}

			BigEndian = _domain.EndianType == MemoryDomain.Endian.Big;

			_maxRow = _domain.Size / 2;

			// Don't reset scroll bar if restarting the same rom
			if (_lastRom != MainForm.CurrentlyOpenRom)
			{
				_lastRom = MainForm.CurrentlyOpenRom;
				ResetScrollBar();
			}
			
			SetDataSize(DataSize);
			SetHeader();

			GeneralUpdate();
		}

		public void SetToAddresses(IEnumerable<long> addresses, MemoryDomain domain, WatchSize size)
		{
			DataSize = (int)size;
			SetDataSize(DataSize);
			var addrList = addresses.ToList();
			if (addrList.Count is not 0)
			{
				SetMemoryDomain(domain.Name);
				SetHighlighted(addrList[0]);
				_secondaryHighlightedAddresses.Clear();
				_secondaryHighlightedAddresses.AddRange(addrList.Where(addr => addr != addrList[0]));
				ClearNibbles();
				GeneralUpdate();
				MemoryViewerBox.Refresh();
			}
		}

		public byte[] ConvertTextToBytes(string str)
		{
			if (_textTable.Count is not 0)
			{
				var byteArr = new byte[str.Length];
				for (var i = 0; i < str.Length; i++)
				{
					var c = str[i];
					byteArr[i] = (byte) (_textTable.FirstOrNull(kvp => kvp.Value == c)?.Key ?? 0);
				}
				return byteArr;
			}

			return str.Select(Convert.ToByte).ToArray();
		}

		public void FindNext(string value, bool wrap)
		{
			long found = -1;

			var search = value.Replace(" ", "").ToUpperInvariant();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			long startByte;
			if (_highlightedAddress == null)
			{
				startByte = 0;
			}
			else if (_highlightedAddress >= (_domain.Size - 1 - numByte))
			{
				startByte = 0;
			}
			else
			{
				startByte = _highlightedAddress.Value + DataSize;
			}

			byte[] searchBytes = ConvertHexStringToByteArray(search);
			for (var i = startByte; i < (_domain.Size - numByte); i++)
			{
				bool differenceFound = false;
				for (var j = 0; j < numByte; j++)
				{
					if (_domain.PeekByte(i + j) != searchBytes[j])
					{
						differenceFound = true;
						break;
					}
				}

				if (!differenceFound)
				{
					found = i;
					break;
				}
			}

			if (found > -1)
			{
				HighlightSecondaries(search, found);
				GoToAddress(found);
				_findStr = search;
			}
			else if (!wrap)
			{
				FindPrev(value, true); // Search the opposite direction if not found
			}

			_hexFind.Close();
		}

		public void FindPrev(string value, bool wrap)
		{
			long found = -1;

			var search = value.Replace(" ", "").ToUpperInvariant();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var numByte = search.Length / 2;

			long startByte = _highlightedAddress - 1 ?? _domain.Size - DataSize - numByte;

			byte[] searchBytes = ConvertHexStringToByteArray(search);
			for (var i = startByte; i >= 0; i--)
			{
				bool differenceFound = false;
				for (var j = 0; j < numByte; j++)
				{
					if (_domain.PeekByte(i + j) != searchBytes[j]) {
						differenceFound = true;
						break;
					}
				}

				if (!differenceFound)
				{
					found = i;
					break;
				}
			}

			if (found > -1)
			{
				HighlightSecondaries(search, found);
				GoToAddress(found);
				_findStr = search;
			}
			else if (!wrap)
			{
				FindPrev(value, true); // Search the opposite direction if not found
			}

			_hexFind.Close();
		}

		private byte[] ConvertHexStringToByteArray(string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return Array.Empty<byte>();

			// TODO: Better method of handling this?
			if (str.Length % 2 == 1)
			{
				str += "0";
			}

			byte[] bytes = new byte[str.Length / 2];
			
			for (int i = 0; i < str.Length; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
			}
			
			return bytes;
		}

		private char Remap(byte val)
		{
			if (_textTable.Count != 0) return _textTable.TryGetValue(val, out var c) ? c : '?';

			if (val < ' ' || val >= 0x7F)
			{
				return '.';
			}

			return (char)val;
		}

		private bool CurrentRomIsArchive()
		{
			var path = MainForm.CurrentlyOpenRom;
			if (path == null)
			{
				return false;
			}

			using var file = new HawkFile(path);

			return file.Exists && file.IsArchive;
		}

		private byte[] GetRomBytes()
		{
			var path = MainForm.CurrentlyOpenRomArgs.OpenAdvanced.SimplePath;
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

			try
			{
				using var file = new HawkFile(path);
				if (file.Exists)
				{
					return file.ReadAllBytes();
				}
			}
			catch (Exception ex)
			{
				using var exceptionBox = new ExceptionBox(ex);
				this.ShowDialogWithTempMute(exceptionBox);
			}

			return null;
		}

		private static int GetNumDigits(long i)
		{
			if (i <= 0x10000)
			{
				return 4;
			}

			return i <= 0x1000000 ? 6 : 8;
		}

		private static bool IsHexKeyCode(char key)
		{
			if (key >= '0' && key <= '9') // 0-9
			{
				return true;
			}
			
			if (key >= 'a' && key <= 'f') // A-F
			{
				return true;
			}
			
			if (key >= 'A' && key <= 'F') // A-F
			{
				return true;
			}
			
			return false;
		}

		private void LoadConfigSettings()
		{
			HexMenuStrip.BackColor = Colors.MenuBar;
			MemoryViewerBox.BackColor = Colors.Background;
			MemoryViewerBox.ForeColor = Colors.Foreground;
			Header.BackColor = Colors.Background;
			Header.ForeColor = Colors.Foreground;

			_freezeBrush = new SolidBrush(Colors.Freeze);
			_freezeHighlightBrush = new SolidBrush(Colors.HighlightFreeze);
			_highlightBrush = new SolidBrush(Colors.Highlight);
			_secondaryHighlightBrush = new SolidBrush(Color.FromArgb(0x44, Colors.Highlight));
		}

		private void CloseHexFind()
		{
			if (_hexFind.IsHandleCreated || !_hexFind.IsDisposed)
			{
				_hexFind.Close();
			}
		}

		private string GenerateAddressString()
		{
			var addrStr = new StringBuilder();

			for (var i = 0; i < _rowsVisible; i++)
			{
				long row = i + HexScrollBar.Value;
				long addr = row << 4;
				if (addr >= _domain.Size)
				{
					break;
				}

				if (_numDigits == 4)
				{
					addrStr.Append("    "); // Hack to line things up better between 4 and 6
				}
				else if (_numDigits == 6)
				{
					addrStr.Append("  ");
				}

				addrStr.AppendLine($"{addr.ToHexString(_numDigits)} |");
			}

			return addrStr.ToString();
		}

		private string GenerateMemoryViewString(bool forWindow)
		{
			var rowStr = new StringBuilder();
			var hexValues = MakeValues(DataSize);
			var charValues = MakeValues(1);
			for (var i = 0; i < _rowsVisible; i++)
			{
				long row = i + HexScrollBar.Value;
				long addr = row << 4;
				if (addr >= _domain.Size)
				{
					break;
				}

				for (var j = 0; j < 16; j += DataSize)
				{
					if (addr + j + DataSize <= _domain.Size)
					{
						var addressVal = hexValues[addr + j];
						rowStr.AppendFormat(_digitFormatString, addressVal);
					}
					else
					{
						for (var t = 0; t < DataSize; t++)
						{
							rowStr.Append("  ");
						}

						rowStr.Append(' ');
					}
				}

				rowStr.Append("| ");
				for (var k = 0; k < 16; k++)
				{
					if (addr + k < _domain.Size)
					{
						
						byte b = (byte)charValues[addr + k];
						char c = Remap(b);
						rowStr.Append(c);

						// winforms will be using these as escape codes for hotkeys
						if (forWindow) if (c == '&') rowStr.Append('&');
					}
				}

				rowStr.AppendLine();
			}

			return rowStr.ToString();
		}

		private Dictionary<long, long> MakeValues(int dataSize)
		{
			long start = (long)HexScrollBar.Value << 4;
			long end = (long)(HexScrollBar.Value + _rowsVisible) << 4;

			end = Math.Min(end, _domain.Size);
			end &= -(long)dataSize;

			var dict = new Dictionary<long, long>();

			if (end <= start)
				return dict;

			var range = start.RangeToExclusive(end);

			switch (dataSize)
			{
				default:
				case 1:
				{
					var vals = new byte[end - start];
					_domain.BulkPeekByte(range, vals);
					int i = 0;
					for (var addr = start; addr < end; addr += dataSize)
						dict.Add(addr, vals[i++]);
					break;
				}
				case 2:
				{
					var vals = new ushort[(end - start) >> 1];
					_domain.BulkPeekUshort(range, BigEndian, vals);
					int i = 0;
					for (var addr = start; addr < end; addr += dataSize)
						dict.Add(addr, vals[i++]);
					break;
				}
				case 4:
				{
					var vals = new uint[(end - start) >> 2];
					_domain.BulkPeekUint(range, BigEndian, vals);
					int i = 0;
					for (var addr = start; addr < end; addr += dataSize)
						dict.Add(addr, vals[i++]);
					break;
				}
			}

			return dict;
		}

		private int MakeValue(int dataSize, long address)
		{
			switch (dataSize)
			{
				default:
				case 1:
					return _domain.PeekByte(address);
				case 2:
					return _domain.PeekUshort(address, BigEndian);
				case 4:
					return (int)_domain.PeekUint(address, BigEndian);
			}
		}

		private void SetMemoryDomain(string name)
		{
			if (_romDomain is not null && name == _romDomain.Name)
			{
				_domain = _romDomain;
			}
			else
			{
				_domain = MemoryDomains[name];
			}

			BigEndian = _domain.EndianType == MemoryDomain.Endian.Big;
			_maxRow = _domain.Size / 2;
			SetUpScrollBar();
			if (HexScrollBar.Minimum.RangeTo(HexScrollBar.Maximum).Contains(0))
			{
				HexScrollBar.Value = 0;
			}

			AddressesLabel.ForeColor = _domain.Writable
				? SystemColors.ControlText
				: SystemColors.ControlDarkDark;

			if (AllHighlightedAddresses.DefaultIfEmpty().Max() >= _domain.Size)
			{
				_highlightedAddress = null;
				_secondaryHighlightedAddresses.Clear();
			}

			UpdateGroupBoxTitle();
			SetHeader();
			GeneralUpdate();
			LastDomain = _domain.Name;
		}

		private void UpdateGroupBoxTitle()
		{
			var addressesString = "0x" + $"{_domain.Size / DataSize:X8}".TrimStart('0');
			var viewerText = $"{Emulator.SystemId} {_domain}{(_domain.Writable ? string.Empty : " (READ-ONLY)")}  -  {addressesString} addresses";
			if (_nibbles.Count is not 0)
			{
				viewerText += $"  Typing: ({MakeNibbles()})";
			}

			MemoryViewerBox.Text = viewerText;
		}

		private void ClearNibbles()
		{
			_nibbles.Clear();
			UpdateGroupBoxTitle();
		}

		private void GoToAddress(long address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= _domain.Size)
			{
				address = _domain.Size - DataSize;
			}

			SetHighlighted(address);
			ClearNibbles();
			GeneralUpdate();
			MemoryViewerBox.Refresh();
		}

		private void SetHighlighted(long address)
		{
			if (address < 0)
			{
				address = 0;
			}

			if (address >= _domain.Size)
			{
				address = _domain.Size - DataSize;
			}

			if (!IsVisible(address))
			{
				var value = (address / 16) - _rowsVisible + 1;
				if (value < 0)
				{
					value = 0;
				}

				HexScrollBar.Value = (int)value; // This will fail on a sufficiently large domain
			}

			_highlightedAddress = address;
			_addressOver = address;
			ClearNibbles();
			UpdateFormText();
		}

		private void UpdateFormText()
		{
			if (!_highlightedAddress.HasValue)
			{
				_windowTitle = WindowTitleStatic;
			}
			else
			{
				var newTitle = "Hex Editor";
				newTitle += " - Editing Address 0x" + string.Format(_numDigitsStr, _highlightedAddress);
				if (_secondaryHighlightedAddresses.Count is not 0)
				{
					newTitle += $" (Selected 0x{_secondaryHighlightedAddresses.Count + (_secondaryHighlightedAddresses.Contains(_highlightedAddress.Value) ? 0 : 1):X})";
				}
				_windowTitle = newTitle;
			}
			UpdateWindowTitle();
		}

		private bool IsVisible(long address) => ((long) HexScrollBar.Value).RangeToExclusive(HexScrollBar.Value + _rowsVisible).Contains(address >> 4);

		private void SetHeader()
		{
			switch (DataSize)
			{
				case 1:
					Header.Text = "         0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
					break;
				case 2:
					Header.Text = "         0    2    4    6    8    A    C    E";
					break;
				case 4:
					Header.Text = "         0        4        8        C";
					break;
			}

			_numDigits = GetNumDigits(_domain.Size);
			_numDigitsStr = $"{{0:X{_numDigits}}}  ";
		}

		private void SetDataSize(int size)
		{
			if (size == 1 || size == 2 || size == 4)
			{
				DataSize = size;
				_digitFormatString = $"{{0:X{DataSize * 2}}} ";
				SetHeader();
				UpdateGroupBoxTitle();
				GeneralUpdate();
				_secondaryHighlightedAddresses.Clear();
			}
		}

		private Watch MakeWatch(long address)
		{
			switch (DataSize)
			{
				default:
				case 1:
					return Watch.GenerateWatch(_domain, address, WatchSize.Byte, Common.WatchDisplayType.Hex, BigEndian);
				case 2:
					return Watch.GenerateWatch(_domain, address, WatchSize.Word, Common.WatchDisplayType.Hex, BigEndian);
				case 4:
					return Watch.GenerateWatch(_domain, address, WatchSize.DWord, Common.WatchDisplayType.Hex, BigEndian);
			}
		}

		private bool IsFrozen(long address)
		{
			return MainForm.CheatList.IsActive(_domain, address);
		}

		private void FreezeHighlighted()
		{
			if (!AreAnyHighlighted) return;
			MainForm.CheatList.AddRange(AllHighlightedAddresses.Select(address =>
			{
				var watch = Watch.GenerateWatch(
					_domain,
					address,
					WatchSize,
					Common.WatchDisplayType.Hex,
					BigEndian);
				return new Cheat(watch, watch.Value);
			}));
			MemoryViewerBox.Refresh();
		}

		private void UnfreezeHighlighted()
		{
			if (!AreAnyHighlighted) return;
			//TODO reconcile the logic between these two
			if (_highlightedAddress >= 0)
			{
				MainForm.CheatList.RemoveRange(MainForm.CheatList.Where(x => x.Contains(_highlightedAddress.Value)));
			}

			if (_secondaryHighlightedAddresses.Count is not 0)
			{
				MainForm.CheatList.RemoveRange(
					MainForm.CheatList.Where(cheat => !cheat.IsSeparator && cheat.Domain == _domain
						&& _secondaryHighlightedAddresses.Contains(cheat.Address ?? 0)));
			}

			MemoryViewerBox.Refresh();
		}

		private void SaveFileBinary(string path)
		{
			var file = new FileInfo(path);
			using var binWriter = new BinaryWriter(File.Open(file.FullName, FileMode.Create));
			for (var i = 0; i < _domain.Size; i++)
			{
				binWriter.Write(_domain.PeekByte(i));
			}
		}

		private string RomDirectory
		{
			get
			{
				string path = Config.RecentRoms.MostRecent;

				if (string.IsNullOrWhiteSpace(path))
				{
					return "";
				}
				return Path.GetDirectoryName(path.SubstringBefore('|'));
			}
		}

		private string RomName
		{
			get
			{
				string path = Config.RecentRoms.MostRecent;

				if (string.IsNullOrWhiteSpace(path))
				{
					return "";
				}
				return Path.GetFileName(path.SubstringAfterLast('|'));
			}
		}

		private string GetBinarySaveFileFromUser()
			=> this.ShowFileSaveDialog(
				discardCWDChange: true,
				filter: _domain.Name is ROM_DOMAIN_NAME
					? CreateBinaryDumpFSFilterSet(Path.GetExtension(RomName).RemovePrefix('.'))
					: BinFilesFSFilterSet,
				initDir: RomDirectory,
				initFileName: _domain.Name is ROM_DOMAIN_NAME
					? RomName
					: Game.FilesystemSafeName()) ?? string.Empty;

		private string GetSaveFileFromUser()
			=> this.ShowFileSaveDialog(
				discardCWDChange: true,
				filter: HexDumpsFSFilterSet,
				initDir: RomDirectory,
				initFileName: _domain.Name is ROM_DOMAIN_NAME
					? $"{Path.GetFileNameWithoutExtension(RomName)}.txt"
					: Game.FilesystemSafeName()) ?? string.Empty;

		private void ResetScrollBar()
		{
			HexScrollBar.Value = 0;
			SetUpScrollBar();
			Refresh();
		}

		private void SetUpScrollBar()
		{
			_rowsVisible = (MemoryViewerBox.Height - (_fontHeight * 2) - (_fontHeight / 2)) / _fontHeight;
			var totalRows = (int)((_domain.Size + 15) / 16);

			if (totalRows < _rowsVisible)
			{
				_rowsVisible = totalRows;
			}

			HexScrollBar.Maximum = totalRows - 1;
			HexScrollBar.LargeChange = _rowsVisible;
			HexScrollBar.Visible = totalRows > _rowsVisible;

			AddressLabel.Text = GenerateAddressString();
		}

		private long GetPointedAddress(int x, int y)
		{
			// Scroll value determines the first row
			long i = HexScrollBar.Value;
			var rowOffset = y / _fontHeight;
			i += rowOffset;
			int colWidth = DataSize * 2 + 1;

			var column = x / (_fontWidth * colWidth);

			var innerOffset = AddressesLabel.Location.X - AddressLabel.Location.X + AddressesLabel.Margin.Left;
			var start = GetTextOffset() - innerOffset;
			if (x > start)
			{
				column = (x - start) / (_fontWidth * DataSize);
			}

			return 0L.RangeTo(_maxRow).Contains(i) && 0.RangeTo(16 / DataSize).Contains(column)
				? i * 16 + column * DataSize
				: -1;
		}

		private void DoShiftClick()
		{
			if (!0L.RangeToExclusive(_domain.Size).Contains(_addressOver)) return;

			_secondaryHighlightedAddresses.Clear();
			if (_addressOver < _highlightedAddress)
			{
				for (var x = _addressOver; x < _highlightedAddress; x += DataSize)
				{
					_secondaryHighlightedAddresses.Add(x);
				}
			}
			else if (_addressOver > _highlightedAddress)
			{
				for (var x = _highlightedAddress.Value + DataSize; x <= _addressOver; x += DataSize)
				{
					_secondaryHighlightedAddresses.Add(x);
				}
			}

			if (!IsVisible(_addressOver))
			{
				var value = (_addressOver / 16) + 1 - ((_addressOver / 16) < HexScrollBar.Value ? 1 : _rowsVisible);
				if (value < 0)
				{
					value = 0;
				}

				HexScrollBar.Value = (int)value; // This will fail on a sufficiently large domain
			}
		}

		private void ClearHighlighted()
		{
			_highlightedAddress = null;
			UpdateFormText();
			MemoryViewerBox.Refresh();
		}

		private Point GetAddressCoordinates(long address)
		{
			var extra = (address % DataSize) * _fontWidth * 2;
			var xOffset = AddressesLabel.Location.X + _fontWidth / 2 - 2;
			var yOffset = AddressesLabel.Location.Y;
			if (OSTailoredCode.IsUnixHost)
			{
				// don't wanna know
				const int MAGIC_FIX_NUMBER_X = -2;
				const int MAGIC_FIX_NUMBER_Y = 2;
				xOffset += MAGIC_FIX_NUMBER_X;
				yOffset += MAGIC_FIX_NUMBER_Y;
			}

			return new Point(
				(int)((((address % 16) / DataSize) * (_fontWidth * (DataSize * 2 + 1))) + xOffset + extra),
				(int)((((address / 16) - HexScrollBar.Value) * _fontHeight) + yOffset));
		}

		// TODO: rename this, but it is a hack work around for highlighting misaligned addresses that result from highlighting on in a smaller data size and switching size
		private bool NeedsExtra(long val)
		{
			return val % DataSize > 0;
		}

		private int GetTextOffset()
		{
			int start = (16 / DataSize) * _fontWidth * (DataSize * 2 + 1);
			start += AddressesLabel.Location.X + _fontWidth / 2;
			start += _fontWidth * 2;
			const int MAGIC_FIX_NUMBER_X_ASCII = -3; // don't wanna know
			if (OSTailoredCode.IsUnixHost) start += MAGIC_FIX_NUMBER_X_ASCII;
			return start;
		}

		private long GetTextX(long address)
		{
			return GetTextOffset() + ((address % 16) * _fontWidth);
		}

		private string MakeNibbles()
		{
			var str = "";
			foreach (var c in _nibbles)
			{
				str += c;
			}

			return str;
		}

		private void AddToSecondaryHighlights(long address)
		{
			if (0L.RangeToExclusive(_domain.Size).Contains(address) && !_secondaryHighlightedAddresses.Contains(address))
			{
				_secondaryHighlightedAddresses.Add(address);
			}
		}

		private void IncrementAddress(long address)
		{
			if (MainForm.CheatList.IsActive(_domain, address))
			{
				// TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
				MainForm.CheatList.First(x => x.Domain == _domain && x.Address == address).Increment();
			}
			else
			{
				switch (DataSize)
				{
					default:
					case 1:
						_domain.PokeByte(
							address,
							(byte)(_domain.PeekByte(address) + 1));
						break;
					case 2:
						_domain.PokeUshort(
							address,
							(ushort)(_domain.PeekUshort(address, BigEndian) + 1),
							BigEndian);
						break;
					case 4:
						_domain.PokeUint(
							address,
							_domain.PeekUint(address, BigEndian) + 1,
							BigEndian);
						break;
				}
			}
		}

		private void DecrementAddress(long address)
		{
			if (MainForm.CheatList.IsActive(_domain, address))
			{
				// TODO: Increment should be intelligent since IsActive is.  If this address is part of a multi-byte cheat it should intelligently increment just that byte
				MainForm.CheatList.First(x => x.Domain == _domain && x.Address == address).Decrement();
			}
			else
			{
				switch (DataSize)
				{
					default:
					case 1:
						_domain.PokeByte(
							address,
							(byte)(_domain.PeekByte(address) - 1));
						break;
					case 2:
						_domain.PokeUshort(
							address,
							(ushort)(_domain.PeekUshort(address, BigEndian) - 1),
							BigEndian);
						break;
					case 4:
						_domain.PokeUint(
							address,
							_domain.PeekUint(address, BigEndian) - 1,
							BigEndian);
						break;
				}
			}
		}

		private string ValueString(long address)
		{
			if (address != -1)
			{
				return string.Format(_digitFormatString, MakeValue(DataSize, address)).Trim();
			}
			
			return "";
		}

		private string GetFindValues()
		{
			if (_highlightedAddress.HasValue)
			{
				var values = ValueString(_highlightedAddress.Value);
				return _secondaryHighlightedAddresses.Aggregate(values, (current, x) => current + ValueString(x));
			}
			
			return "";
		}

		private void HighlightSecondaries(string value, long found)
		{
			// This function assumes that the primary highlighted value has been set and sets the remaining characters in this string
			_secondaryHighlightedAddresses.Clear();

			var addrLength = DataSize * 2;
			if (value.Length <= addrLength)
			{
				return;
			}

			var numToHighlight = (value.Length / addrLength) - 1;

			for (var i = 0; i < numToHighlight; i += DataSize)
			{
				_secondaryHighlightedAddresses.Add(found + DataSize + i);
			}
		}

		private bool LoadTable(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}

			_textTable.Clear();
			var file = new FileInfo(path);
			if (!file.Exists)
			{
				return false;
			}

			using var sr = file.OpenText();
			string line;
			while ((line = sr.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				var parts = line.Split('=');
				_textTable.Add(int.Parse(parts[0], NumberStyles.HexNumber), parts[1][0]);
			}

			return true;
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (_domain.Name is ROM_DOMAIN_NAME)
			{
				SaveMenuItem.Visible = !CurrentRomIsArchive();
				SaveAsBinaryMenuItem.Text = "Save as ROM...";
			}
			else
			{
				SaveMenuItem.Visible = false;
				SaveAsBinaryMenuItem.Text = "Save as binary...";
			}

			CloseTableFileMenuItem.Enabled = _textTable.Count is not 0;
		}
		
		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!CurrentRomIsArchive())
			{
				SaveFileBinary(MainForm.CurrentlyOpenRom);
			}
		}

		private void SaveAsBinaryMenuItem_Click(object sender, EventArgs e)
		{
			var path = GetBinarySaveFileFromUser();
			if (!string.IsNullOrEmpty(path))
			{
				SaveFileBinary(path);
			}
		}

		private void importAsBinaryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(!_domain.Writable)
			{
				DialogController.ShowMessageBox("This Memory Domain can't be Poked; so importing can't work");
				return;
			}

			var path = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: ImportableFSFilterSet,
				initDir: Config!.PathEntries.ToolsAbsolutePath());
			if (path is null) return;

			using var inf = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			long todo = Math.Min(inf.Length, _domain.Size);
			for (long i = 0; i < todo; i++)
			{
				_domain.PokeByte(i, (byte)inf.ReadByte());
			}
		}

		private void SaveAsTextMenuItem_Click(object sender, EventArgs e)
		{
			var path = GetSaveFileFromUser();
			if (!string.IsNullOrWhiteSpace(path))
			{
				var file = new FileInfo(path);
				using var sw = new StreamWriter(file.FullName);
				var sb = new StringBuilder();

				for (var i = 0; i < _domain.Size / 16; i++)
				{
					for (var j = 0; j < 16; j++)
					{
						sb.Append($"{_domain.PeekByte((i * 16) + j):X2} ");
					}

					sb.AppendLine();
				}

				sw.WriteLine(sb);
			}
		}

		private void LoadTableFileMenuItem_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileOpenDialog(
				discardCWDChange: false,
				filter: TextTablesFSFilterSet,
				initDir: Config!.PathEntries.ToolsAbsolutePath(),
				initFileName: $"{Path.GetFileNameWithoutExtension(Config.RecentRoms.MostRecent.SubstringAfterLast('|'))}.tbl");
			if (result is null) return;
			LoadTable(result);
			RecentTables.Add(result);
			GeneralUpdate();
		}

		private void CloseTableFileMenuItem_Click(object sender, EventArgs e)
		{
			_textTable.Clear();
		}

		public void LoadFileFromRecent(string path)
		{

			var result = LoadTable(path);
			if (!result)
			{
				RecentTables.HandleLoadError(MainForm, path);
			}
			else
			{
				RecentTables.Add(path);
				GeneralUpdate();
			}
		}

		private void RecentTablesSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentTablesSubMenu.ReplaceDropDownItems(RecentTables.RecentMenu(this, LoadFileFromRecent, "Session"));

		private void EditMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			var data = Clipboard.GetDataObject();
			PasteMenuItem.Enabled = _domain.Writable && AreAnyHighlighted
				&& data?.GetDataPresent(DataFormats.Text) is true;
			FindNextMenuItem.Enabled = !string.IsNullOrWhiteSpace(_findStr);
		}

		private string MakeCopyExportString(bool export)
		{
			var addresses = AllHighlightedAddresses.Order().ToArray();
			if (addresses.Length is 0) return null;

			// find the maximum length of the exported string
			int maximumLength = addresses.Length * (export ? 3 : 2) + 8;
			var sb = new StringBuilder(maximumLength);

			// generate it differently for export (as you see it) or copy (raw bytes)
			if (export)
				for (int i = 0; i < addresses.Length; i++)
				{
					sb.Append(ValueString(addresses[i]));
					if (i != addresses.Length - 1)
					{
						sb.Append(' ');
					}
				}
			else
			{
				foreach (var addr in addresses)
				{
					long start = addr;
					long end = addr + DataSize -1 ;

					for (long a = start; a <= end; a++)
					{
						sb.AppendFormat("{0:X2}", MakeValue(1, a));
					}
				}
			}

			return sb.ToString();
		}

		private void ExportMenuItem_Click(object sender, EventArgs e)
		{
			var value = MakeCopyExportString(true);
			if (!string.IsNullOrEmpty(value))
			{
				Clipboard.SetDataObject(value);
			}
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			var value = MakeCopyExportString(false);
			if (!string.IsNullOrEmpty(value))
			{
				Clipboard.SetDataObject(value);
			}
		}

		private void PasteMenuItem_Click(object sender, EventArgs e)
		{
			var data = Clipboard.GetDataObject();

			if (data == null || !data.GetDataPresent(DataFormats.Text))
			{
				return;
			}
			
			var clipboardRaw = (string)data.GetData(DataFormats.Text);
			var hex = clipboardRaw.OnlyHex();

			var numBytes = hex.Length / 2;
			for (var i = 0; i < numBytes; i++)
			{
				var value = int.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
				var address = (_highlightedAddress ?? 0) + i;

				if (address < _domain.Size)
				{
					_domain.PokeByte(address, (byte)value);
				}
			}

			GeneralUpdate();
		}

		private bool _lastSearchWasText;
		private void SearchTypeChanged(bool isText)
		{
			_lastSearchWasText = isText;
		}

		private void FindMenuItem_Click(object sender, EventArgs e)
		{
			_findStr = GetFindValues();
			if (!_hexFind.IsHandleCreated || _hexFind.IsDisposed)
			{
				_hexFind = new HexFind(this)
				{
					InitialLocation = PointToScreen(AddressesLabel.Location),
					InitialValue = _findStr,
					SearchTypeChangedCallback = SearchTypeChanged,
					InitialText = _lastSearchWasText
				};

				_hexFind.Show();
			}
			else
			{
				_hexFind.InitialValue = _findStr;
				_hexFind.Activate();
			}
		}

		private void FindNextMenuItem_Click(object sender, EventArgs e)
		{
			FindNext(_findStr, false);
		}

		private void FindPrevMenuItem_Click(object sender, EventArgs e)
		{
			FindPrev(_findStr, false);
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			BigEndianMenuItem.Checked = BigEndian;
			DataSizeByteMenuItem.Checked = DataSize == 1;
			DataSizeWordMenuItem.Checked = DataSize == 2;
			DataSizeDWordMenuItem.Checked = DataSize == 4;

			if (_highlightedAddress.HasValue && IsFrozen(_highlightedAddress.Value))
			{
				FreezeAddressMenuItem.Image = Resources.Unfreeze;
				FreezeAddressMenuItem.Text = "Un&freeze Address";
			}
			else
			{
				FreezeAddressMenuItem.Image = Resources.Freeze;
				FreezeAddressMenuItem.Text = "&Freeze Address";
			}

			AddToRamWatchMenuItem.Enabled =
				_highlightedAddress.HasValue;

			PokeAddressMenuItem.Enabled = FreezeAddressMenuItem.Enabled
				= _highlightedAddress is not null && _domain.Writable;
		}

		private void MemoryDomainsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsMenuItem.ReplaceDropDownItems(MemoryDomains.MenuItems(SetMemoryDomain, _domain.Name).ToArray());

			if (_romDomain != null)
			{
				var romMenuItem = new ToolStripMenuItem
				{
					Text = _romDomain.Name,
					Checked = _domain.Name == _romDomain.Name
				};

				MemoryDomainsMenuItem.DropDownItems.Add(new ToolStripSeparator());
				MemoryDomainsMenuItem.DropDownItems.Add(romMenuItem);

				romMenuItem.Click += (o, ev) => SetMemoryDomain(_romDomain.Name);
			}
		}

		private void DataSizeByteMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(1);
		}

		private void DataSizeWordMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(2);
		}

		private void DataSizeDWordMenuItem_Click(object sender, EventArgs e)
		{
			SetDataSize(4);
		}

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
		{
			BigEndian = !BigEndian;
			GeneralUpdate();
		}

		private void GoToAddressMenuItem_Click(object sender, EventArgs e)
		{
			using var inputPrompt = new InputPrompt
			{
				Text = "Go to Address",
				StartLocation = this.ChildPointToScreen(MemoryViewerBox),
				Message = "Enter a hexadecimal value"
			};

			if (this.ShowDialogWithTempMute(inputPrompt).IsOk() && inputPrompt.PromptText.IsHex())
			{
				GoToAddress(long.Parse(inputPrompt.PromptText, NumberStyles.HexNumber));
			}

			AddressLabel.Text = GenerateAddressString();
		}

		private void AddToRamWatchMenuItem_Click(object sender, EventArgs e)
		{
			if (!AreAnyHighlighted) return;
			Tools.LoadRamWatch(true);
			foreach (var addr in AllHighlightedAddresses) Tools.RamWatch.AddWatch(MakeWatch(addr));
		}

		private void FreezeAddressMenuItem_Click(object sender, EventArgs e)
		{
			if (!_domain.Writable)
			{
				return;
			}

			if (_highlightedAddress.HasValue)
			{
				var highlighted = _highlightedAddress.Value;
				if (IsFrozen(highlighted))
				{
					UnfreezeHighlighted();
				}
				else
				{
					FreezeHighlighted();
				}
			}

			Tools.UpdateCheatRelatedTools(null, null);
			MemoryViewerBox.Refresh();
		}

		private void UnfreezeAllMenuItem_Click(object sender, EventArgs e)
		{
			MainForm.CheatList.Clear();
		}

		private void PokeAddressMenuItem_Click(object sender, EventArgs e)
		{
			if (!_domain.Writable || !AreAnyHighlighted) return;
			var watches = AllHighlightedAddresses.Select(address => Watch.GenerateWatch(
				_domain,
				address,
				WatchSize,
				Common.WatchDisplayType.Hex,
				BigEndian));
			using var poke = new RamPoke(DialogController, watches, MainForm.CheatList)
			{
				InitialLocation = this.ChildPointToScreen(AddressLabel),
				ParentTool = this
			};
			this.ShowDialogWithTempMute(poke);
			GeneralUpdate();
		}

		private void SetColorsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new HexColorsForm(this);
			this.ShowDialogWithTempMute(form);
		}

		private void ResetColorsToDefaultMenuItem_Click(object sender, EventArgs e)
		{
			MemoryViewerBox.BackColor = Color.FromName("Control");
			MemoryViewerBox.ForeColor = Color.FromName("ControlText");
			HexMenuStrip.BackColor = Color.FromName("Control");
			Header.BackColor = Color.FromName("Control");
			Header.ForeColor = Color.FromName("ControlText");
			Colors = new ColorConfig();
		}

		private void HexEditor_Resize(object sender, EventArgs e)
		{
			SetUpScrollBar();
			GeneralUpdate();
		}

		private void HexEditor_ResizeEnd(object sender, EventArgs e)
		{
			SetUpScrollBar();
		}

		private void HexEditor_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.G)
			{
				GoToAddressMenuItem_Click(sender, e);
				return;
			}

			if (e.Control && e.KeyCode == Keys.P)
			{
				PokeAddressMenuItem_Click(sender, e);
				return;
			}

			long currentAddress = _highlightedAddress ?? -1;
			long newHighlighted;
			switch (e.KeyCode)
			{
				case Keys.Up:
					newHighlighted = currentAddress - 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = newHighlighted + DataSize; i <= currentAddress; i += DataSize)
						{
							AddToSecondaryHighlights(i);
						}

						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.Down:
					newHighlighted = currentAddress + 16;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = currentAddress; i < newHighlighted; i += DataSize)
						{
							AddToSecondaryHighlights(i);
						}

						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.Left:
					newHighlighted = currentAddress - (1 * DataSize);
					if (e.Modifiers == Keys.Shift)
					{
						AddToSecondaryHighlights(currentAddress);
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.Right:
					newHighlighted = currentAddress + (1 * DataSize);
					if (e.Modifiers == Keys.Shift)
					{
						AddToSecondaryHighlights(currentAddress);
						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.PageUp:
					newHighlighted = currentAddress - (_rowsVisible * 16);
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = newHighlighted + 1; i <= currentAddress; i += DataSize)
						{
							AddToSecondaryHighlights(i);
						}

						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.PageDown:
					newHighlighted = currentAddress + (_rowsVisible * 16);
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = currentAddress + 1; i < newHighlighted; i += DataSize)
						{
							AddToSecondaryHighlights(i);
						}

						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.Tab:
					_secondaryHighlightedAddresses.Clear();
					if (e.Modifiers == Keys.Shift)
					{
						GoToAddress(currentAddress - 8);
					}
					else
					{
						GoToAddress(currentAddress + 8);
					}

					break;
				case Keys.Home:
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = 1; i <= currentAddress; i += DataSize)
						{
							AddToSecondaryHighlights(i);
						}

						GoToAddress(0);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(0);
					}

					break;
				case Keys.End:
					newHighlighted = _domain.Size - DataSize;
					if (e.Modifiers == Keys.Shift)
					{
						for (var i = currentAddress; i < newHighlighted; i += DataSize)
						{
							AddToSecondaryHighlights(i);
						}

						GoToAddress(newHighlighted);
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						GoToAddress(newHighlighted);
					}

					break;
				case Keys.Add:
					IncrementContextItem_Click(sender, e);
					break;
				case Keys.Subtract:
					DecrementContextItem_Click(sender, e);
					break;
				case Keys.Space:
					FreezeAddressMenuItem_Click(sender, e);
					break;
				case Keys.Delete:
					if (e.Modifiers == Keys.Shift)
					{
						MainForm.CheatList.Clear();
					}
					else
					{
						UnfreezeHighlighted();
					}

					break;
				case Keys.W:
					if (e.Modifiers == Keys.Control)
					{
						AddToRamWatchMenuItem_Click(sender, e);
					}

					break;
				case Keys.Escape:
					_secondaryHighlightedAddresses.Clear();
					ClearHighlighted();
					break;
			}
		}

		private void HexEditor_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!IsHexKeyCode(e.KeyChar))
			{
				e.Handled = true;
				return;
			}

			if ((ModifierKeys & (Keys.Control | Keys.Shift | Keys.Alt)) != 0)
			{
				return;
			}

			if (!_domain.Writable || !_highlightedAddress.HasValue)
			{
				return;
			}

			var currentAddress = _highlightedAddress ?? 0;
			_nibbles.Add(e.KeyChar);
			if (_nibbles.Count == DataSize * 2)
			{
				var nibbleStr = MakeNibbles();
				switch (DataSize)
				{
					default:
					case 1:
						var byteVal = byte.Parse(nibbleStr, NumberStyles.HexNumber);
						_domain.PokeByte(currentAddress, byteVal);
						break;
					case 2:
						var ushortVal = ushort.Parse(nibbleStr, NumberStyles.HexNumber);
						_domain.PokeUshort(currentAddress, ushortVal, BigEndian);
						break;
					case 4:
						var uintVal = uint.Parse(nibbleStr, NumberStyles.HexNumber);
						_domain.PokeUint(currentAddress, uintVal, BigEndian);
						break;
				}

				ClearNibbles();
				SetHighlighted(currentAddress + DataSize);
				GeneralUpdate();
				Refresh();
			}

			UpdateGroupBoxTitle();
			GeneralUpdate();
		}

		private void ViewerContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var data = Clipboard.GetDataObject();

			CopyContextItem.Visible = AddToRamWatchContextItem.Visible = AreAnyHighlighted;

			FreezeContextItem.Visible = PokeContextItem.Visible
				= IncrementContextItem.Visible
				= DecrementContextItem.Visible
				= ContextSeparator2.Visible
					= AreAnyHighlighted && _domain.Writable;

			UnfreezeAllContextItem.Visible = MainForm.CheatList.AnyActive;
			PasteContextItem.Visible = _domain.Writable && data != null && data.GetDataPresent(DataFormats.Text);

			ContextSeparator1.Visible = AreAnyHighlighted || data?.GetDataPresent(DataFormats.Text) is true;

			if (_highlightedAddress.HasValue && IsFrozen(_highlightedAddress.Value))
			{
				FreezeContextItem.Text = "Un&freeze";
				FreezeContextItem.Image = Resources.Unfreeze;
			}
			else
			{
				FreezeContextItem.Text = "&Freeze";
				FreezeContextItem.Image = Resources.Freeze;
			}

			var shouldShowN64Matrix = _highlightedAddress is not null && Emulator.SystemId is VSystemID.Raw.N64;
			toolStripMenuItem1.Visible = viewN64MatrixToolStripMenuItem.Visible = shouldShowN64Matrix;
			viewN64MatrixToolStripMenuItem.Enabled = shouldShowN64Matrix && (_highlightedAddress.Value & 0b11) is 0;
		}

		private void IncrementContextItem_Click(object sender, EventArgs e)
		{
			if (!_domain.Writable)
			{
				return;
			}
			foreach (var addr in AllHighlightedAddresses) IncrementAddress(addr);
			GeneralUpdate();
		}

		private void DecrementContextItem_Click(object sender, EventArgs e)
		{
			if (!_domain.Writable)
			{
				return;
			}
			foreach (var addr in AllHighlightedAddresses) DecrementAddress(addr);
			GeneralUpdate();
		}

		private void HexEditor_MouseWheel(object sender, MouseEventArgs e)
		{
			var delta = 0;
			if (e.Delta > 0)
			{
				delta = -1;
			}
			else if (e.Delta < 0)
			{
				delta = 1;
			}

			var newValue = HexScrollBar.Value + delta;
			if (newValue < HexScrollBar.Minimum)
			{
				newValue = HexScrollBar.Minimum;
			}

			if (newValue > HexScrollBar.Maximum - HexScrollBar.LargeChange + 1)
			{
				newValue = HexScrollBar.Maximum - HexScrollBar.LargeChange + 1;
			}
			
			if (newValue != HexScrollBar.Value)
			{
				HexScrollBar.Value = newValue;
				MemoryViewerBox.Refresh();
			}
		}

		private void MemoryViewerBox_Paint(object sender, PaintEventArgs e)
		{
			var activeCheats = MainForm.CheatList.Where(x => x.Enabled);
			foreach (var cheat in activeCheats)
			{
				if (IsVisible(cheat.Address ?? 0))
				{
					if (_domain.ToString() == cheat.Domain.Name)
					{
						var gaps = (int)cheat.Size - DataSize;

						if (cheat.Size == WatchSize.DWord && DataSize == 2)
						{
							gaps -= 1;
						}

						if (gaps < 0) { gaps = 0; }
						
						var width = (_fontWidth * 2 * (int)cheat.Size) + (gaps * _fontWidth);

						var rect = new Rectangle(GetAddressCoordinates(cheat.Address ?? 0), new Size(width, _fontHeight));
						e.Graphics.DrawRectangle(_blackPen, rect);
						_freezeBrush.Color = Colors.Freeze;
						e.Graphics.FillRectangle(_freezeBrush, rect);
					}
				}
			}

			if (_highlightedAddress.HasValue && IsVisible(_highlightedAddress.Value))
			{
				long addressHighlighted = _highlightedAddress ?? 0;

				// Create a slight offset to increase rectangle sizes
				var point = GetAddressCoordinates(addressHighlighted);
				var textX = (int)GetTextX(addressHighlighted);
				var textPoint = new Point(textX, point.Y);

				var rect = new Rectangle(point, new Size(_fontWidth * 2 * DataSize + (NeedsExtra(addressHighlighted) ? _fontWidth : 0) + 2, _fontHeight));
				e.Graphics.DrawRectangle(_blackPen, rect);

				var textRect = new Rectangle(textPoint, new Size(_fontWidth * DataSize, _fontHeight));

				if (MainForm.CheatList.IsActive(_domain, addressHighlighted))
				{
					_freezeHighlightBrush.Color = Colors.HighlightFreeze;
					e.Graphics.FillRectangle(_freezeHighlightBrush, rect);
					e.Graphics.FillRectangle(_freezeHighlightBrush, textRect);
				}
				else
				{
					_highlightBrush.Color = Colors.Highlight;
					e.Graphics.FillRectangle(_highlightBrush, rect);
					e.Graphics.FillRectangle(_highlightBrush, textRect);
				}
			}

			foreach (var address in _secondaryHighlightedAddresses)
			{
				if (IsVisible(address))
				{
					var point = GetAddressCoordinates(address);
					var textX = (int)GetTextX(address);
					var textPoint = new Point(textX, point.Y);

					var rect = new Rectangle(point, new Size(_fontWidth * 2 * DataSize + 2, _fontHeight));
					e.Graphics.DrawRectangle(_blackPen, rect);

					var textRect = new Rectangle(textPoint, new Size(_fontWidth * DataSize, _fontHeight));

					if (MainForm.CheatList.IsActive(_domain, address))
					{
						_freezeHighlightBrush.Color = Colors.HighlightFreeze;
						e.Graphics.FillRectangle(_freezeHighlightBrush, rect);
						e.Graphics.FillRectangle(_freezeHighlightBrush, textRect);
					}
					else
					{
						_secondaryHighlightBrush.Color = Color.FromArgb(0x44, Colors.Highlight);
						e.Graphics.FillRectangle(_secondaryHighlightBrush, rect);
						e.Graphics.FillRectangle(_secondaryHighlightBrush, textRect);
					}
				}
			}
		}

		private void AddressesLabel_MouseUp(object sender, MouseEventArgs e)
		{
			_mouseIsDown = false;
		}

		private void AddressesLabel_MouseMove(object sender, MouseEventArgs e)
		{
			_addressOver = GetPointedAddress(e.X, e.Y);

			if (_mouseIsDown)
			{
				DoShiftClick();
				UpdateFormText();
				MemoryViewerBox.Refresh();
			}
		}

		private void AddressesLabel_MouseLeave(object sender, EventArgs e)
		{
			_addressOver = -1;
			MemoryViewerBox.Refresh();
		}

		private void AddressesLabel_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				var pointedAddress = GetPointedAddress(e.X, e.Y);
				if (pointedAddress >= 0)
				{
					if ((ModifierKeys & Keys.Control) == Keys.Control)
					{
						if (pointedAddress == _highlightedAddress)
						{
							ClearHighlighted();
						}
						else if (_secondaryHighlightedAddresses.Contains(pointedAddress))
						{
							_secondaryHighlightedAddresses.Remove(pointedAddress);
						}
						else
						{
							_secondaryHighlightedAddresses.Add(pointedAddress);
						}
					}
					else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
					{
						DoShiftClick();
					}
					else
					{
						_secondaryHighlightedAddresses.Clear();
						_findStr = "";
						SetHighlighted(pointedAddress);
					}

					MemoryViewerBox.Refresh();
				}

				_mouseIsDown = true;
			}
		}

		private bool _programmaticallyChangingValue;
		private void HexScrollBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingValue)
			{
				if (HexScrollBar.Value < 0)
				{
					_programmaticallyChangingValue = true;
					HexScrollBar.Value = 0;
					_programmaticallyChangingValue = false;
				}

				GeneralUpdate();
			}
		}

		private void viewN64MatrixToolStripMenuItem_Click(object sender, EventArgs e)
		{
			static double Wat(uint n)
				=> unchecked((int) n) / 65536.0;
			if (_highlightedAddress is null) return;
			var addr = _highlightedAddress.Value & ~0b11L;
			const int SIZE = 4;
			var raw = new ushort[2 * SIZE * SIZE];
			_domain.BulkPeekUshort(addr.RangeTo(addr + (SIZE * SIZE * sizeof(float) - 1)), bigEndian: true, raw);
			List<List<string>> strings = new();
			for (var y = 0; y < SIZE; y++)
			{
				strings.Add(new());
				for (var x = 0; x < SIZE; x++)
				{
					var i = y * SIZE + x;
					uint n = raw[i];
					n <<= 16;
					n |= raw[SIZE * SIZE + i];
					strings[y].Add(((float) Wat(n)).ToString(CultureInfo.InvariantCulture)); // was going to right-pad, as the previous code did (poorly), but I realised that's not necessary and not really helpful, as well as being hard to get right --yoshi
				}
			}
			using N64MatrixDisplayDialog dialog = new(strings);
			this.ShowDialogAsChild(dialog);
		}
	}
}
