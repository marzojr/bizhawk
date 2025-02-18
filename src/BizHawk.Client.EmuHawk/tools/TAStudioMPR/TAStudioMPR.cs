using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;


namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioMPR : ToolFormBase, IToolFormAutoConfig, IControlMainform
	{
		public static readonly FilesystemFilterSet TAStudioProjectsFSFilterSet = new(FilesystemFilter.TAStudioProjects);

		public static Icon ToolIcon
			=> Resources.TAStudioIcon;

		public override bool BlocksInputWhenFocused => IsInMenuLoop;

		public new IMainFormForTools MainForm => base.MainForm;

		public new IMovieSession MovieSession => base.MovieSession;

		// TODO: UI flow that conveniently allows to start from savestate
		public ITasMovie CurrentTasMovie => MovieSession.Movie as ITasMovie;

		public bool IsInMenuLoop { get; private set; }

		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();
		private const string CursorColumnName = "CursorColumn";
		private const string FrameColumnName = "FrameColumn";
		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)
		private UndoHistoryFormMPR _undoForm;
		private Timer _autosaveTimer;

		private readonly int _defaultMainSplitDistance;
		private readonly int _defaultBranchMarkerSplitDistance;

		private bool _initialized;
		private bool _exiting;
		private bool _engaged;

		private bool CanAutoload => Settings.RecentTas.AutoLoad && !string.IsNullOrEmpty(Settings.RecentTas.MostRecent);

		/// <summary>
		/// Gets a value that separates "restore last position" logic from seeking caused by navigation.
		/// TASEditor never kills LastPositionFrame, and it only pauses on it, if it hasn't been greenzoned beforehand and middle mouse button was pressed.
		/// </summary>
		public int LastPositionFrame { get; private set; }

		[ConfigPersist]
		public TAStudioSettingsMPR Settings { get; set; } = new TAStudioSettingsMPR();

		public TAStudioPaletteMPR Palette => Settings.Palette;

		[ConfigPersist]
		public Font TasViewFont { get; set; } = new Font("Arial", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);

		public List<InputRoll> TasViews = new List<InputRoll>();
		public InputRoll CurrentTasView = new InputRoll(); //For various events. Set in TasView_SelectedIndexChanged
		public class TAStudioSettingsMPR
		{
			public TAStudioSettingsMPR()
			{
				RecentTas = new RecentFiles(8);
				AutoPause = true;
				FollowCursor = true;
				ScrollSpeed = 6;
				FollowCursorAlwaysScroll = false;
				FollowCursorScrollMethod = "near";
				AutosaveInterval = 120000;
				AutosaveAsBk2 = false;
				AutosaveAsBackupFile = false;
				BackupPerFileSave = false;
				SingleClickAxisEdit = false;
				OldControlSchemeForBranches = false;
				LoadBranchOnDoubleClick = true;
				CopyIncludesFrameNo = false;
				AutoadjustInput = false;

				// default to taseditor fashion
				DenoteStatesWithIcons = false;
				DenoteStatesWithBGColor = true;
				DenoteMarkersWithIcons = false;
				DenoteMarkersWithBGColor = true;

				Palette = TAStudioPaletteMPR.Default;
			}

			public RecentFiles RecentTas { get; set; }
			public bool AutoPause { get; set; }
			public bool AutoRestoreLastPosition { get; set; }
			public bool FollowCursor { get; set; }
			public int ScrollSpeed { get; set; }
			public bool FollowCursorAlwaysScroll { get; set; }
			public string FollowCursorScrollMethod { get; set; }
			public uint AutosaveInterval { get; set; }
			public bool AutosaveAsBk2 { get; set; }
			public bool AutosaveAsBackupFile { get; set; }
			public bool BackupPerFileSave { get; set; }
			public bool SingleClickAxisEdit { get; set; }
			public bool OldControlSchemeForBranches { get; set; } // branch loading will behave differently depending on the recording mode
			public bool LoadBranchOnDoubleClick { get; set; }
			public bool DenoteStatesWithIcons { get; set; }
			public bool DenoteStatesWithBGColor { get; set; }
			public bool DenoteMarkersWithIcons { get; set; }
			public bool DenoteMarkersWithBGColor { get; set; }
			public int MainVerticalSplitDistance { get; set; }
			public int BranchMarkerSplitDistance { get; set; }
			public bool BindMarkersToInput { get; set; }
			public bool CopyIncludesFrameNo { get; set; }
			public bool AutoadjustInput { get; set; }
			public TAStudioPaletteMPR Palette { get; set; }
			public int MaxUndoSteps { get; set; } = 1000;
		}

		public TAStudioMPR()
		{
			InitializeComponent();

			RecentSubMenu.Image = Resources.Recent;
			recentMacrosToolStripMenuItem.Image = Resources.Recent;
			TASEditorManualOnlineMenuItem.Image = Resources.Help;
			ForumThreadMenuItem.Image = Resources.TAStudio;
			Icon = ToolIcon;

			_defaultMainSplitDistance = MainVertialSplit.SplitterDistance;
			_defaultBranchMarkerSplitDistance = BranchesMarkersSplit.SplitterDistance;

			// TODO: show this at all times or hide it when saving is done?
			ProgressBar.Visible = false;

			WantsToControlStopMovie = true;
			WantsToControlRestartMovie = true;
			TasPlaybackBoxMPR.TastudioMPR = this;
			MarkerControlMPR.TastudioMPR = this;
			BookMarkControlMPR.TastudioMPR = this;

			LastPositionFrame = -1;

			BookMarkControlMPR.LoadedCallback = BranchLoaded;
			BookMarkControlMPR.SavedCallback = BranchSaved;
			BookMarkControlMPR.RemovedCallback = BranchRemoved;
		}

		private void Tastudio_Load(object sender, EventArgs e)
		{
			this.Width = 1050;
			TasView1.QueryItemText += TasView_QueryItemText;
			TasView1.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView1.QueryRowBkColor += TasView_QueryRowBkColor;
			TasView1.QueryItemIcon += TasView_QueryItemIcon;
			TasView1.QueryFrameLag += TasView_QueryFrameLag;
			TasView1.PointedCellChanged += TasView_PointedCellChanged;
			TasView1.MouseLeave += TAStudio_MouseLeave;
			TasView1.CellHovered += (_, e) =>
			{
				if (e.NewCell.RowIndex is null)
				{
					toolTip1.Show(e.NewCell.Column!.Name, TasView1, PointToClient(Cursor.Position));
				}
			};
			this.pasteSelectedRowsFromToolStripMenuItem.DropDownItems.Add(
				new ToolStripMenuItem(
				TasView1.Name,
				null,
				PasteToDestMenuItem_Click)
				);
			TasViews.Add(TasView1);

			//get number of controllers MnemonicMapPlayerController
			int controllersCount = Emulator.ControllerDefinition.ControlsOrdered.Count;
			var startLocation = new System.Drawing.Point(TasView1.Location.X + TasView1.Width + 10, 20);

			//start from "2" since first TasView1 is already defined in designer.
			for (int i = 2; i < controllersCount + 1; i++)
			{
				InputRoll tasView = new InputRoll();
				tasView.Name = "TasView" + i.ToString();
				tasView.Parent = MainVertialSplit.Panel1;
				tasView.Height = TasView1.Height;
				tasView.Location = startLocation;
				startLocation.X += tasView.Width + 10; //for next tasView

				tasView.Anchor = ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left);

				tasView.CellHeightPadding = 0;
				tasView.ChangeSelectionWhenPaging = false;
				tasView.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
				tasView.FullRowSelect = true;
				tasView.InputPaintingMode = true;
				tasView.LetKeysModifySelection = true;
				tasView.Rotatable = true;
				tasView.ScrollMethod = "near";
				tasView.ScrollSpeed = 1;

				tasView.QueryItemText += TasView_QueryItemText;
				tasView.QueryItemBkColor += TasView_QueryItemBkColor;
				tasView.QueryRowBkColor += TasView_QueryRowBkColor;
				tasView.QueryItemIcon += TasView_QueryItemIcon;
				tasView.QueryFrameLag += TasView_QueryFrameLag;
				tasView.PointedCellChanged += TasView_PointedCellChanged;
				tasView.MouseLeave += TAStudio_MouseLeave;

				tasView.CellDropped += TasView_CellDropped;
				tasView.ColumnClick += TasView_ColumnClick;
				tasView.ColumnReordered += TasView_ColumnReordered;
				tasView.ColumnRightClick += TasView_ColumnRightClick;
				tasView.KeyDown += TasView_KeyDown;
				tasView.MouseDoubleClick += TasView_MouseDoubleClick;
				tasView.MouseDown += TasView_MouseDown;
				tasView.MouseEnter += TasView_MouseEnter;
				tasView.MouseMove += TasView_MouseMove;
				tasView.MouseUp += TasView_MouseUp;
				tasView.RightMouseScrolled += TasView_MouseWheel;
				tasView.SelectedIndexChanged += TasView_SelectedIndexChanged;

				tasView.CellHovered += (_, e) =>
				{
					if (e.NewCell.RowIndex is null)
					{
						toolTip1.Show(e.NewCell.Column!.Name, tasView, PointToClient(Cursor.Position));
					}
				};
				this.ConfigSubMenu.DropDownItems.Add(
						new ToolStripMenuItem(
						"Toggle Disable " + tasView.Name,
						null,
						disableTasViewToolStripMenuItem_Click)
						);
				this.pasteSelectedRowsFromToolStripMenuItem.DropDownItems.Add(
						new ToolStripMenuItem(
						tasView.Name,
						null,
						PasteToDestMenuItem_Click)
						);
				TasViews.Add(tasView);
			}


			if (!Engage())
			{
				Close();
				return;
			}

			if (TasViews[1].Rotatable)
			{
				RightClickMenu.Items.AddRange(TasViews[1].GenerateContextMenuItems()
					.ToArray());

				RightClickMenu.Items
					.OfType<ToolStripMenuItem>()
					.First(t => t.Name == "RotateMenuItem")
					.Click += (o, ov) => { CurrentTasMovie.FlagChanges(); };
			}

			for (int i = 0; i < TasViews.Count; i++)
			{
				TasViews[i].ScrollSpeed = Settings.ScrollSpeed;
				TasViews[i].AlwaysScroll = Settings.FollowCursorAlwaysScroll;
				TasViews[i].ScrollMethod = Settings.FollowCursorScrollMethod;

				_autosaveTimer = new Timer(components);
				_autosaveTimer.Tick += AutosaveTimerEventProcessor;
				if (Settings.AutosaveInterval > 0)
				{
					_autosaveTimer.Interval = (int) Settings.AutosaveInterval;
					_autosaveTimer.Start();
				}

				MainVertialSplit.SetDistanceOrDefault(
					Settings.MainVerticalSplitDistance,
					_defaultMainSplitDistance);

				BranchesMarkersSplit.SetDistanceOrDefault(
					Settings.BranchMarkerSplitDistance,
					_defaultBranchMarkerSplitDistance);

				TasViews[i].Font = TasViewFont;
				RefreshDialog();
				_initialized = true;
			}
		}
		private bool LoadMostRecentOrStartNew()
		{
			return LoadFileWithFallback(Settings.RecentTas.MostRecent);
		}

		private bool Engage(int viewIndex = 0)
		{
			_engaged = false;
			MainForm.PauseOnFrame = null;
			MainForm.PauseEmulator();
			bool success = false;

			// Nag if inaccurate core, but not if auto-loading or movie is already loaded
			if (!CanAutoload && MovieSession.Movie.NotActive())
			{
				// Nag but allow the user to continue anyway, so ignore the return value
				MainForm.EnsureCoreIsAccurate();
			}

			// Start Scenario 1: A regular movie is active
			if (MovieSession.Movie.IsActive() && MovieSession.Movie is not ITasMovie)
			{
				var changesString = "Would you like to save the current movie before closing it?";
				if (MovieSession.Movie.Changes)
				{
					changesString = "The current movie has unsaved changes. Would you like to save before closing it?";
				}
				var result = DialogController.ShowMessageBox3(
					"TAStudio will create a new project file from the current movie.\n\n" + changesString,
					"Convert movie",
					EMsgBoxIcon.Question);
				if (result == true)
				{
					MovieSession.Movie.Save();
				}
				else if (result == null)
				{
					return false;
				}

				ConvertCurrentMovieToTasproj();
				success = StartNewMovieWrapper(CurrentTasMovie, isNew: false);
			}

			// Start Scenario 2: A tasproj is already active
			else if (MovieSession.Movie.IsActive() && MovieSession.Movie is ITasMovie)
			{
				success = LoadMovie(CurrentTasMovie, gotoFrame: Emulator.Frame);
				if (!success)
				{
					success = StartNewTasMovie();
				}
			}

			// Start Scenario 3: No movie, but user wants to autoload their last project
			else if (CanAutoload)
			{
				success = LoadMostRecentOrStartNew();
			}

			// Start Scenario 4: No movie, default behavior of engaging tastudio with a new default project
			else
			{
				success = StartNewTasMovie();
			}

			// Attempts to load failed, abort
			if (!success)
			{
				Disengage();
				return false;
			}

			MainForm.AddOnScreenMessage("TAStudio engaged");
			_originalEndAction = Config.Movies.MovieEndAction;
			MainForm.DisableRewind();
			Config.Movies.MovieEndAction = MovieEndAction.Record;
			MainForm.SetMainformMovieInfo();
			MovieSession.ReadOnly = true;
			SetSplicer();

			//Setting the tasview sizes here
			var lastTasViewWidth = 0;
			var lastTasViewLocation = new System.Drawing.Point(0, 20);
			foreach (InputRoll tasView in TasViews)
			{
				var width = 0;

				foreach (var column in tasView.AllColumns)
				{
					width += column.Width;
				}
				tasView.Location = new System.Drawing.Point(lastTasViewLocation.X + lastTasViewWidth + 10, 20);
				tasView.Width = width + 18;// padding for scrollbar
				lastTasViewWidth = tasView.Width;
				lastTasViewLocation = tasView.Location;
			}
			_engaged = true;
			return true;
		}

		private void AutosaveTimerEventProcessor(object sender, EventArgs e)
		{
			if (CurrentTasMovie == null)
			{
				return;
			}

			if (!CurrentTasMovie.Changes || Settings.AutosaveInterval == 0
				|| CurrentTasMovie.Filename == DefaultTasProjName())
			{
				return;
			}

			if (Settings.AutosaveAsBackupFile)
			{
				if (Settings.AutosaveAsBk2)
				{
					SaveTas(saveAsBk2: true, saveBackup: true);
				}
				else
				{
					SaveTas(saveBackup: true);
				}
			}
			else
			{
				if (Settings.AutosaveAsBk2)
				{
					SaveTas(saveAsBk2: true);
				}
				else
				{
					SaveTas();
				}
			}
		}

		private static readonly string[] N64CButtonSuffixes = { " C Up", " C Down", " C Left", " C Right" };

		private void SetUpColumnsPerControl(int viewIndex)
		{
			InputRoll tasView = TasViews[viewIndex];
			tasView.AllColumns.Clear();
			tasView.AllColumns.Add(new(name: CursorColumnName, widthUnscaled: 18, type: ColumnType.Boolean, text: string.Empty));
			tasView.AllColumns.Add(new(name: FrameColumnName, widthUnscaled: 60, text: "Frame#")
			{
				Rotatable = true,
			});

			//viewIndex here is the same as the control group index
			foreach ((string name, string mnemonic0, int maxLength) in MnemonicMapPlayerController(viewIndex))
			{
				var mnemonic = Emulator.SystemId is VSystemID.Raw.N64 && N64CButtonSuffixes.Any(name.EndsWithOrdinal)
					? $"c{mnemonic0.ToUpperInvariant()}" // prepend 'c' to differentiate from L/R buttons -- this only affects the column headers
					: mnemonic0;

				var type = ControllerType.Axes.ContainsKey(name) ? ColumnType.Axis : ColumnType.Boolean;

				tasView.AllColumns.Add(new(
					name: name,
					widthUnscaled: (maxLength * 6) + 14, // magic numbers reused in EditBranchTextPopUp() --feos // not since eb63fa5a9 (before 2.3.3) --yoshi
					type: type,
					text: mnemonic));
			}

			var columnsToHide = tasView.AllColumns
				.Where(c =>
					// todo: make a proper user editable list?
					c.Name == "Power"
					|| c.Name == "Reset"
					|| c.Name == "Light Sensor"
					|| c.Name == "Disc Select"
					|| c.Name == "Disk Index"
					|| c.Name == "Next Drive"
					|| c.Name == "Next Slot"
					|| c.Name == "Insert Disk"
					|| c.Name == "Eject Disk"
					|| c.Name.StartsWithOrdinal("Tilt")
					|| c.Name.StartsWithOrdinal("Key ")
					|| c.Name.StartsWithOrdinal("Open")
					|| c.Name.StartsWithOrdinal("Close")
					|| c.Name.EndsWithOrdinal("Tape")
					|| c.Name.EndsWithOrdinal("Disk")
					|| c.Name.EndsWithOrdinal("Block")
					|| c.Name.EndsWithOrdinal("Status"));

			if (Emulator.SystemId is VSystemID.Raw.N64)
			{
				var fakeAnalogControls = tasView.AllColumns
					.Where(c =>
						c.Name.EndsWithOrdinal("A Up")
						|| c.Name.EndsWithOrdinal("A Down")
						|| c.Name.EndsWithOrdinal("A Left")
						|| c.Name.EndsWithOrdinal("A Right"));

				columnsToHide = columnsToHide.Concat(fakeAnalogControls);
			}

			foreach (var column in columnsToHide)
			{
				column.Visible = false;
			}

			foreach (var column in tasView.VisibleColumns)
			{
				if (InputManager.StickyHoldController.IsSticky(column.Name) || InputManager.StickyAutofireController.IsSticky(column.Name))
				{
					column.Emphasis = true;
				}
			}

			tasView.AllColumns.ColumnsChanged();

		}
		private void SetupCustomPatterns()
		{
			// custom autofire patterns to allow configuring a unique pattern for each button or axis
			BoolPatterns = new AutoPatternBool[ControllerType.BoolButtons.Count];
			AxisPatterns = new AutoPatternAxis[ControllerType.Axes.Count];

			for (int i = 0; i < BoolPatterns.Length; i++)
			{
				// standard 1 on 1 off autofire pattern
				BoolPatterns[i] = new AutoPatternBool(1, 1);
			}

			for (int i = 0; i < AxisPatterns.Length; i++)
			{
				// autohold pattern with the maximum axis range as hold value (bit arbitrary)
				var axisSpec = ControllerType.Axes[ControllerType.Axes[i]];
				AxisPatterns[i] = new AutoPatternAxis([ axisSpec.Range.EndInclusive ]);
			}
		}

		/// <remarks>for Lua</remarks>
		public void AddColumn(string name, string text, int widthUnscaled)
			=> TasView1.AllColumns.Add(new(name: name, widthUnscaled: widthUnscaled, type: ColumnType.Text, text: text));

		public void LoadBranchByIndex(int index) => BookMarkControlMPR.LoadBranchExternal(index);
		public void ClearFramesExternal() => ClearFramesMenuItem_Click(null, null);
		public void InsertFrameExternal() => InsertFrameMenuItem_Click(null, null);
		public void InsertNumFramesExternal() => InsertNumFramesMenuItem_Click(null, null);
		public void DeleteFramesExternal() => DeleteFramesMenuItem_Click(null, null);
		public void CloneFramesExternal() => CloneFramesMenuItem_Click(null, null);
		public void CloneFramesXTimesExternal() => CloneFramesXTimesMenuItem_Click(null, null);
		public void UndoExternal() => UndoMenuItem_Click(null, null);
		public void RedoExternal() => RedoMenuItem_Click(null, null);
		public void SelectBetweenMarkersExternal() => SelectBetweenMarkersMenuItem_Click(null, null);
		public void SelectAllExternal() => SelectAllMenuItem_Click(null, null);
		public void ReselectClipboardExternal() => ReselectClipboardMenuItem_Click(null, null);

		public IMovieController GetBranchInput(string branchId, int frame)
		{
			var branch = CurrentTasMovie.Branches.FirstOrDefault(b => b.Uuid.ToString() == branchId);
			if (branch == null || frame >= branch.InputLog.Count)
			{
				return null;
			}

			var controller = MovieSession.GenerateMovieController();
			controller.SetFromMnemonic(branch.InputLog[frame]);
			return controller;
		}

		private int? FirstNonEmptySelectedFrame
		{
			get
			{
				var empty = Bk2LogEntryGenerator.EmptyEntry(MovieSession.MovieController);
				foreach (var row in TasView1.SelectedRows)
				{
					if (CurrentTasMovie[row].LogEntry != empty)
					{
						return row;
					}
				}

				return null;
			}
		}

		private void ConvertCurrentMovieToTasproj()
		{
			MovieSession.ConvertToTasProj();
			Settings.RecentTas.Add(MovieSession.Movie.Filename);
			MainForm.SetMainformMovieInfo();
		}

		private bool LoadMovie(ITasMovie tasMovie, bool startsFromSavestate = false, int gotoFrame = 0)
		{
			_engaged = false;

			if (!StartNewMovieWrapper(tasMovie, isNew: false))
			{
				return false;
			}

			_engaged = true;
			Settings.RecentTas.Add(CurrentTasMovie.Filename); // only add if it did load

			if (startsFromSavestate)
			{
				GoToFrame(0);
			}
			else if (gotoFrame > 0)
			{
				GoToFrame(gotoFrame);
			}
			else
			{
				GoToFrame(CurrentTasMovie.TasSession.CurrentFrame);
			}

			// clear all selections

			foreach (InputRoll tasView in TasViews)
			{
				tasView.DeselectAll();
			}

			BookMarkControlMPR.Restart();
			MarkerControlMPR.Restart();

			RefreshDialog();
			return true;
		}

		private bool StartNewTasMovie(int viewIndex = 0)
		{
			if (!AskSaveChanges())
			{
				return false;
			}

			if (Game.IsNullInstance()) throw new InvalidOperationException("how is TAStudio open with no game loaded? please report this including as much detail as possible");

			var filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
			var tasMovie = (ITasMovie) MovieSession.Get(filename);
			tasMovie.Author = Config.DefaultAuthor;

			bool success = StartNewMovieWrapper(tasMovie, isNew: true); //here controllers are set in MovieController

			if (success)
			{
				// clear all selections				
				foreach (InputRoll tasView in TasViews)
				{
					tasView.DeselectAll();
				}
				BookMarkControlMPR.Restart();
				MarkerControlMPR.Restart();
				RefreshDialog();
			}

			return success;
		}

		private bool StartNewMovieWrapper(ITasMovie movie, bool isNew)
		{
			_initializing = true;

			movie.InputRollSettingsForSave = () => TasView1.UserSettingsSerialized();
			movie.BindMarkersToInput = Settings.BindMarkersToInput;
			movie.GreenzoneInvalidated = GreenzoneInvalidated;
			movie.ChangeLog.MaxSteps = Settings.MaxUndoSteps;
			movie.PropertyChanged += TasMovie_OnPropertyChanged;

			SuspendLayout();
			WantsToControlStopMovie = false;
			bool result = MainForm.StartNewMovie(movie, isNew);
			WantsToControlStopMovie = true;
			ResumeLayout();
			if (result)
			{
				BookMarkControlMPR.UpdateTextColumnWidth();
				MarkerControlMPR.UpdateTextColumnWidth();
				TastudioPlayMode();
				UpdateWindowTitle();


				if (CurrentTasMovie.InputRollSettings != null)
				{
					for (int i = 0; i < TasViews.Count; i++)
					{
						TasViews[i].LoadSettingsSerialized(CurrentTasMovie.InputRollSettings); //not sure if this is necessary with multiple InputRolls
						SetUpColumnsPerControl(i);
					}
				}
				else
				{
					for (int i = 0; i < TasViews.Count; i++)
					{
						SetUpColumnsPerControl(i);
					}
				}
				SetUpToolStripColumns();
				SetupCustomPatterns();
				UpdateAutoFire();
			}

			_initializing = false;

			return result;
		}

		private void DummyLoadProject(string path)
		{
			if (AskSaveChanges())
			{
				LoadFileWithFallback(path);
			}
		}

		private bool LoadFileWithFallback(string path)
		{
			bool movieLoadSucceeded = false;

			if (!File.Exists(path))
			{
				Settings.RecentTas.HandleLoadError(MainForm, path);
			}
			else
			{
				var movie = MovieSession.Get(path, loadMovie: true);
				var tasMovie = movie as ITasMovie ?? movie.ToTasMovie();
				movieLoadSucceeded = LoadMovie(tasMovie);
			}

			if (!movieLoadSucceeded)
			{
				movieLoadSucceeded = StartNewTasMovie();
				_engaged = true;
			}

			return movieLoadSucceeded;
		}

		private void DummyLoadMacro(string path)
		{
			if (!TasView1.AnyRowsSelected)
			{
				return;
			}

			var loadZone = new MovieZone(path, MainForm, Emulator, MovieSession, Tools)
			{
				Start = TasView1.SelectionStartIndex!.Value,
			};
			loadZone.PlaceZone(CurrentTasMovie, Config);
		}

		private void TastudioToggleReadOnly()
		{
			TasPlaybackBoxMPR.RecordingMode = !TasPlaybackBoxMPR.RecordingMode;
			WasRecording = TasPlaybackBoxMPR.RecordingMode; // hard reset at manual click and hotkey
		}

		private void TastudioPlayMode(bool resetWasRecording = false)
		{
			TasPlaybackBoxMPR.RecordingMode = false;

			// once user started editing, rec mode is unsafe
			if (resetWasRecording)
			{
				WasRecording = TasPlaybackBoxMPR.RecordingMode;
			}
		}

		private void TastudioRecordMode(bool resetWasRecording = false)
		{
			TasPlaybackBoxMPR.RecordingMode = true;

			if (resetWasRecording)
			{
				WasRecording = TasPlaybackBoxMPR.RecordingMode;
			}
		}

		private void TastudioStopMovie()
		{
			MovieSession.StopMovie(false);
			MainForm.SetMainformMovieInfo();
		}

		private void Disengage()
		{
			_engaged = false;
			MainForm.PauseOnFrame = null;
			MainForm.AddOnScreenMessage("TAStudio disengaged");
			Config.Movies.MovieEndAction = _originalEndAction;
			WantsToControlRewind = false;
			MainForm.EnableRewind(true);
			MainForm.SetMainformMovieInfo();
		}

		private const string DefaultTasProjectName = "default";

		// Used when starting a new project
		private string DefaultTasProjName()
		{
			return Path.Combine(
				Config.PathEntries.MovieAbsolutePath(),
				$"{DefaultTasProjectName}.{MovieService.TasMovieExtension}");
		}

		// Used for things like SaveFile dialogs to suggest a name to the user
		private string SuggestedTasProjName()
		{
			return Path.Combine(
				Config.PathEntries.MovieAbsolutePath(),
				$"{Game.FilesystemSafeName()}.{MovieService.TasMovieExtension}");
		}

		private void SaveTas(bool saveAsBk2 = false, bool saveBackup = false)
		{
			if (string.IsNullOrEmpty(CurrentTasMovie.Filename) || CurrentTasMovie.Filename == DefaultTasProjName()) return;

			_autosaveTimer.Stop();
			MessageStatusLabel.Text = saveBackup
				? "Saving backup..."
				: "Saving...";
			MessageStatusLabel.Owner.Update();
			Cursor = Cursors.WaitCursor;

			IMovie movieToSave = CurrentTasMovie;
			if (saveAsBk2)
			{
				movieToSave = CurrentTasMovie.ToBk2();
				movieToSave.Attach(Emulator);
			}

			if (saveBackup)
				movieToSave.SaveBackup();
			else
				movieToSave.Save();

			MessageStatusLabel.Text = saveBackup
				? $"Backup .{(saveAsBk2 ? MovieService.StandardMovieExtension : MovieService.TasMovieExtension)} saved to \"Movie backups\" path."
				: "File saved.";
			Cursor = Cursors.Default;
			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Start();
			}
		}

		private void SaveAsTas()
		{
			_autosaveTimer.Stop();

			var filename = CurrentTasMovie.Filename;
			if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
			{
				filename = SuggestedTasProjName();
			}

			var fileInfo = SaveFileDialog(
				currentFile: filename,
				path: Config!.PathEntries.MovieAbsolutePath(),
				TAStudioProjectsFSFilterSet,
				this);

			if (fileInfo != null)
			{
				MessageStatusLabel.Text = "Saving...";
				MessageStatusLabel.Owner.Update();
				Cursor = Cursors.WaitCursor;
				CurrentTasMovie.Filename = fileInfo.FullName;
				CurrentTasMovie.Save();
				Settings.RecentTas.Add(CurrentTasMovie.Filename);
				MessageStatusLabel.Text = "File saved.";
				Cursor = Cursors.Default;
			}

			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Start();
			}

			UpdateWindowTitle(); // changing the movie's filename does not flag changes, so we need to ensure the window title is always updated
			MainForm.UpdateWindowTitle();
		}

		protected override string WindowTitle
			=> CurrentTasMovie == null
				? "TAStudio"
				: CurrentTasMovie.Changes
					? $"TAStudio - {CurrentTasMovie.Name}*"
					: $"TAStudio - {CurrentTasMovie.Name}";

		protected override string WindowTitleStatic => "TAStudio";

		public IEnumerable<int> GetSelection()
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					return tasView.SelectedRows;
				}
			}
			return null; //something messed up
		}
		// Slow but guarantees the entire dialog refreshes
		private void FullRefresh()
		{
			SetTasViewRowCount();
			foreach (InputRoll tasView in TasViews)
			{
				tasView.Refresh(); // An extra refresh potentially but we need to guarantee
			}

			MarkerControlMPR.UpdateValues();
			BookMarkControlMPR.UpdateValues();

			if (_undoForm != null && !_undoForm.IsDisposed)
			{
				_undoForm.UpdateValues();
			}
		}

		public void RefreshDialog(bool refreshTasView = true, bool refreshBranches = true)
		{
			if (_exiting)
			{
				return;
			}

			if (refreshTasView)
			{
				SetTasViewRowCount();
			}

			MarkerControlMPR?.UpdateValues();

			if (refreshBranches)
			{
				BookMarkControlMPR?.UpdateValues();
			}

			if (_undoForm != null && !_undoForm.IsDisposed)
			{
				_undoForm.UpdateValues();
			}
		}

		public void RefreshForInputChange(int firstChangedFrame)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.IsPartiallyVisible(firstChangedFrame) || firstChangedFrame < tasView.FirstVisibleRow)
				{
					RefreshDialog();
				}
			}
		}

		private void SetTasViewRowCount()
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.RowCount = CurrentTasMovie.InputLogLength + 1;
			}

			_lastRefresh = Emulator.Frame;
		}

		public void DoAutoRestore()
		{
			if (Settings.AutoRestoreLastPosition && LastPositionFrame != -1)
			{
				if (LastPositionFrame > Emulator.Frame) // Don't unpause if we are already on the desired frame, else runaway seek
				{
					StartSeeking(LastPositionFrame);
				}
			}
			else
			{
				if (_autoRestorePaused.HasValue && !_autoRestorePaused.Value)
				{
					// this happens when we're holding the left button while unpaused - view scrolls down, new input gets drawn, seek pauses
					MainForm.UnpauseEmulator();
				}

				_autoRestorePaused = null;
			}
		}

		/// <summary>
		/// Get a savestate prior to the previous frame so code following the call can frame advance and have a framebuffer.
		/// If frame is 0, return the initial state.
		/// </summary>
		private KeyValuePair<int, Stream> GetPriorStateForFramebuffer(int frame)
		{
			return CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame > 0 ? frame - 1 : 0);
		}

		private void StartAtNearestFrameAndEmulate(int frame, bool fromLua, bool fromRewinding)
		{
			if (frame == Emulator.Frame)
			{
				return;
			}

			_unpauseAfterSeeking = (fromRewinding || WasRecording) && !MainForm.EmulatorPaused;
			TastudioPlayMode();
			var closestState = GetPriorStateForFramebuffer(frame);
			if (closestState.Value.Length > 0 && (frame < Emulator.Frame || closestState.Key > Emulator.Frame))
			{
				LoadState(closestState, true);
			}
			closestState.Value.Dispose();

			if (fromLua)
			{
				bool wasPaused = MainForm.EmulatorPaused;

				// why not use this? because I'm not letting the form freely run. it all has to be under this loop.
				// i could use this and then poll StepRunLoop_Core() repeatedly, but.. that's basically what I'm doing
				// PauseOnFrame = frame;

				while (Emulator.Frame != frame)
				{
					MainForm.SeekFrameAdvance();
				}

				if (!wasPaused)
				{
					MainForm.UnpauseEmulator();
				}

				// lua botting users will want to re-activate record mode automatically -- it should be like nothing ever happened
				if (WasRecording)
				{
					TastudioRecordMode();
				}

				// now the next section won't happen since we're at the right spot
			}

			// frame == Emulator.Frame when frame == 0
			if (frame > Emulator.Frame)
			{
				// make seek frame keep up with emulation on fast scrolls
				if (MainForm.EmulatorPaused || MainForm.IsSeeking || fromRewinding || WasRecording)
				{
					StartSeeking(frame);
				}
				else
				{
					// GUI users may want to be protected from clobbering their video when skipping around...
					// well, users who are rewinding aren't. (that gets done through the seeking system in the call above)
					// users who are clicking around.. I don't know.
				}
			}
		}

		public void LoadState(KeyValuePair<int, Stream> state, bool discardApiHawkSurfaces = false)
		{
			StatableEmulator.LoadStateBinary(new BinaryReader(state.Value));

			if (state.Key == 0 && CurrentTasMovie.StartsFromSavestate)
			{
				Emulator.ResetCounters();
			}

			UpdateTools();
			if (discardApiHawkSurfaces)
			{
				DisplayManager.DiscardApiHawkSurfaces();
			}
		}

		public void AddBranchExternal() => BookMarkControlMPR.AddBranchExternal();
		public void RemoveBranchExternal() => BookMarkControlMPR.RemoveBranchExternal();

		private void UpdateTools()
		{
			Tools.UpdateToolsBefore();
			Tools.UpdateToolsAfter();
		}

		public void TogglePause()
		{
			MainForm.TogglePause();
		}

		private void SetSplicer()
		{

			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					var selectedRowCount = tasView.SelectedRows.Count();
					var temp = $"Selected: {selectedRowCount} {(selectedRowCount == 1 ? "frame" : "frames")}, States: {CurrentTasMovie.TasStateManager.Count}";
					if (_tasClipboard.Count > 0) temp += $", Clipboard: {_tasClipboard.Count} {(_tasClipboard.Count == 1 ? "frame" : "frames")}";
					SplicerStatusLabel.Text = temp;
				}
			}
			// TODO: columns selected?

		}

		private void DoTriggeredAutoRestoreIfNeeded()
		{
			// Disable the seek that could have been initiated when painting.
			// This must done before DoAutoRestore, otherwise it would disable the auto-restore seek.
			if (_playbackInterrupted)
			{
				MainForm.PauseOnFrame = null;
			}

			if (_triggerAutoRestore)
			{
				TastudioPlayMode(true); // once user started editing, rec mode is unsafe
				DoAutoRestore();

				_triggerAutoRestore = false;
				_autoRestorePaused = null;
			}

			if (_playbackInterrupted)
			{
				MainForm.UnpauseEmulator();
				_playbackInterrupted = false;
			}
		}

		public void InsertNumFramesMPR(int insertionFrame, int numberOfFrames)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|"
									 //for (int k = i; k >= 0; k--)
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				if (insertionFrame <= CurrentTasMovie.InputLogLength)
				{
					var needsToRollback = CurrentTasView.SelectionStartIndex < Emulator.Frame;

					CurrentTasMovie.InsertEmptyFramesMPR(insertionFrame, startOffset, currentControlLength, numberOfFrames);

					if (needsToRollback)
					{
						GoToLastEmulatedFrameIfNecessary(insertionFrame);
						DoAutoRestore();
					}
					else
					{
						RefreshForInputChange(insertionFrame);
					}
				}
			}
		}
		public void DeleteFrames(int beginningFrame, int numberOfFrames)
		{
			if (beginningFrame < CurrentTasMovie.InputLogLength)
			{
				int[] framesToRemove = Enumerable.Range(beginningFrame, numberOfFrames).ToArray();
				CurrentTasMovie.RemoveFrames(framesToRemove);
				SetSplicer();

				var needsToRollback = beginningFrame < Emulator.Frame;
				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(beginningFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshForInputChange(beginningFrame);
				}
			}
		}
		public void ClearFrames(int beginningFrame, int numberOfFrames)
		{
			for (int i = 0; i < TasViews.Count; i++)
			{
				if (TasViews[i].Focused && TasViews[i].AnyRowsSelected) //moving out of for loop to only have the currently focused tasview cloned
				{
					if (beginningFrame < CurrentTasMovie.InputLogLength)
					{
						int startOffset = 1; //starts with "|"
						for (int k = 0; k < i; k++) //add up inputs to get start string offset
						{
							startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
							startOffset += 1; //add 1 for pipe
						}
						int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[i].Count;

						var needsToRollback = TasViews[i].SelectionStartIndex < Emulator.Frame;
						int last = Math.Min(beginningFrame + numberOfFrames, CurrentTasMovie.InputLogLength);
						foreach (int frame in TasViews[i].SelectedRows)
						{
							CurrentTasMovie.ClearFrameMPR(frame, startOffset, currentControlLength);
						}

						if (needsToRollback)
						{
							GoToLastEmulatedFrameIfNecessary(beginningFrame);
							DoAutoRestore();
						}
						else
						{
							RefreshForInputChange(beginningFrame);
						}
					}
				}
			}
		}
		private void Tastudio_Closing(object sender, FormClosingEventArgs e)
		{
			if (!_initialized)
			{
				return;
			}

			_exiting = true;

			if (AskSaveChanges())
			{
				WantsToControlStopMovie = false;
				TastudioStopMovie();
				Disengage();
			}
			else
			{
				e.Cancel = true;
				_exiting = false;
			}

			_undoForm?.Close();
		}

		/// <summary>
		/// This method is called every time the Changes property is toggled on a <see cref="TasMovie"/> instance.
		/// </summary>
		private void TasMovie_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			UpdateWindowTitle();
		}

		private void TAStudio_DragDrop(object sender, DragEventArgs e)
		{
			// TODO: Maybe this should call Mainform's DragDrop method,
			// since that can file types that are not movies,
			// and it can process multiple files sequentially
			var filePaths = (string[]) e.Data.GetData(DataFormats.FileDrop);
			LoadMovieFile(filePaths[0]);
		}

		private void TAStudio_MouseLeave(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					toolTip1.SetToolTip(tasView, null);
					DoTriggeredAutoRestoreIfNeeded();
				}
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Tab
				|| keyData == (Keys.Shift | Keys.Tab)
				|| keyData == Keys.Space)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private bool AutoAdjustInput()
		{
			var lagLog = CurrentTasMovie[Emulator.Frame - 1]; // Minus one because get frame is +1;
			bool isLag = Emulator.AsInputPollable().IsLagFrame;

			if (lagLog.WasLagged.HasValue)
			{
				if (lagLog.WasLagged.Value && !isLag)
				{
					// remove all consecutive was-lag frames in batch like taseditor
					// current frame has no lag so they will all have to go anyway
					var framesToRemove = new List<int> { Emulator.Frame - 1 };
					for (int frame = Emulator.Frame; CurrentTasMovie[frame].WasLagged.HasValue; frame++)
					{
						if (CurrentTasMovie[frame].WasLagged.Value)
						{
							framesToRemove.Add(frame);
						}
						else
						{
							break;
						}
					}

					// Deleting this frame requires rewinding a frame.
					CurrentTasMovie.ChangeLog.AddInputBind(Emulator.Frame - 1, true, $"Bind Input; Delete {Emulator.Frame - 1}");
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;
					CurrentTasMovie.RemoveFrames(framesToRemove);
					foreach (int f in framesToRemove)
					{
						CurrentTasMovie.LagLog.RemoveHistoryAt(f + 1); // Removes from WasLag
					}
					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;

					CurrentTasMovie.LastPositionStable = true;
					GoToLastEmulatedFrameIfNecessary(Emulator.Frame - 1);
					if (!MainForm.HoldFrameAdvance)
					{
						MainForm.PauseOnFrame = LastPositionFrame;
					}
					return true;
				}

				if (!lagLog.WasLagged.Value && isLag)
				{
					// (it shouldn't need to rewind, since the inserted input wasn't polled)
					CurrentTasMovie.ChangeLog.AddInputBind(Emulator.Frame - 1, false, $"Bind Input; Insert {Emulator.Frame - 1}");
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.InsertInput(Emulator.Frame - 1, CurrentTasMovie.GetInputLogEntry(Emulator.Frame - 2));
					CurrentTasMovie.LagLog.InsertHistoryAt(Emulator.Frame, true);

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					return true;
				}
			}

			return false;
		}

		private void MainVerticalSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.MainVerticalSplitDistance = MainVertialSplit.SplitterDistance;
		}

		private void BranchesMarkersSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.BranchMarkerSplitDistance = BranchesMarkersSplit.SplitterDistance;
		}

		private void TasView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell?.RowIndex != null && !CurrentTasMovie.Markers.IsMarker(e.NewCell.RowIndex.Value))
			{
				var currentMarker = CurrentTasMovie.Markers.Single(m => m.Frame == e.OldCell.RowIndex.Value);
				int newFrame = e.NewCell.RowIndex.Value;
				var newMarker = new TasMovieMarker(newFrame, currentMarker.Message);
				CurrentTasMovie.Markers.Remove(currentMarker);
				CurrentTasMovie.Markers.Add(newMarker);
				RefreshDialog();
			}
		}

		private void TASMenu_MenuActivate(object sender, EventArgs e)
		{
			IsInMenuLoop = true;
		}

		private void TASMenu_MenuDeactivate(object sender, EventArgs e)
		{
			IsInMenuLoop = false;
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}

		private void SetFontMenuItem_Click(object sender, EventArgs e)
		{
			using var fontDialog = new FontDialog
			{
				ShowColor = false,
				Font = TasViews[1].Font
			};
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
			{
				foreach (InputRoll tasView in TasViews)
				{
					tasView.Font = TasViewFont = fontDialog.Font;
					tasView.Refresh();
				}
			}
		}
		private IMovieController ControllerFromMnemonicStr(string inputLogEntry)
		{
			try
			{
				var controller = MovieSession.GenerateMovieController();
				controller.SetFromMnemonic(inputLogEntry);

				return controller;
			}
			catch (Exception)
			{
				DialogController.ShowMessageBox($"Invalid mnemonic string: {inputLogEntry}", "Paste Input failed!");
				return null;
			}
		}

		private IEnumerable<(string Name, string Mnemonic, int MaxLength)> MnemonicMap()
		{
			if (MovieSession.MovieController.Definition.MnemonicsCache is null)
				throw new InvalidOperationException("Can't build mnemonic map with empty mnemonics cache");

			foreach (var playerControls in MovieSession.MovieController.Definition.ControlsOrdered)
			{
				foreach ((string name, AxisSpec? axisSpec) in playerControls)
				{
					if (axisSpec.HasValue)
					{
						string mnemonic = Bk2MnemonicLookup.LookupAxis(name, MovieSession.Movie.SystemID);
						yield return (name, mnemonic, Math.Max(mnemonic.Length, axisSpec.Value.MaxDigits));
					}
					else
					{
						yield return (name, MovieSession.MovieController.Definition.MnemonicsCache[name].ToString(), 1);
					}
				}
			}
		}

		private IEnumerable<(string Name, string Mnemonic, int MaxLength)> MnemonicMapPlayerController(int viewIndex)
		{
			if (MovieSession.MovieController.Definition.MnemonicsCache is null)
				throw new InvalidOperationException("Can't build mnemonic map with empty mnemonics cache");

			if (MovieSession.MovieController.Definition.ControlsOrdered.Count != TasViews.Count)
				throw new InvalidOperationException("Number of Controllers has changed unexpectedly. Select desired Core, Reboot Core, and try again.");

			var playerControls = MovieSession.MovieController.Definition.ControlsOrdered[viewIndex];

			//foreach (var playerControls in MovieSession.MovieController.Definition.ControlsOrdered)
			//{
			foreach ((string name, AxisSpec? axisSpec) in playerControls)
			{
				if (axisSpec.HasValue)
				{
					string mnemonic = Bk2MnemonicLookup.LookupAxis(name, MovieSession.Movie.SystemID);
					yield return (name, mnemonic, Math.Max(mnemonic.Length, axisSpec.Value.MaxDigits));
				}
				else
				{
					yield return (name, MovieSession.MovieController.Definition.MnemonicsCache[name].ToString(), 1);
				}
			}
			//}
		}

		//experimental feature. See if this makes a difference at all.
		private void disableTasViewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var tasViewToToggleDisable = sender.ToString().Substring(15);
			InputRoll tasView = TasViews.Find(t => t.Name == tasViewToToggleDisable);
			tasView.Enabled = !tasView.Enabled;
			tasView.Visible = !tasView.Visible;
		}
	}
}
