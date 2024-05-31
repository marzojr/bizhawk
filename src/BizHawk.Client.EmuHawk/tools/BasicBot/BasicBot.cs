using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.EmuHawk.ToolExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed partial class BasicBot : ToolFormBase, IToolFormAutoConfig
	{
		private static readonly FilesystemFilterSet BotFilesFSFilterSet = new(new FilesystemFilter("Bot files", new[] { "bot" }));

		public static Icon ToolIcon
			=> Resources.BasicBot;

		private string _currentFileName = "";

		private string CurrentFileName
		{
			get => _currentFileName;
			set
			{
				_currentFileName = value;

				_windowTitle = !string.IsNullOrWhiteSpace(_currentFileName)
					? $"{WindowTitleStatic} - {Path.GetFileNameWithoutExtension(_currentFileName)}"
					: WindowTitleStatic;
				UpdateWindowTitle();
			}

		}

		private bool _isBotting;
		private long _attempts = 1;
		private long _frames;
		private int _targetFrame;
		private bool _oldCountingSetting;
		private BotAttempt _currentBotAttempt;
		private BotAttempt _bestBotAttempt;
		private readonly BotAttempt _comparisonBotAttempt;
		private bool _replayMode;
		private int _startFrame;
		private string _lastRom = "";
		private int _lastFrameAdvanced;

		private bool _doNotUpdateValues;

		private MemoryDomain _currentDomain;
		private bool _bigEndian;
		private int _dataSize;

		private Dictionary<string, double> _cachedControlProbabilities;
		
		private bool _previousDisplayMessage;
		private bool _previousInvisibleEmulation;

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IStatable StatableCore { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[ConfigPersist]
		public BasicBotSettings Settings { get; set; }

		public class BasicBotSettings
		{
			public RecentFiles RecentBotFiles { get; set; } = new RecentFiles();
			public bool TurboWhenBotting { get; set; } = true;
			public bool InvisibleEmulation { get; set; }
		}

		private string _windowTitle = "Basic Bot";

		protected override string WindowTitle => _windowTitle;

		protected override string WindowTitleStatic => "Basic Bot";

		private IMovie CurrentMovie => MovieSession.Movie;

		public BasicBot()
		{
			InitializeComponent();
			Icon = ToolIcon;
			NewMenuItem.Image = Resources.NewFile;
			OpenMenuItem.Image = Resources.OpenFile;
			SaveMenuItem.Image = Resources.SaveAs;
			RecentSubMenu.Image = Resources.Recent;
			RunBtn.Image = Resources.Play;
			BotStatusButton.Image = Resources.Placeholder;
			btnCopyBestInput.Image = Resources.Duplicate;
			PlayBestButton.Image = Resources.Play;
			ClearBestButton.Image = Resources.Close;
			StopBtn.Image = Resources.Stop;
			if (OSTailoredCode.IsUnixHost)
			{
				AutoSize = false;
				Margin = new(0, 0, 0, 8);
			}

			Settings = new BasicBotSettings();

			_comparisonBotAttempt = new BotAttempt();
			_currentBotAttempt = new BotAttempt();
			_bestBotAttempt = new BotAttempt();

			_comparisonBotAttempt.is_Reset = true;
			_currentBotAttempt.is_Reset = true;
			_bestBotAttempt.is_Reset = true;

			MainOperator.SelectedItem = ">=";
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{
			// Movie recording must be active (check TAStudio because opening a project re-loads the ROM,
			// which resets tools before the movie session becomes active)
			if (CurrentMovie.NotActive() && !Tools.IsLoaded<TAStudio>())
			{
				DialogController.ShowMessageBox("In order to use this tool you must be recording a movie.");
				Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			if (Config!.OpposingDirPolicy is not OpposingDirPolicy.Allow)
			{
				DialogController.ShowMessageBox("In order to use this tool, U+D/L+R policy in the controller binds dialog must be set to 'Allow'.");
				Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			if (OSTailoredCode.IsUnixHost) ClientSize = new(707, 587);

			_previousInvisibleEmulation = InvisibleEmulationCheckBox.Checked = Settings.InvisibleEmulation;
			_previousDisplayMessage = Config.DisplayMessages;
		}

		private Dictionary<string, double> ControlProbabilities =>
			ControlProbabilityPanel.Controls
				.OfType<BotControlsRow>()
				.ToDictionary(tkey => tkey.ButtonName, tvalue => tvalue.Probability);
		
		private int SelectedSlot
			=> 1 + StartFromSlotBox.SelectedIndex;

		private long Attempts
		{
			get => _attempts;
			set
			{
				_attempts = value;
				AttemptsLabel.Text = _attempts.ToString();
			}
		}

		private long Frames
		{
			get => _frames;
			set
			{
				_frames = value;
				FramesLabel.Text = _frames.ToString();
			}
		}

		private int FrameLength
		{
			get => (int)FrameLengthNumeric.Value;
			set => FrameLengthNumeric.Value = value;
		}

		private ulong? MaximizeAddress
		{
			get => MaximizeAddressBox.ToU64();
			set => MaximizeAddressBox.SetFromU64(value);
		}

		private int MaximizeValue
			=> GetRamValue(MaximizeAddress);

		private ulong? TieBreaker1Address
		{
			get => TieBreaker1Box.ToU64();
			set => TieBreaker1Box.SetFromU64(value);
		}

		private int TieBreaker1Value
			=> GetRamValue(TieBreaker1Address);

		private ulong? TieBreaker2Address
		{
			get => TieBreaker2Box.ToU64();
			set => TieBreaker2Box.SetFromU64(value);
		}

		private int TieBreaker2Value
			=> GetRamValue(TieBreaker2Address);

		private ulong? TieBreaker3Address
		{
			get => TieBreaker3Box.ToU64();
			set => TieBreaker3Box.SetFromU64(value);
		}

		private int TieBreaker3Value
			=> GetRamValue(TieBreaker3Address);

		public byte MainComparisonType
		{
			get => (byte)MainOperator.SelectedIndex;
			set => MainOperator.SelectedIndex = value < 6 ? value : 0;
		}

		public byte Tie1ComparisonType
		{
			get => (byte)Tiebreak1Operator.SelectedIndex;
			set => Tiebreak1Operator.SelectedIndex = value < 6 ? value : 0;
		}

		public byte Tie2ComparisonType
		{
			get => (byte)Tiebreak2Operator.SelectedIndex;
			set => Tiebreak2Operator.SelectedIndex = value < 6 ? value : 0;
		}

		public byte Tie3ComparisonType
		{
			get => (byte)Tiebreak3Operator.SelectedIndex;
			set => Tiebreak3Operator.SelectedIndex = value < 6 ? value : 0;
		}

		public string FromSlot
		{
			get => StartFromSlotBox.SelectedItem != null
				? StartFromSlotBox.SelectedItem.ToString()
				: "";

			set
			{
				var item = StartFromSlotBox.Items
					.OfType<object>()
					.FirstOrDefault(o => o.ToString() == value);

				StartFromSlotBox.SelectedItem = item;
			}
		}

		// Controls need to be set and synced after emulation, so that everything works out properly at the start of the next frame
		// Consequently, when loading a state, input needs to be set before the load, to ensure everything works out in the correct order

		protected override void UpdateAfter() => Update(fast: false);
		protected override void FastUpdateAfter() => Update(fast: true);

		public override void Restart()
		{
			_ = StatableCore!; // otherwise unused due to loadstating via MainForm; however this service is very much required so the property needs to be present

			if (_currentDomain == null
				|| MemoryDomains.Contains(_currentDomain))
			{
				_currentDomain = MemoryDomains.MainMemory;
				_bigEndian = _currentDomain.EndianType == MemoryDomain.Endian.Big;
				_dataSize = 1;
			}

			if (_isBotting)
			{
				StopBot();
			}
			else if (_replayMode)
			{
				FinishReplay();
			}


			if (_lastRom != MainForm.CurrentlyOpenRom)
			{
				_lastRom = MainForm.CurrentlyOpenRom;
				SetupControlsAndProperties();
			}
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = !string.IsNullOrWhiteSpace(CurrentFileName);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentSubMenu.ReplaceDropDownItems(Settings.RecentBotFiles.RecentMenu(this, LoadFileFromRecent, "Bot Parameters"));

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			CurrentFileName = "";
			_bestBotAttempt.is_Reset = true;

			foreach (var cp in ControlProbabilityPanel.Controls.OfType<BotControlsRow>())
			{
				cp.Probability = 0;
			}

			FrameLength = 0;
			MaximizeAddress = 0;
			TieBreaker1Address = 0;
			TieBreaker2Address = 0;
			TieBreaker3Address = 0;
			StartFromSlotBox.SelectedIndex = 0;
			MainOperator.SelectedIndex = 0;
			Tiebreak1Operator.SelectedIndex = 0;
			Tiebreak2Operator.SelectedIndex = 0;
			Tiebreak3Operator.SelectedIndex = 0;
			MainBestRadio.Checked = true;
			MainValueNumeric.Value = 0;
			TieBreak1Numeric.Value = 0;
			TieBreak2Numeric.Value = 0;
			TieBreak3Numeric.Value = 0;
			TieBreak1BestRadio.Checked = true;
			TieBreak2BestRadio.Checked = true;
			TieBreak3BestRadio.Checked = true;

			UpdateBestAttempt();
			UpdateComparisonBotAttempt();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = OpenFileDialog(
				currentFile: CurrentFileName,
				path: Config!.PathEntries.ToolsAbsolutePath(),
				BotFilesFSFilterSet);

			if (file != null)
			{
				_ = LoadBotFile(file.FullName);
			}
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(CurrentFileName))
			{
				SaveBotFile(CurrentFileName);
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			var fileName = CurrentFileName;
			if (string.IsNullOrWhiteSpace(fileName))
			{
				fileName = Game.FilesystemSafeName();
			}

			var file = SaveFileDialog(
				currentFile: fileName,
				path: Config!.PathEntries.ToolsAbsolutePath(),
				BotFilesFSFilterSet,
				this);

			if (file != null)
			{
				SaveBotFile(file.FullName);
				_currentFileName = file.FullName;
			}
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			TurboWhileBottingMenuItem.Checked = Settings.TurboWhenBotting;
			BigEndianMenuItem.Checked = _bigEndian;
		}

		private void MemoryDomainsMenuItem_DropDownOpened(object sender, EventArgs e)
			=> MemoryDomainsMenuItem.ReplaceDropDownItems(MemoryDomains.MenuItems(SetMemoryDomain, _currentDomain.Name).ToArray());

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
			=> _bigEndian = !_bigEndian;

		private void DataSizeMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			_1ByteMenuItem.Checked = _dataSize == 1;
			_2ByteMenuItem.Checked = _dataSize == 2;
			_4ByteMenuItem.Checked = _dataSize == 4;
		}

		private void OneByteMenuItem_Click(object sender, EventArgs e)
		{
			_dataSize = 1;
		}

		private void TwoByteMenuItem_Click(object sender, EventArgs e)
		{
			_dataSize = 2;
		}

		private void FourByteMenuItem_Click(object sender, EventArgs e)
		{
			_dataSize = 4;
		}

		private void TurboWhileBottingMenuItem_Click(object sender, EventArgs e)
			=> Settings.TurboWhenBotting = !Settings.TurboWhenBotting;

		private void RunBtn_Click(object sender, EventArgs e)
		{
			StartBot();
		}

		private void StopBtn_Click(object sender, EventArgs e)
		{
			StopBot();
		}

		private void ClearBestButton_Click(object sender, EventArgs e)
		{
			_bestBotAttempt.is_Reset = true;
			Attempts = 0;
			Frames = 0;
			UpdateBestAttempt();
			UpdateComparisonBotAttempt();
		}

		private void PlayBestButton_Click(object sender, EventArgs e)
		{
			StopBot();
			_replayMode = true;
			_doNotUpdateValues = true;

			// here we need to apply the initial frame's input from the best attempt
			var logEntry = _bestBotAttempt.Log[0];
			var controller = MovieSession.GenerateMovieController();
			controller.SetFromMnemonic(logEntry);
			foreach (var button in controller.Definition.BoolButtons)
			{
				// TODO: make an input adapter specifically for the bot?
				InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
			}

			InputManager.SyncControls(Emulator, MovieSession, Config);

			_ = MainForm.LoadQuickSave(SelectedSlot, true); // Triggers an UpdateValues call
			_lastFrameAdvanced = Emulator.Frame;
			_doNotUpdateValues = false;
			_startFrame = Emulator.Frame;
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replaying";
			MainForm.UnpauseEmulator();
		}

		private void FrameLengthNumeric_ValueChanged(object sender, EventArgs e)
		{
			AssessRunButtonStatus();
		}

		private void ClearStatsContextMenuItem_Click(object sender, EventArgs e)
		{
			Attempts = 0;
			Frames = 0;
		}

		private class BotAttempt
		{
			public long Attempt { get; set; }
			public int Maximize { get; set; }
			public int TieBreak1 { get; set; }
			public int TieBreak2 { get; set; }
			public int TieBreak3 { get; set; }
			public byte ComparisonTypeMain { get; set; }
			public byte ComparisonTypeTie1 { get; set; }
			public byte ComparisonTypeTie2 { get; set; }
			public byte ComparisonTypeTie3 { get; set; }

			public List<string> Log { get; } = new List<string>();

			public bool is_Reset { get; set; }
		}

		private void reset_curent(long attempt_num)
		{
			_currentBotAttempt.Attempt = attempt_num;
			_currentBotAttempt.Maximize = 0;
			_currentBotAttempt.TieBreak1 = 0;
			_currentBotAttempt.TieBreak2 = 0;
			_currentBotAttempt.TieBreak3 = 0;

			// no references to ComparisonType parameters

			_currentBotAttempt.Log.Clear();

			_currentBotAttempt.is_Reset = true;
		}

		private void copy_curent_to_best()
		{
			_bestBotAttempt.Attempt = _currentBotAttempt.Attempt;
			_bestBotAttempt.Maximize = _currentBotAttempt.Maximize;
			_bestBotAttempt.TieBreak1 = _currentBotAttempt.TieBreak1;
			_bestBotAttempt.TieBreak2 = _currentBotAttempt.TieBreak2;
			_bestBotAttempt.TieBreak3 = _currentBotAttempt.TieBreak3;

			// no references to ComparisonType parameters

			_bestBotAttempt.Log.Clear();

			for (int i = 0; i < _currentBotAttempt.Log.Count; i++)
			{
				_bestBotAttempt.Log.Add(_currentBotAttempt.Log[i]);
			}

			_bestBotAttempt.is_Reset = false;
		}

		private class BotData
		{
			public BotAttempt Best { get; set; }
			public Dictionary<string, double> ControlProbabilities { get; set; }
			public ulong? Maximize { get; set; }
			public ulong? TieBreaker1 { get; set; }
			public ulong? TieBreaker2 { get; set; }
			public ulong? TieBreaker3 { get; set; }
			public byte ComparisonTypeMain { get; set; }
			public byte ComparisonTypeTie1 { get; set; }
			public byte ComparisonTypeTie2 { get; set; }
			public byte ComparisonTypeTie3 { get; set; }
			public bool MainCompareToBest { get; set; } = true;
			public bool TieBreaker1CompareToBest { get; set; } = true;
			public bool TieBreaker2CompareToBest { get; set; } = true;
			public bool TieBreaker3CompareToBest { get; set; } = true;
			public int MainCompareToValue { get; set; }
			public int TieBreaker1CompareToValue { get; set; }
			public int TieBreaker2CompareToValue { get; set; }
			public int TieBreaker3CompareToValue { get; set; }
			public int FrameLength { get; set; }
			public string FromSlot { get; set; }
			public long Attempts { get; set; }
			public long Frames { get; set; }

			public string MemoryDomain { get; set; }
			public bool BigEndian { get; set; }
			public int DataSize { get; set; }

			public string HawkVersion { get; set; }
			public string SysID { get; set; }
			public string CoreName { get; set; }
			public string GameName { get; set; }
		}

		private void LoadFileFromRecent(string path)
		{
			var result = LoadBotFile(path);
			if (!result && !File.Exists(path))
			{
				Settings.RecentBotFiles.HandleLoadError(MainForm, path);
			}
		}

		private bool LoadBotFile(string path)
		{
			BotData botData;
			try
			{
				botData = ConfigService.LoadWithType<BotData>(File.ReadAllText(path));
			}
			catch (Exception e)
			{
				using ExceptionBox dialog = new(e);
				this.ShowDialogAsChild(dialog);
				return false;
			}
			if (botData.SysID != Emulator.SystemId)
			{
				this.ModalMessageBox(text: $"This file was made for a different system ({botData.SysID}).");
				if (!string.IsNullOrEmpty(botData.SysID)) return false; // there's little chance the file would load without throwing, and if it did, it wouldn't be useful
				// else grandfathered (made with old version, sysID unknowable), user has been warned
			}
			// if something else is off, though, let the user decide
			var hawkVersionMatches = VersionInfo.DeveloperBuild || botData.HawkVersion == VersionInfo.GetEmuVersion();
			var coreNameMatches = botData.CoreName == Emulator.Attributes().CoreName;
			var gameNameMatches = botData.GameName == Game.Name;
			if (!(hawkVersionMatches && coreNameMatches && gameNameMatches))
			{
				var s = hawkVersionMatches
					? coreNameMatches
						? string.Empty
						: $" with a different core ({botData.CoreName ?? "unknown"})"
					: coreNameMatches
						? " with a different version of EmuHawk"
						: $" with a different core ({botData.CoreName ?? "unknown"}) on a different version of EmuHawk";
				if (!gameNameMatches) s = $"for a different game ({botData.GameName ?? "unknown"}){s}";
				if (!this.ModalMessageBox2(
					text: $"This file was made {s}. Load it anyway?",
					caption: "Confirm file load",
					icon: EMsgBoxIcon.Question))
				{
					return false;
				}
			}
			try
			{
				LoadBotFileInner(botData, path);
				return true;
			}
			catch (Exception e)
			{
				using ExceptionBox dialog = new(e);
				this.ShowDialogAsChild(dialog);
				return false;
			}
		}

		private void LoadBotFileInner(BotData botData, string path)
		{
			_bestBotAttempt.Attempt = botData.Best.Attempt;
			_bestBotAttempt.Maximize = botData.Best.Maximize;
			_bestBotAttempt.TieBreak1 = botData.Best.TieBreak1;
			_bestBotAttempt.TieBreak2 = botData.Best.TieBreak2;
			_bestBotAttempt.TieBreak3 = botData.Best.TieBreak3;

			// no references to ComparisonType parameters

			_bestBotAttempt.Log.Clear();

			for (int i = 0; i < botData.Best.Log.Count; i++)
			{
				_bestBotAttempt.Log.Add(botData.Best.Log[i]);
			}

			_bestBotAttempt.is_Reset = false;

			var probabilityControls = ControlProbabilityPanel.Controls
					.OfType<BotControlsRow>()
					.ToList();

			foreach (var (button, p) in botData.ControlProbabilities)
			{
				var control = probabilityControls.Single(c => c.ButtonName == button);
				control.Probability = p;
			}

			MaximizeAddress = botData.Maximize;
			TieBreaker1Address = botData.TieBreaker1;
			TieBreaker2Address = botData.TieBreaker2;
			TieBreaker3Address = botData.TieBreaker3;
			try
			{
				MainComparisonType = botData.ComparisonTypeMain;
				Tie1ComparisonType = botData.ComparisonTypeTie1;
				Tie2ComparisonType = botData.ComparisonTypeTie2;
				Tie3ComparisonType = botData.ComparisonTypeTie3;

				MainBestRadio.Checked = botData.MainCompareToBest;
				TieBreak1BestRadio.Checked = botData.TieBreaker1CompareToBest;
				TieBreak2BestRadio.Checked = botData.TieBreaker2CompareToBest;
				TieBreak3BestRadio.Checked = botData.TieBreaker3CompareToBest;
				MainValueRadio.Checked = !botData.MainCompareToBest;
				TieBreak1ValueRadio.Checked = !botData.TieBreaker1CompareToBest;
				TieBreak2ValueRadio.Checked = !botData.TieBreaker2CompareToBest;
				TieBreak3ValueRadio.Checked = !botData.TieBreaker3CompareToBest;

				MainValueNumeric.Value = botData.MainCompareToValue;
				TieBreak1Numeric.Value = botData.TieBreaker1CompareToValue;
				TieBreak2Numeric.Value = botData.TieBreaker2CompareToValue;
				TieBreak3Numeric.Value = botData.TieBreaker3CompareToValue;
			}
			catch
			{
				MainComparisonType = 0;
				Tie1ComparisonType = 0;
				Tie2ComparisonType = 0;
				Tie3ComparisonType = 0;

				MainBestRadio.Checked = true;
				TieBreak1BestRadio.Checked = true;
				TieBreak2BestRadio.Checked = true;
				TieBreak3BestRadio.Checked = true;
				MainBestRadio.Checked = false;
				TieBreak1BestRadio.Checked = false;
				TieBreak2BestRadio.Checked = false;
				TieBreak3BestRadio.Checked = false;

				MainValueNumeric.Value = 0;
				TieBreak1Numeric.Value = 0;
				TieBreak2Numeric.Value = 0;
				TieBreak3Numeric.Value = 0;
			}
			FrameLength = botData.FrameLength;
			FromSlot = botData.FromSlot;
			Attempts = botData.Attempts;
			Frames = botData.Frames;

			_currentDomain = !string.IsNullOrWhiteSpace(botData.MemoryDomain)
					? MemoryDomains[botData.MemoryDomain]
					: MemoryDomains.MainMemory;

			_bigEndian = botData.BigEndian;
			_dataSize = botData.DataSize > 0 ? botData.DataSize : 1;

			UpdateBestAttempt();
			UpdateComparisonBotAttempt();

			if (!_bestBotAttempt.is_Reset)
			{
				PlayBestButton.Enabled = true;
			}

			CurrentFileName = path;
			Settings.RecentBotFiles.Add(CurrentFileName);
			MessageLabel.Text = $"{Path.GetFileNameWithoutExtension(path)} loaded";

			AssessRunButtonStatus();
		}

		private void SaveBotFile(string path)
		{
			var data = new BotData
			{
				Best = _bestBotAttempt,
				ControlProbabilities = ControlProbabilities,
				Maximize = MaximizeAddress,
				TieBreaker1 = TieBreaker1Address,
				TieBreaker2 = TieBreaker2Address,
				TieBreaker3 = TieBreaker3Address,
				ComparisonTypeMain = MainComparisonType,
				ComparisonTypeTie1 = Tie1ComparisonType,
				ComparisonTypeTie2 = Tie2ComparisonType,
				ComparisonTypeTie3 = Tie3ComparisonType,
				MainCompareToBest = MainBestRadio.Checked,
				TieBreaker1CompareToBest = TieBreak1BestRadio.Checked,
				TieBreaker2CompareToBest = TieBreak2BestRadio.Checked,
				TieBreaker3CompareToBest = TieBreak3BestRadio.Checked,
				MainCompareToValue = (int)MainValueNumeric.Value,
				TieBreaker1CompareToValue = (int)TieBreak1Numeric.Value,
				TieBreaker2CompareToValue = (int)TieBreak2Numeric.Value,
				TieBreaker3CompareToValue = (int)TieBreak3Numeric.Value,
				FromSlot = FromSlot,
				FrameLength = FrameLength,
				Attempts = Attempts,
				Frames = Frames,
				MemoryDomain = _currentDomain.Name,
				BigEndian = _bigEndian,
				DataSize = _dataSize,
				HawkVersion = VersionInfo.GetEmuVersion(),
				SysID = Emulator.SystemId,
				CoreName = Emulator.Attributes().CoreName,
				GameName = Game.Name,
			};

			var json = ConfigService.SaveWithType(data);

			File.WriteAllText(path, json);
			CurrentFileName = path;
			Settings.RecentBotFiles.Add(CurrentFileName);
			MessageLabel.Text = $"{Path.GetFileName(CurrentFileName)} saved";
		}

		public bool HasFrameAdvanced()
		{
			// If the emulator frame is different from the last time it tried calling
			// the function then we can continue, otherwise we need to stop.
			return _lastFrameAdvanced != Emulator.Frame;
		}
		private void SetupControlsAndProperties()
		{
			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);

			StartFromSlotBox.SelectedIndex = 0;

			const int startY = 0;
			const int lineHeight = 30;
			const int marginLeft = 15;
			int accumulatedY = 0;
			int count = 0;

			ControlProbabilityPanel.SuspendLayout();
			ControlProbabilityPanel.Controls.Clear();
			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				var control = new BotControlsRow
				{
					ButtonName = button,
					Probability = 0.0,
					Location = new Point(marginLeft, startY + accumulatedY),
					TabIndex = count + 1,
					ProbabilityChangedCallback = AssessRunButtonStatus
				};
				control.Scale(UIHelper.AutoScaleFactor);

				ControlProbabilityPanel.Controls.Add(control);
				accumulatedY += lineHeight;
				count++;
			}

			ControlProbabilityPanel.ResumeLayout();

			if (Settings.RecentBotFiles.AutoLoad)
			{
				LoadFileFromRecent(Settings.RecentBotFiles.MostRecent);
			}

			UpdateBotStatusIcon();
		}

		private void SetMemoryDomain(string name)
		{
			_currentDomain = MemoryDomains[name];
			_bigEndian = _currentDomain!.EndianType == MemoryDomain.Endian.Big;

			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);
		}

		private int GetRamValue(ulong? address)
		{
			if (address is null) return 0;
			var addr = checked((long) address); //TODO MemoryDomain needs converting one day
			var val = _dataSize switch
			{
				1 => _currentDomain.PeekByte(addr),
				2 => _currentDomain.PeekUshort(addr, _bigEndian),
				4 => (int) _currentDomain.PeekUint(addr, _bigEndian),
				_ => _currentDomain.PeekByte(addr)
			};

			return val;
		}

		private void Update(bool fast)
		{
			if (_doNotUpdateValues)
			{
				return;
			}

			if (!HasFrameAdvanced())
			{
				return;
			}

			if (_replayMode)
			{
				int index = Emulator.Frame - _startFrame;

				if (index < _bestBotAttempt.Log.Count)
				{
					var logEntry = _bestBotAttempt.Log[index];
					var controller = MovieSession.GenerateMovieController();
					controller.SetFromMnemonic(logEntry);
					foreach (var button in controller.Definition.BoolButtons)
					{
						// TODO: make an input adapter specifically for the bot?
						InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
					}

					InputManager.SyncControls(Emulator, MovieSession, Config);

					_lastFrameAdvanced = Emulator.Frame;
				}
				else
				{
					FinishReplay();
				}
			}
			else if (_isBotting)
			{

				if (Emulator.Frame >= _targetFrame)
				{
					Attempts++;
					Frames += FrameLength;

					_currentBotAttempt.Maximize = MaximizeValue;
					_currentBotAttempt.TieBreak1 = TieBreaker1Value;
					_currentBotAttempt.TieBreak2 = TieBreaker2Value;
					_currentBotAttempt.TieBreak3 = TieBreaker3Value;
					PlayBestButton.Enabled = true;

					if (_bestBotAttempt.is_Reset || IsBetter(_bestBotAttempt, _currentBotAttempt))
					{
						copy_curent_to_best();
						UpdateBestAttempt();
					}

					reset_curent(Attempts);
					_doNotUpdateValues = true;
					PressButtons(true);
					_ = MainForm.LoadQuickSave(SelectedSlot, true);
					_lastFrameAdvanced = Emulator.Frame;
					_doNotUpdateValues = false;
					return;
				}

				// Before this would have 2 additional hits before the frame even advanced, making the amount of inputs greater than the number of frames to test.
				if (_currentBotAttempt.Log.Count < FrameLength) //aka do not Add more inputs than there are Frames to test
				{
					PressButtons(false);
					_lastFrameAdvanced = Emulator.Frame;
				}
			}
		}

		private void FinishReplay()
		{
			MainForm.PauseEmulator();
			_startFrame = 0;
			_replayMode = false;
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replay stopped";
		}

		private bool IsBetter(BotAttempt comparison, BotAttempt current)
		{
			static bool TestValue(byte operation, int currentValue, int bestValue)
				=> operation switch
				{
					0 => (currentValue > bestValue),
					1 => (currentValue >= bestValue),
					2 => (currentValue == bestValue),
					3 => (currentValue <= bestValue),
					4 => (currentValue < bestValue),
					5 => (currentValue != bestValue),
					_ => false
				};

			if (!TestValue(MainComparisonType, current.Maximize, comparison.Maximize)) return false;
			if (current.Maximize != comparison.Maximize) return true;

			if (!TestValue(Tie1ComparisonType, current.TieBreak1, comparison.TieBreak1)) return false;
			if (current.TieBreak1 != comparison.TieBreak1) return true;

			if (!TestValue(Tie2ComparisonType, current.TieBreak2, comparison.TieBreak2)) return false;
			if (current.TieBreak2 != comparison.TieBreak2) return true;

			if (!TestValue(Tie3ComparisonType, current.TieBreak3, comparison.TieBreak3)) return false;
			/*if (current.TieBreak3 != comparison.TieBreak3)*/ return true;
		}

		private void UpdateBestAttempt()
		{
			if (!_bestBotAttempt.is_Reset)
			{
				ClearBestButton.Enabled = true;
				BestAttemptNumberLabel.Text = _bestBotAttempt.Attempt.ToString();
				BestMaximizeBox.Text = _bestBotAttempt.Maximize.ToString();
				BestTieBreak1Box.Text = _bestBotAttempt.TieBreak1.ToString();
				BestTieBreak2Box.Text = _bestBotAttempt.TieBreak2.ToString();
				BestTieBreak3Box.Text = _bestBotAttempt.TieBreak3.ToString();

				var sb = new StringBuilder();
				foreach (var logEntry in _bestBotAttempt.Log)
				{
					sb.AppendLine(logEntry);
				}
				BestAttemptLogLabel.Text = sb.ToString();
				PlayBestButton.Enabled = true;
				btnCopyBestInput.Enabled = true;
			}
			else
			{
				ClearBestButton.Enabled = false;
				BestAttemptNumberLabel.Text = "";
				BestMaximizeBox.Text = "";
				BestTieBreak1Box.Text = "";
				BestTieBreak2Box.Text = "";
				BestTieBreak3Box.Text = "";
				BestAttemptLogLabel.Text = "";
				PlayBestButton.Enabled = false;
				btnCopyBestInput.Enabled = false;
			}
		}

		private void PressButtons(bool clear_log)
		{
			var rand = new Random((int)DateTime.Now.Ticks);

			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				double probability = _cachedControlProbabilities[button];
				bool pressed = !(rand.Next(100) < probability);

				InputManager.ClickyVirtualPadController.SetBool(button, pressed);
			}
			InputManager.SyncControls(Emulator, MovieSession, Config);

			if (clear_log) { _currentBotAttempt.Log.Clear(); }
			_currentBotAttempt.Log.Add(Bk2LogEntryGenerator.GenerateLogEntry(InputManager.ClickyVirtualPadController));
		}

		private void StartBot()
		{
			var message = CanStart();
			if (!string.IsNullOrWhiteSpace(message))
			{
				DialogController.ShowMessageBox(message);
				return;
			}

			_isBotting = true;
			ControlsBox.Enabled = false;
			StartFromSlotBox.Enabled = false;
			RunBtn.Visible = false;
			StopBtn.Visible = true;
			GoalGroupBox.Enabled = false;
			reset_curent(Attempts);

			if (MovieSession.Movie.IsRecording())
			{
				_oldCountingSetting = MovieSession.Movie.IsCountingRerecords;
				MovieSession.Movie.IsCountingRerecords = false;
			}

			_cachedControlProbabilities = ControlProbabilities;

			_doNotUpdateValues = true;
			PressButtons(true);
			_ = MainForm.LoadQuickSave(SelectedSlot, true); // Triggers an UpdateValues call
			_lastFrameAdvanced = Emulator.Frame;
			_doNotUpdateValues = false;
			_startFrame = Emulator.Frame;

			_targetFrame = Emulator.Frame + (int)FrameLengthNumeric.Value;

			_previousDisplayMessage = Config.DisplayMessages;
			Config.DisplayMessages = false;

			MainForm.UnpauseEmulator();
			if (Settings.TurboWhenBotting)
			{
				SetMaxSpeed();
			}

			if (InvisibleEmulationCheckBox.Checked)
			{
				_previousInvisibleEmulation = MainForm.InvisibleEmulation;
				MainForm.InvisibleEmulation = true;
			}

			UpdateBotStatusIcon();
			MessageLabel.Text = "Running...";
		}

		private string CanStart()
		{
			if (!ControlProbabilities.Any(cp => cp.Value > 0))
			{
				return "At least one control must have a probability greater than 0.";
			}

			if (MaximizeAddress is null) return "A main value address is required.";

			if (FrameLengthNumeric.Value == 0)
			{
				return "A frame count greater than 0 is required";
			}

			return null;
		}

		private void StopBot()
		{
			RunBtn.Visible = true;
			StopBtn.Visible = false;
			_isBotting = false;
			_targetFrame = 0;
			ControlsBox.Enabled = true;
			StartFromSlotBox.Enabled = true;
			_targetFrame = 0;
			reset_curent(0);
			GoalGroupBox.Enabled = true;

			if (MovieSession.Movie.IsRecording())
			{
				MovieSession.Movie.IsCountingRerecords = _oldCountingSetting;
			}

			Config.DisplayMessages = _previousDisplayMessage;
			MainForm.InvisibleEmulation = _previousInvisibleEmulation;
			MainForm.PauseEmulator();
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Bot stopped";
		}

		private void UpdateBotStatusIcon()
		{
			if (_replayMode)
			{
				BotStatusButton.Image = Resources.Play;
				BotStatusButton.ToolTipText = "Replaying best result";
			}
			else if (_isBotting)
			{
				BotStatusButton.Image = Resources.Record;
				BotStatusButton.ToolTipText = "Botting in progress";
			}
			else
			{
				BotStatusButton.Image = Resources.Pause;
				BotStatusButton.ToolTipText = "Bot is currently not running";
			}
		}

		private void SetMaxSpeed()
		{
			MainForm.Unthrottle();
		}

		private void SetNormalSpeed()
		{
			MainForm.Throttle();
		}

		private void AssessRunButtonStatus()
		{
			RunBtn.Enabled =
				FrameLength > 0
				&& !string.IsNullOrWhiteSpace(MaximizeAddressBox.Text)
				&& ControlProbabilities.Any(kvp => kvp.Value > 0);
		}

		/// <summary>
		/// Updates comparison bot attempt with current best bot attempt values for values where the "best" radio button is selected
		/// </summary>
		private void UpdateComparisonBotAttempt()
		{
			if (_bestBotAttempt.is_Reset)
			{
				if (MainBestRadio.Checked)
				{
					_comparisonBotAttempt.Maximize = 0;
				}

				if (TieBreak1BestRadio.Checked)
				{
					_comparisonBotAttempt.TieBreak1 = 0;
				}

				if (TieBreak2BestRadio.Checked)
				{
					_comparisonBotAttempt.TieBreak2 = 0;
				}

				if (TieBreak3BestRadio.Checked)
				{
					_comparisonBotAttempt.TieBreak3 = 0;
				}
			}
			else
			{
				if (MainBestRadio.Checked && _bestBotAttempt.Maximize != _comparisonBotAttempt.Maximize)
				{
					_comparisonBotAttempt.Maximize = _bestBotAttempt.Maximize;
				}

				if (TieBreak1BestRadio.Checked && _bestBotAttempt.TieBreak1 != _comparisonBotAttempt.TieBreak1)
				{
					_comparisonBotAttempt.TieBreak1 = _bestBotAttempt.TieBreak1;
				}

				if (TieBreak2BestRadio.Checked && _bestBotAttempt.TieBreak2 != _comparisonBotAttempt.TieBreak2)
				{
					_comparisonBotAttempt.TieBreak2 = _bestBotAttempt.TieBreak2;
				}

				if (TieBreak3BestRadio.Checked && _bestBotAttempt.TieBreak3 != _comparisonBotAttempt.TieBreak3)
				{
					_comparisonBotAttempt.TieBreak3 = _bestBotAttempt.TieBreak3;
				}
			}
		}

		private void MainBestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				MainValueNumeric.Enabled = false;
				_comparisonBotAttempt.Maximize = _bestBotAttempt?.Maximize ?? 0;
			}
		}

		private void Tiebreak1BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak1Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak1 = _bestBotAttempt?.TieBreak1 ?? 0;
			}
		}

		private void Tiebreak2BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak2Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak2 = _bestBotAttempt?.TieBreak2 ?? 0;
			}
		}

		private void Tiebreak3BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak3Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak3 = _bestBotAttempt?.TieBreak3 ?? 0;
			}
		}

		private void MainValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				MainValueNumeric.Enabled = true;
				_comparisonBotAttempt.Maximize = (int)MainValueNumeric.Value;
			}
		}

		private void TieBreak1ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak1Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak1 = (int)TieBreak1Numeric.Value;
			}
		}

		private void TieBreak2ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak2Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak2 = (int)TieBreak2Numeric.Value;
			}
		}

		private void TieBreak3ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak3Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak3 = (int)TieBreak3Numeric.Value;
			}
		}

		private void MainValueNumeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.Maximize = (int)numericUpDown.Value;
		}

		private void TieBreak1Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.TieBreak1 = (int)numericUpDown.Value;
		}

		private void TieBreak2Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.TieBreak2 = (int)numericUpDown.Value;
		}

		private void TieBreak3Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.TieBreak3 = (int)numericUpDown.Value;
		}

		// Copy to Clipboard
		private void BtnCopyBestInput_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(BestAttemptLogLabel.Text);
		}

		private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Process.Start("https://tasvideos.org/Bizhawk/BasicBot");
		}

		private void InvisibleEmulationCheckBox_CheckedChanged(object sender, EventArgs e)
			=> Settings.InvisibleEmulation = !Settings.InvisibleEmulation;

		private void MaximizeAddressBox_TextChanged(object sender, EventArgs e)
		{
			AssessRunButtonStatus();
		}
	}
}
