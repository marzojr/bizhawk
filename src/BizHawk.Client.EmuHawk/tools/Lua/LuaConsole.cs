using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaConsole : ToolFormBase, IToolFormAutoConfig
	{
		private const string IconColumnName = "Icon";
		private const string ScriptColumnName = "Script";
		private const string PathColumnName = "PathName";

		private static readonly FilesystemFilterSet JustScriptsFSFilterSet = new(FilesystemFilter.LuaScripts);

		private static readonly FilesystemFilterSet ScriptsAndTextFilesFSFilterSet = new(FilesystemFilter.LuaScripts, FilesystemFilter.TextFiles);

		private static readonly FilesystemFilterSet SessionsFSFilterSet = new(new FilesystemFilter("Lua Session Files", new[] { "luases" }));

		public static Icon ToolIcon
			=> Resources.TextDocIcon;

		private readonly LuaAutocompleteInstaller _luaAutoInstaller = new();
		private readonly Dictionary<LuaFile, FileSystemWatcher> _watches = new();

		private readonly int _defaultSplitDistance;
		private LuaFile _lastScriptUsed = null;

		[RequiredService]
		private IEmulator Emulator { get; set; }

		private bool _sortReverse;
		private string _lastColumnSorted;

		private readonly List<string> _consoleCommandHistory = new();
		private int _consoleCommandHistoryIndex = -1;

		public ToolDialogSettings.ColumnList Columns { get; set; }

		public class LuaConsoleSettings
		{
			public LuaConsoleSettings()
			{
				Columns = new List<RollColumn>
				{
					new(name: IconColumnName, widthUnscaled: 22, type: ColumnType.Image, text: " "),
					new(name: ScriptColumnName, widthUnscaled: 92, text: "Script"),
					new(name: PathColumnName, widthUnscaled: 300, text: "Path"),
				};
			}

			public List<RollColumn> Columns { get; set; }

			public bool ReloadOnScriptFileChange { get; set; }
			public bool ToggleAllIfNoneSelected { get; set; } = true;

			public int SplitDistance { get; set; }

			public bool DisableLuaScriptsOnLoad { get; set; }

			public bool WarnedOnceOnOverwrite { get; set; }
		}

		[ConfigPersist]
		public LuaConsoleSettings Settings { get; set; }

		protected override string WindowTitleStatic => "Lua Console";

		public LuaConsole()
		{
			Settings = new LuaConsoleSettings();
			_sortReverse = false;
			_lastColumnSorted = "";

			InitializeComponent();
			ToggleScriptContextItem.Image = Resources.Refresh;
			PauseScriptContextItem.Image = Resources.Pause;
			EditScriptContextItem.Image = Resources.Cut;
			RemoveScriptContextItem.Image = Resources.Close;
			InsertSeperatorContextItem.Image = Resources.InsertSeparator;
			StopAllScriptsContextItem.Image = Resources.Stop;
			ClearRegisteredFunctionsContextItem.Image = Resources.Delete;
			NewSessionMenuItem.Image = Resources.NewFile;
			OpenSessionMenuItem.Image = Resources.OpenFile;
			SaveSessionMenuItem.Image = Resources.SaveAs;
			NewScriptMenuItem.Image = Resources.NewFile;
			OpenScriptMenuItem.Image = Resources.OpenFile;
			RefreshScriptMenuItem.Image = Resources.Refresh;
			ToggleScriptMenuItem.Image = Resources.Checkbox;
			PauseScriptMenuItem.Image = Resources.Pause;
			EditScriptMenuItem.Image = Resources.Cut;
			RemoveScriptMenuItem.Image = Resources.Delete;
			InsertSeparatorMenuItem.Image = Resources.InsertSeparator;
			MoveUpMenuItem.Image = Resources.MoveUp;
			MoveDownMenuItem.Image = Resources.MoveDown;
			StopAllScriptsMenuItem.Image = Resources.Stop;
			RegisterSublimeText2MenuItem.Image = Resources.GreenCheck;
			ClearRegisteredFunctionsLogContextItem.Image = Resources.Delete;
			NewScriptToolbarItem.Image = Resources.NewFile;
			OpenScriptToolbarItem.Image = Resources.OpenFile;
			ToggleScriptToolbarItem.Image = Resources.Checkbox;
			RefreshScriptToolbarItem.Image = Resources.Refresh;
			PauseToolbarItem.Image = Resources.Pause;
			EditToolbarItem.Image = Resources.Cut;
			RemoveScriptToolbarItem.Image = Resources.Delete;
			DuplicateToolbarButton.Image = Resources.Duplicate;
			ClearConsoleToolbarButton.Image = Resources.ClearConsole;
			MoveUpToolbarItem.Image = Resources.MoveUp;
			toolStripButtonMoveDown.Image = Resources.MoveDown;
			InsertSeparatorToolbarItem.Image = Resources.InsertSeparator;
			EraseToolbarItem.Image = Resources.Erase;
			RecentScriptsSubMenu.Image = Resources.Recent;
			Icon = ToolIcon;

			Closing += (o, e) =>
			{
				if (AskSaveChanges())
				{
					Settings.Columns = LuaListView.AllColumns;
					
					DisplayManager.ClearApiHawkSurfaces();
					DisplayManager.ClearApiHawkTextureCache();
					ResetDrawSurfacePadding();
					ClearFileWatches();
					LuaImp?.Close();
					DisplayManager.OSD.ClearGuiText();
				}
				else
				{
					e.Cancel = true;
				}
			};

			LuaListView.QueryItemText += LuaListView_QueryItemText;
			LuaListView.QueryItemBkColor += LuaListView_QueryItemBkColor;
			LuaListView.QueryItemIcon += LuaListView_QueryItemImage;

			// this is bad, in case we ever have more than one gui part running lua.. not sure how much other badness there is like that
			LuaSandbox.DefaultLogger = WriteToOutputWindow;
			_defaultSplitDistance = splitContainer1.SplitterDistance;
		}

		public ILuaLibraries LuaImp { get; private set; }

		private IEnumerable<LuaFile> SelectedItems =>  LuaListView.SelectedRows.Select(index => LuaImp.ScriptList[index]);

		private IEnumerable<LuaFile> SelectedFiles => SelectedItems.Where(x => !x.IsSeparator);

		private void LuaConsole_Load(object sender, EventArgs e)
		{
			if (Config.RecentLuaSession.AutoLoad && !Config.RecentLuaSession.Empty)
			{
				LoadSessionFromRecent(Config.RecentLuaSession.MostRecent);
			}
			else if (Config.RecentLua.AutoLoad)
			{
				if (!Config.RecentLua.Empty)
				{
					LoadLuaFile(Config.RecentLua.MostRecent);
				}
			}

			LuaListView.AllColumns.Clear();
			SetColumns();

			splitContainer1.SetDistanceOrDefault(Settings.SplitDistance, _defaultSplitDistance);
		}

		private void BranchesMarkersSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.SplitDistance = splitContainer1.SplitterDistance;
		}

		public override void Restart()
		{
			List<LuaFile> runningScripts = new();

			// Things we need to do with the existing LuaImp before we can make a new one
			if (LuaImp is not null)
			{
				if (LuaImp.IsRebootingCore)
				{
					// Even if the lua console is self-rebooting from client.reboot_core() we still want to re-inject dependencies
					LuaImp.Restart(Emulator.ServiceProvider, Config, Emulator, Game);
					return;
				}

				runningScripts = LuaImp.ScriptList.Where(lf => lf.Enabled).ToList();

				// we don't use runningScripts here as the other scripts need to be stopped too
				foreach (var file in LuaImp.ScriptList)
				{
					DisableLuaScript(file);
				}
			}

			LuaFileList newScripts = new(LuaImp?.ScriptList, onChanged: SessionChangedCallback);
			LuaFunctionList registeredFuncList = new(onChanged: UpdateRegisteredFunctionsDialog);
			LuaImp?.Close();
			LuaImp = new LuaLibraries(
				newScripts,
				registeredFuncList,
				Emulator.ServiceProvider,
				(MainForm) MainForm, //HACK
				DisplayManager,
				InputManager,
				Config,
				Emulator,
				Game);

			InputBox.AutoCompleteCustomSource.AddRange(LuaImp.Docs.Where(static f => f.SuggestInREPL)
				.Select(static f => $"{f.Library}.{f.Name}")
				.ToArray());

			foreach (var file in runningScripts)
			{
				try
				{
					LuaSandbox.Sandbox(file.Thread, () =>
					{
						LuaImp.SpawnAndSetFileThread(file.Path, file);
						LuaSandbox.CreateSandbox(file.Thread, Path.GetDirectoryName(file.Path));
						file.State = LuaFile.RunState.Running;
					}, () =>
					{
						file.State = LuaFile.RunState.Disabled;
					});
				}
				catch (Exception ex)
				{
					DialogController.ShowMessageBox(ex.ToString());
				}
			}

			UpdateDialog();
		}

		public void ToggleLastLuaScript()
		{
			if (_lastScriptUsed is not null)
			{
				ToggleLuaScript(_lastScriptUsed);
			}
		}

		private void SetColumns()
		{
			LuaListView.AllColumns.AddRange(Settings.Columns);
			LuaListView.Refresh();
		}

		private void AddFileWatches()
		{
			if (Settings.ReloadOnScriptFileChange)
			{
				ClearFileWatches();
				foreach (var item in LuaImp.ScriptList.Where(s => !s.IsSeparator))
				{
					CreateFileWatcher(item);
				}
			}
		}

		private void ClearFileWatches()
		{
			foreach (var watch in _watches.Values)
				watch.Dispose();
			_watches.Clear();
		}

		private void CreateFileWatcher(LuaFile item)
		{
			if (_watches.ContainsKey(item))
				return;

			var (dir, file) = item.Path.SplitPathToDirAndFile();

			// prevent error when (auto)loading session referencing scripts in deleted/renamed directories
			if (!Directory.Exists(dir))
				return;

			var watcher = new FileSystemWatcher
			{
				Path = dir,
				Filter = file,
				NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
				EnableRaisingEvents = true,
				SynchronizingObject = this, // invoke event handlers on GUI thread
			};

			// TODO, Deleted and Renamed events
			watcher.Changed += (_, _) => OnLuaFileChanged(item);

			_watches.Add(item, watcher);
		}

		private void RemoveFileWatcher(LuaFile item)
		{
			if (_watches.TryGetValue(item, out var watcher))
			{
				_watches.Remove(item);
				watcher.Dispose();
			}
		}

		private void OnLuaFileChanged(LuaFile item)
		{
			if (item.Enabled && LuaImp.ScriptList.Contains(item))
			{
				RefreshLuaScript(item);
				if (!item.Enabled)
				{
					UpdateDialog();
				}
			}
		}

		public void LoadLuaFile(string path)
		{
			var absolutePath = Path.GetFullPath(path);

			var alreadyLoadedFile = LuaImp.ScriptList.FirstOrDefault(t => absolutePath == t.Path);
			if (alreadyLoadedFile is not null)
			{
				if (!alreadyLoadedFile.Enabled && !Settings.DisableLuaScriptsOnLoad)
				{
					ToggleLuaScript(alreadyLoadedFile);
				}
			}
			else
			{
				var luaFile = new LuaFile("", absolutePath);

				LuaImp.ScriptList.Add(luaFile);
				LuaListView.RowCount = LuaImp.ScriptList.Count;
				Config.RecentLua.Add(absolutePath);

				if (!Settings.DisableLuaScriptsOnLoad)
				{
					luaFile.State = LuaFile.RunState.Running;
					EnableLuaFile(luaFile);
				}
				else
				{
					luaFile.State = LuaFile.RunState.Disabled;
				}

				if (Settings.ReloadOnScriptFileChange)
				{
					CreateFileWatcher(luaFile);
				}
			}

			UpdateDialog();
		}

		public void RemoveLuaFile(string path)
		{
			var absolutePath = Path.GetFullPath(path);

			var luaFile = LuaImp.ScriptList.FirstOrDefault(t => absolutePath == t.Path);
			if (luaFile is not null)
			{
				RemoveLuaFile(luaFile);
				UpdateDialog();
			}
		}

		private void RemoveLuaFile(LuaFile item)
		{
			if (!item.IsSeparator)
			{
				DisableLuaScript(item);
				RemoveFileWatcher(item);
			}
			LuaImp.ScriptList.Remove(item);
		}

		private void RemoveAllLuaFiles()
		{
			while (LuaImp.ScriptList.Count > 0)
			{
				RemoveLuaFile(LuaImp.ScriptList[LuaImp.ScriptList.Count - 1]);
			}
		}

		private void UpdateDialog()
		{
			LuaListView.RowCount = LuaImp.ScriptList.Count;
			UpdateNumberOfScripts();
			UpdateRegisteredFunctionsDialog();
		}

		private void SessionChangedCallback()
		{
			OutputMessages.Text =
				(LuaImp.ScriptList.Changes ? "* " : "") +
				Path.GetFileName(LuaImp.ScriptList.Filename);
		}

		private void LuaListView_QueryItemImage(int index, RollColumn column, ref Bitmap bitmap, ref int offsetX, ref int offsetY)
		{
			if (column.Name != IconColumnName)
			{
				return;
			}

			if (LuaImp.ScriptList[index].IsSeparator)
			{
				return;
			}

			bitmap = LuaImp.ScriptList[index].State switch
			{
				LuaFile.RunState.Running => Resources.ts_h_arrow_green,
				LuaFile.RunState.Paused => Resources.Pause,
				_ => Resources.Stop
			};
		}

		private void LuaListView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			var lf = LuaImp.ScriptList[index];
			if (lf.IsSeparator) color = BackColor;
			else if (lf.Paused) color = Color.LightPink;
			else if (lf.Enabled) color = Color.LightCyan;
		}

		private void LuaListView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			if (LuaImp.ScriptList[index].IsSeparator)
			{
				return;
			}

			if (column.Name == ScriptColumnName)
			{
				text = Path.GetFileNameWithoutExtension(LuaImp.ScriptList[index].Path); // TODO: how about allow the user to name scripts?
			}
			else if (column.Name == PathColumnName)
			{
				text = DressUpRelative(LuaImp.ScriptList[index].Path);
			}
		}

		private string DressUpRelative(string path)
		{
			return path.StartsWithOrdinal(".\\") ? path.Replace(".\\", "") : path;
		}

		private void UpdateNumberOfScripts()
		{
			var message = "";
			var total = LuaImp.ScriptList.Count(file => !file.IsSeparator);
			var active = LuaImp.ScriptList.Count(file => !file.IsSeparator && file.Enabled);
			var paused = LuaImp.ScriptList.Count(static lf => !lf.IsSeparator && lf.Paused);

			if (total == 1)
			{
				message += $"{total} script ({active} active, {paused} paused)";
			}
			else if (total == 0)
			{
				message += $"{total} scripts";
			}
			else
			{
				message += $"{total} scripts ({active} active, {paused} paused)";
			}

			NumberOfScripts.Text = message;
		}

		private void WriteLine(string message) => WriteToOutputWindow(message + "\n");

		private int _messageCount;
		private const int MaxCount = 100;
		public void WriteToOutputWindow(string message)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			_messageCount++;
			if (_messageCount > MaxCount) return;
			if (_messageCount == MaxCount) message += "\nFlood warning! Message cap reached, suppressing output.\n";
			OutputBox.Invoke(() =>
			{
				OutputBox.Text += message;
				OutputBox.SelectionStart = OutputBox.Text.Length;
				OutputBox.ScrollToCaret();
			});
			
		}

		public void ClearOutputWindow()
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.Text = "";
				OutputBox.Refresh();
			});
		}

		public bool LoadLuaSession(string path)
		{
			RemoveAllLuaFiles();

			var result = LuaImp.ScriptList.Load(path, Settings.DisableLuaScriptsOnLoad);

			foreach (var script in LuaImp.ScriptList)
			{
				if (!script.IsSeparator)
				{
					if (script.Enabled)
					{
						EnableLuaFile(script);
					}

					Config.RecentLua.Add(script.Path);
				}
			}

			LuaImp.ScriptList.Changes = false;
			Config.RecentLuaSession.Add(path);
			UpdateDialog();
			AddFileWatches();

			ClearOutputWindow();
			return result;
		}

		protected override void UpdateBefore()
		{
			if (LuaImp.IsUpdateSupressed)
			{
				return;
			}

			LuaImp.CallFrameBeforeEvent();
		}

		protected override void UpdateAfter()
		{
			if (LuaImp.IsUpdateSupressed)
			{
				return;
			}

			LuaImp.CallFrameAfterEvent();
			ResumeScripts(true);
		}

		protected override void FastUpdateBefore()
		{
			if (Config.RunLuaDuringTurbo)
			{
				UpdateBefore();
			}
		}

		protected override void FastUpdateAfter()
		{
			if (Config.RunLuaDuringTurbo)
			{
				UpdateAfter();
			}
		}

		private void ResetDrawSurfacePadding()
		{
			var resized = false;
			if (DisplayManager.ClientExtraPadding != (0, 0, 0, 0))
			{
				DisplayManager.ClientExtraPadding = (0, 0, 0, 0);
				resized = true;
			}
			if (DisplayManager.GameExtraPadding != (0, 0, 0, 0))
			{
				DisplayManager.GameExtraPadding = (0, 0, 0, 0);
				resized = true;
			}
			if (resized) MainForm.FrameBufferResized();
		}

		/// <summary>
		/// resumes suspended Co-routines
		/// </summary>
		/// <param name="includeFrameWaiters">should frame waiters be waken up? only use this immediately before a frame of emulation</param>
		public void ResumeScripts(bool includeFrameWaiters)
		{
			if (!LuaImp.ScriptList.Any()
				|| LuaImp.IsUpdateSupressed
				|| (MainForm.IsTurboing && !Config.RunLuaDuringTurbo))
			{
				return;
			}

			foreach (var lf in LuaImp.ScriptList.Where(static lf => lf.State is LuaFile.RunState.Running && lf.Thread is not null))
			{
				try
				{
					LuaSandbox.Sandbox(lf.Thread, () =>
					{
						var prohibit = lf.FrameWaiting && !includeFrameWaiters;
						if (!prohibit)
						{
							var (waitForFrame, terminated) = LuaImp.ResumeScript(lf);
							if (terminated)
							{
								LuaImp.CallExitEvent(lf);
								lf.Stop();
								DetachRegisteredFunctions(lf);
								UpdateDialog();
							}

							lf.FrameWaiting = waitForFrame;
						}
					}, () =>
					{
						lf.Stop();
						DetachRegisteredFunctions(lf);
						LuaListView.Refresh();
					});
				}
				catch (Exception ex)
				{
					DialogController.ShowMessageBox(ex.ToString());
				}
			}

			_messageCount = 0;
		}

		private void DetachRegisteredFunctions(LuaFile lf)
		{
			foreach (var nlf in LuaImp.RegisteredFunctions
				.Where(f => f.LuaFile == lf))
			{
				nlf.DetachFromScript();
			}
		}

		private FileInfo GetSaveFileFromUser()
		{
			string initDir;
			string initFileName;
			if (!string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename))
			{
				(initDir, initFileName, _) = LuaImp.ScriptList.Filename.SplitPathToDirFileAndExt();
			}
			else
			{
				initDir = Config!.PathEntries.LuaAbsolutePath();
				initFileName = Game.IsNullInstance() ? "NULL" : Game.FilesystemSafeName();
			}
			var result = this.ShowFileSaveDialog(
				discardCWDChange: true,
				filter: SessionsFSFilterSet,
				initDir: initDir,
				initFileName: initFileName);
			return result is not null ? new FileInfo(result) : null;
		}

		private void SaveSessionAs()
		{
			var file = GetSaveFileFromUser();
			if (file != null)
			{
				LuaImp.ScriptList.Save(file.FullName);
				Config.RecentLuaSession.Add(file.FullName);
				OutputMessages.Text = $"{file.Name} saved.";
			}
		}

		private void LoadSessionFromRecent(string path)
		{
			var load = true;
			if (LuaImp.ScriptList.Changes)
			{
				load = AskSaveChanges();
			}

			if (load)
			{
				if (!LoadLuaSession(path))
				{
					Config.RecentLuaSession.HandleLoadError(MainForm, path);
				}
			}
		}

		public override bool AskSaveChanges()
		{
			if (!LuaImp.ScriptList.Changes || string.IsNullOrEmpty(LuaImp.ScriptList.Filename)) return true;
			var result = DialogController.DoWithTempMute(() => this.ModalMessageBox3(
				caption: "Closing with Unsaved Changes",
				icon: EMsgBoxIcon.Question,
				text: $"Save {WindowTitleStatic} session?"));
			if (result is null) return false;
			if (result.Value) SaveOrSaveAs();
			else LuaImp.ScriptList.Changes = false;
			return true;
		}

		private void UpdateRegisteredFunctionsDialog()
		{
			if (LuaImp is null) return;

			foreach (var form in Application.OpenForms.OfType<LuaRegisteredFunctionsList>().ToList())
			{
				form.UpdateValues(LuaImp.RegisteredFunctions);
			}
		}

		private void SaveOrSaveAs()
		{
			if (!string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename))
			{
				LuaImp.ScriptList.Save(LuaImp.ScriptList.Filename);
				Config.RecentLuaSession.Add(LuaImp.ScriptList.Filename);
			}
			else
			{
				SaveSessionAs();
			}
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveSessionMenuItem.Enabled = LuaImp.ScriptList.Changes;
		}

		private void RecentSessionsSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentSessionsSubMenu.ReplaceDropDownItems(Config!.RecentLuaSession.RecentMenu(this, LoadSessionFromRecent, "Session"));

		private void RecentScriptsSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentScriptsSubMenu.ReplaceDropDownItems(Config!.RecentLua.RecentMenu(this, LoadLuaFile, "Script"));

		private void NewSessionMenuItem_Click(object sender, EventArgs e)
		{
			var result = !LuaImp.ScriptList.Changes || AskSaveChanges();

			if (result)
			{
				RemoveAllLuaFiles();
				LuaImp.ScriptList.Clear();
				ClearOutputWindow();
				UpdateDialog();
			}
		}

		private void OpenSessionMenuItem_Click(object sender, EventArgs e)
		{
			var initDir = Config!.PathEntries.LuaAbsolutePath();
			Directory.CreateDirectory(initDir);
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: SessionsFSFilterSet,
				initDir: initDir);
			if (result is not null) LoadLuaSession(result);
		}

		private void SaveSessionMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaImp.ScriptList.Changes)
			{
				SaveOrSaveAs();
				OutputMessages.Text = $"{Path.GetFileName(LuaImp.ScriptList.Filename)} saved.";
			}
		}

		private void SaveSessionAsMenuItem_Click(object sender, EventArgs e)
		{
			SaveSessionAs();
		}

		private void ScriptSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToggleScriptMenuItem.Enabled =
				PauseScriptMenuItem.Enabled =
				EditScriptMenuItem.Enabled =
				SelectedFiles.Any();

			RemoveScriptMenuItem.Enabled =
				DuplicateScriptMenuItem.Enabled =
				MoveUpMenuItem.Enabled =
				MoveDownMenuItem.Enabled =
					LuaListView.AnyRowsSelected;

			SelectAllMenuItem.Enabled = LuaImp.ScriptList.Any();
			StopAllScriptsMenuItem.Enabled = LuaImp.ScriptList.Any(script => script.Enabled);
			RegisteredFunctionsMenuItem.Enabled = LuaImp.RegisteredFunctions.Any();
		}

		private void NewScriptMenuItem_Click(object sender, EventArgs e)
		{
			var luaDir = Config!.PathEntries.LuaAbsolutePath();
			string initDir;
			string ext;
			if (!string.IsNullOrWhiteSpace(LuaImp.ScriptList.Filename))
			{
				(initDir, ext, _) = LuaImp.ScriptList.Filename.SplitPathToDirFileAndExt();
			}
			else
			{
				initDir = luaDir;
				ext = Path.GetFileNameWithoutExtension(Game.Name);
			}
			var result = this.ShowFileSaveDialog(
				fileExt: ".lua",
				filter: JustScriptsFSFilterSet,
				initDir: initDir,
				initFileName: ext);
			if (string.IsNullOrWhiteSpace(result)) return;
			const string TEMPLATE_FILENAME = ".template.lua";
			var templatePath = Path.Combine(luaDir, TEMPLATE_FILENAME);
			const string DEF_TEMPLATE_CONTENTS = "-- This template lives at `.../Lua/.template.lua`.\nwhile true do\n\t-- Code here will run once when the script is loaded, then after each emulated frame.\n\temu.frameadvance();\nend\n";
			if (!File.Exists(templatePath)) File.WriteAllText(path: templatePath, contents: DEF_TEMPLATE_CONTENTS);
			if (!Settings.WarnedOnceOnOverwrite && File.Exists(result))
			{
				// the user normally gets an "are you sure you want to overwrite" message from the OS
				// but some newcomer users seem to think the New Script button is for opening up scripts
				// mostly due to weird behavior in other emulators with their lua implementations
				// we'll warn again the first time, clarifying usage then let the OS handle warning the user
				Settings.WarnedOnceOnOverwrite = true;
				if (!this.ModalMessageBox2("You are about to overwrite an existing Lua script.\n" +
						"Keep in mind the \"New Lua Script\" option is for creating a brand new Lua script, not for opening Lua scripts.\n" +
						"This warning will not appear again! (the file manager would be warning you about an overwrite anyways)\n" +
						"Proceed with overwrite?", "Overwrite", EMsgBoxIcon.Warning, useOKCancel: true))
				{
					return;
				}
			}
			File.Copy(sourceFileName: templatePath, destFileName: result, overwrite: true);
			LuaImp.ScriptList.Add(new LuaFile(Path.GetFileNameWithoutExtension(result), result));
			Config!.RecentLua.Add(result);
			UpdateDialog();
			Process.Start(new ProcessStartInfo
			{
				Verb = "Open",
				FileName = result,
			});
			AddFileWatches();
		}

		private void OpenScriptMenuItem_Click(object sender, EventArgs e)
		{
			var initDir = Config!.PathEntries.LuaAbsolutePath();
			Directory.CreateDirectory(initDir);
			var result = this.ShowFileMultiOpenDialog(
				discardCWDChange: true,
				filter: ScriptsAndTextFilesFSFilterSet,
				initDir: initDir);
			if (result is null) return;
			foreach (var file in result)
			{
				LoadLuaFile(file);
				Config.RecentLua.Add(file);
			}

			UpdateDialog();
		}

		private void ToggleScriptMenuItem_Click(object sender, EventArgs e)
		{
			var files = !SelectedFiles.Any() && Settings.ToggleAllIfNoneSelected
				? LuaImp.ScriptList
				: SelectedFiles;
			foreach (var file in files)
			{
				ToggleLuaScript(file);
			}

			UpdateDialog();
		}

		private void EnableLuaFile(LuaFile item)
		{
			try
			{
				LuaSandbox.Sandbox(null, () =>
				{
					LuaImp.SpawnAndSetFileThread(item.Path, item);
					LuaSandbox.CreateSandbox(item.Thread, Path.GetDirectoryName(item.Path));
				}, () =>
				{
					item.State = LuaFile.RunState.Disabled;
				});

				// there used to be a call here which did a redraw of the Gui/OSD, which included a call to `Tools.UpdateToolsAfter` --yoshi
			}
			catch (IOException)
			{
				item.State = LuaFile.RunState.Disabled;
				WriteLine($"Unable to access file {item.Path}");
			}
			catch (Exception ex)
			{
				DialogController.ShowMessageBox(ex.ToString());
			}
		}

		private void PauseScriptMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var s in SelectedFiles)
			{
				s.TogglePause();
			}

			UpdateDialog();
		}

		private void EditScriptMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var file in SelectedFiles)
			{
				Process.Start(new ProcessStartInfo
				{
					Verb = "Open",
					FileName = file.Path
				});
			}
		}

		private void RemoveScriptMenuItem_Click(object sender, EventArgs e)
		{
			var items = SelectedItems.ToList();
			if (items.Any())
			{
				foreach (var item in items)
				{
					RemoveLuaFile(item);
				}

				UpdateDialog();
				DisplayManager.ClearApiHawkSurfaces();
				DisplayManager.ClearApiHawkTextureCache();
				DisplayManager.OSD.ClearGuiText();
				if (!LuaImp.ScriptList.Any(static lf => !lf.IsSeparator)) ResetDrawSurfacePadding(); // just removed last script, reset padding
			}
		}

		private void DuplicateScriptMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaListView.AnyRowsSelected)
			{
				var script = SelectedItems.First();

				if (script.IsSeparator)
				{
					LuaImp.ScriptList.Add(LuaFile.SeparatorInstance);
					UpdateDialog();
					return;
				}

				var (dir, fileNoExt, _) = script.Path.SplitPathToDirFileAndExt();
				var result = this.ShowFileSaveDialog(
					fileExt: ".lua",
					filter: JustScriptsFSFilterSet,
					initDir: dir,
					initFileName: $"{fileNoExt} (1)");
				if (result is null) return;
				string text = File.ReadAllText(script.Path);
				File.WriteAllText(result, text);
				LuaImp.ScriptList.Add(new LuaFile(Path.GetFileNameWithoutExtension(result), result));
				Config!.RecentLua.Add(result);
				UpdateDialog();
				Process.Start(new ProcessStartInfo
				{
					Verb = "Open",
					FileName = result,
				});
			}
		}

		private void ClearConsoleMenuItem_Click(object sender, EventArgs e)
		{
			ClearOutputWindow();
		}

		private void InsertSeparatorMenuItem_Click(object sender, EventArgs e)
		{
			LuaImp.ScriptList.Insert(LuaListView.SelectionStartIndex ?? LuaImp.ScriptList.Count, LuaFile.SeparatorInstance);
			UpdateDialog();
		}

		private void MoveUpMenuItem_Click(object sender, EventArgs e)
		{
			var indices = LuaListView.SelectedRows.ToList();
			if (indices.Count == 0 || indices[0] == 0)
			{
				return;
			}

			foreach (var index in indices)
			{
				var file = LuaImp.ScriptList[index];
				LuaImp.ScriptList.Remove(file);
				LuaImp.ScriptList.Insert(index - 1, file);
			}

			var newIndices = indices.Select(t => t - 1);

			LuaListView.DeselectAll();
			foreach (var i in newIndices)
			{
				LuaListView.SelectRow(i, true);
			}

			UpdateDialog();
		}

		private void MoveDownMenuItem_Click(object sender, EventArgs e)
		{
			var indices = LuaListView.SelectedRows.ToList();
			if (indices.Count == 0
				|| indices[indices.Count - 1] == LuaImp.ScriptList.Count - 1) // at end already
			{
				return;
			}

			for (var i = indices.Count - 1; i >= 0; i--)
			{
				var file = LuaImp.ScriptList[indices[i]];
				LuaImp.ScriptList.Remove(file);
				LuaImp.ScriptList.Insert(indices[i] + 1, file);
			}

			var newIndices = indices.Select(t => t + 1);

			LuaListView.DeselectAll();
			foreach (var i in newIndices)
			{
				LuaListView.SelectRow(i, true);
			}

			UpdateDialog();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
			=> LuaListView.ToggleSelectAll();

		private void StopAllScriptsMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var file in LuaImp.ScriptList)
			{
				DisableLuaScript(file);
			}
			UpdateDialog();
		}

		private void RegisteredFunctionsMenuItem_Click(object sender, EventArgs e)
		{
			if (LuaImp.RegisteredFunctions.Any())
			{
				var alreadyOpen = false;
				foreach (Form form in Application.OpenForms)
				{
					if (form is LuaRegisteredFunctionsList)
					{
						alreadyOpen = true;
						form.Activate();
					}
				}

				if (!alreadyOpen)
				{
					new LuaRegisteredFunctionsList((MainForm) MainForm, LuaImp.RegisteredFunctions)
					{
						StartLocation = this.ChildPointToScreen(LuaListView)
					}.Show();
				}
			}
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisableScriptsOnLoadMenuItem.Checked = Settings.DisableLuaScriptsOnLoad;
			ReturnAllIfNoneSelectedMenuItem.Checked = Settings.ToggleAllIfNoneSelected;
			ReloadWhenScriptFileChangesMenuItem.Checked = Settings.ReloadOnScriptFileChange;
		}

		private void DisableScriptsOnLoadMenuItem_Click(object sender, EventArgs e)
			=> Settings.DisableLuaScriptsOnLoad = !Settings.DisableLuaScriptsOnLoad;

		private void ToggleAllIfNoneSelectedMenuItem_Click(object sender, EventArgs e)
			=> Settings.ToggleAllIfNoneSelected = !Settings.ToggleAllIfNoneSelected;

		private void ReloadWhenScriptFileChangesMenuItem_Click(object sender, EventArgs e)
		{
			Settings.ReloadOnScriptFileChange = !Settings.ReloadOnScriptFileChange;
			if (Settings.ReloadOnScriptFileChange)
			{
				AddFileWatches();
			}
			else
			{
				ClearFileWatches();
			}
		}

		private void RegisterToTextEditorsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			// Hide until this one is implemented
			RegisterNotePadMenuItem.Visible = false;

			if (_luaAutoInstaller.IsInstalled(LuaAutocompleteInstaller.TextEditors.Sublime2))
			{
				if (_luaAutoInstaller.IsBizLuaRegistered(LuaAutocompleteInstaller.TextEditors.Sublime2))
				{
					RegisterSublimeText2MenuItem.Text = "Sublime Text 2 (installed)";
					RegisterSublimeText2MenuItem.SetStyle(FontStyle.Regular);
					RegisterSublimeText2MenuItem.Image = Resources.GreenCheck;
				}
				else
				{
					RegisterSublimeText2MenuItem.Text = "Sublime Text 2 (detected)";
					RegisterSublimeText2MenuItem.SetStyle(FontStyle.Italic);
					RegisterSublimeText2MenuItem.Image = null;
				}
			}
			else
			{
				RegisterSublimeText2MenuItem.Text = "Sublime Text 2";
				RegisterSublimeText2MenuItem.SetStyle(FontStyle.Regular);
				RegisterSublimeText2MenuItem.Image = null;
			}

			if (_luaAutoInstaller.IsInstalled(LuaAutocompleteInstaller.TextEditors.NotePad))
			{
				if (_luaAutoInstaller.IsBizLuaRegistered(LuaAutocompleteInstaller.TextEditors.NotePad))
				{
					RegisterNotePadMenuItem.Text = "Notepad++ (installed)";
					RegisterNotePadMenuItem.SetStyle(FontStyle.Regular);
					RegisterNotePadMenuItem.Image = Resources.GreenCheck;
				}
				else
				{
					RegisterNotePadMenuItem.Text = "Notepad++ (detected)";
					RegisterNotePadMenuItem.SetStyle(FontStyle.Italic);
					RegisterNotePadMenuItem.Image = null;
				}
			}
			else
			{
				RegisterNotePadMenuItem.Text = "Notepad++";
				RegisterNotePadMenuItem.SetStyle(FontStyle.Regular);
				RegisterNotePadMenuItem.Image = null;
			}
		}

		private void RegisterSublimeText2MenuItem_Click(object sender, EventArgs e)
		{
			_luaAutoInstaller.InstallBizLua(LuaAutocompleteInstaller.TextEditors.Sublime2, LuaImp.Docs);
		}

		private void RegisterNotePadMenuItem_Click(object sender, EventArgs e)
		{
			_luaAutoInstaller.InstallBizLua(LuaAutocompleteInstaller.TextEditors.NotePad, LuaImp.Docs);
		}

		private void FunctionsListMenuItem_Click(object sender, EventArgs e)
		{
			new LuaFunctionsForm(LuaImp.Docs).Show();
		}

		private void OnlineDocsMenuItem_Click(object sender, EventArgs e)
		{
			Process.Start("https://tasvideos.org/BizHawk/LuaFunctions");
		}

		private void ScriptListContextMenu_Opening(object sender, CancelEventArgs e)
		{
			ToggleScriptContextItem.Enabled =
				PauseScriptContextItem.Enabled =
				EditScriptContextItem.Enabled =
				SelectedFiles.Any();

			StopAllScriptsContextItem.Visible =
				ScriptContextSeparator.Visible =
				LuaImp.ScriptList.Exists(file => file.Enabled);

			ClearRegisteredFunctionsContextItem.Enabled =
				LuaImp.RegisteredFunctions.Any();
		}

		private void ConsoleContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RegisteredFunctionsContextItem.Enabled = LuaImp.RegisteredFunctions.Any();
			CopyContextItem.Enabled = OutputBox.SelectedText.Any();
			ClearConsoleContextItem.Enabled =
				SelectAllContextItem.Enabled =
				OutputBox.Text.Any();

			ClearRegisteredFunctionsLogContextItem.Enabled =
				LuaImp.RegisteredFunctions.Any();
		}

		private void ClearConsoleContextItem_Click(object sender, EventArgs e)
		{
			ClearOutputWindow();
		}

		private void SelectAllContextItem_Click(object sender, EventArgs e)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.SelectAll();
				OutputBox.Refresh();
			});
		}

		private void CopyContextItem_Click(object sender, EventArgs e)
		{
			if (!OutputBox.IsHandleCreated || OutputBox.IsDisposed)
			{
				return;
			}

			OutputBox.Invoke(() =>
			{
				OutputBox.Copy();
				OutputBox.Refresh();
			});
		}

		private void ClearRegisteredFunctionsContextMenuItem_Click(object sender, EventArgs e)
		{
			LuaImp.RegisteredFunctions.Clear(Emulator);
		}

		public bool LoadByFileExtension(string path, out bool abort)
		{
			var ext = Path.GetExtension(path)?.ToLowerInvariant();
			if (ext is ".luases")
			{
				LoadLuaSession(path);
				abort = true;
				return true;
			}
			abort = false;
			if (ext is ".lua" or ".txt")
			{
				LoadLuaFile(path);
				UpdateDialog();
				return true;
			}
			return false;
		}

		private void LuaConsole_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			try
			{
				foreach (var path in filePaths)
				{
					_ = LoadByFileExtension(path, out var abort);
					if (abort) return;
				}
			}
			catch (Exception ex)
			{
				DialogController.ShowMessageBox(ex.ToString());
			}
		}

		private void LuaListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.Delete))
			{
				RemoveScriptMenuItem_Click(null, null);
			}
			else if (e.IsCtrl(Keys.A))
			{
				SelectAllMenuItem_Click(null, null);
			}
			else if (e.IsPressed(Keys.F12))
			{
				RegisteredFunctionsMenuItem_Click(null, null);
			}
		}

		private void OutputBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.F12))
			{
				RegisteredFunctionsMenuItem_Click(null, null);
			}
		}

		/// <summary>
		/// Sorts the column Ascending on the first click and Descending on the second click.
		/// </summary>
		private void LuaListView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var columnToSort = e.Column!.Name;
			var luaListTemp = new List<LuaFile>();
			if (columnToSort != _lastColumnSorted)
			{
				_sortReverse = false;
			}

			// For getting the name of the .lua file, for some reason this field is kept blank in LuaFile.cs?
			// The Name variable gets emptied again near the end just in case it would break something.
			for (var i = 0; i < LuaImp.ScriptList.Count; i++)
			{
				var words = Regex.Split(LuaImp.ScriptList[i].Path, ".lua");
				var split = words[0].Split(Path.DirectorySeparatorChar);

				luaListTemp.Add(LuaImp.ScriptList[i]);
				luaListTemp[i].Name = split[split.Length - 1];
			}

			// Script, Path
			switch (columnToSort)
			{
				case "Script":
					luaListTemp = luaListTemp
						.OrderBy(lf => lf.Name, _sortReverse)
						.ThenBy(lf => lf.Path)
						.ToList();
					break;
				case "PathName":
					luaListTemp = luaListTemp
						.OrderBy(lf => lf.Path, _sortReverse)
						.ThenBy(lf => lf.Name)
						.ToList();
					break;
			}

			for (var i = 0; i < LuaImp.ScriptList.Count; i++)
			{
				LuaImp.ScriptList[i] = luaListTemp[i];
				LuaImp.ScriptList[i].Name = "";
			}

			UpdateDialog();
			_lastColumnSorted = columnToSort;
			_sortReverse = !_sortReverse;
		}

		private void RefreshScriptMenuItem_Click(object sender, EventArgs e)
		{
			var files = !SelectedFiles.Any() && Settings.ToggleAllIfNoneSelected
				? LuaImp.ScriptList
				: SelectedFiles;
			foreach (var file in files) RefreshLuaScript(file);
			UpdateDialog();
		}

		private void InputBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				var rawCommand = InputBox.Text;
				InputBox.Clear();
				InputBox.Refresh(); // if the command is something like `client.seekframe`, the Lua Console (and MainForm) will freeze until it finishes, so at least make it obvious that the Enter press was received
				// TODO: Maybe make these try-catches more general // what try-catches? LuaSandbox.Sandbox? --yoshi
				if (!string.IsNullOrWhiteSpace(rawCommand))
				{
					if (rawCommand.Contains("emu.frameadvance(")) //TODO this is pitiful; do it properly with a flag like the one we use for rom loads --yoshi
					{
						WriteLine("emu.frameadvance() can not be called from the console");
						return;
					}

					LuaSandbox.Sandbox(null, () =>
					{
						var prevMessageCount = _messageCount;
						var results = LuaImp.ExecuteString(rawCommand);
						// empty array if the command was e.g. a variable assignment or a loop without return statement
						// "void" functions return a single null
						// if output didn't change, Print will take care of writing out "(no return)"
						if (results is not ([ ] or [ null ]) || _messageCount == prevMessageCount)
						{
							LuaLibraries.Print(results);
						}
					});

					_messageCount = 0;
					_consoleCommandHistory.Insert(0, rawCommand);
					_consoleCommandHistoryIndex = -1;
				}
			}
			else if (e.KeyCode == Keys.Up)
			{
				if (_consoleCommandHistoryIndex < _consoleCommandHistory.Count - 1)
				{
					_consoleCommandHistoryIndex++;
					InputBox.Text = _consoleCommandHistory[_consoleCommandHistoryIndex];
					InputBox.Select(InputBox.Text.Length, 0);
				}

				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Down)
			{
				if (_consoleCommandHistoryIndex == 0)
				{
					_consoleCommandHistoryIndex--;
					InputBox.Text = "";
				}
				else if (_consoleCommandHistoryIndex > 0)
				{
					_consoleCommandHistoryIndex--;
					InputBox.Text = _consoleCommandHistory[_consoleCommandHistoryIndex];
					InputBox.Select(InputBox.Text.Length, 0);
				}

				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Tab)
			{
				ProcessTabKey(false);
				e.Handled = true;
			}
		}

		// For whatever reason an auto-complete TextBox doesn't respond to delete
		// Which is annoying but worse is that it let's the key propagate
		// If a script is highlighted in the ListView, and the user presses
		// delete, it will remove the script without this hack
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Delete && InputBox.Focused)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override bool ProcessTabKey(bool forward)
		{
			// TODO: Make me less dirty (please)
			return false;
		}

		private void EraseToolbarItem_Click(object sender, EventArgs e)
		{
			DisplayManager.ClearApiHawkSurfaces();
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			GenericDragEnter(sender, e);
		}

		private void LuaListView_DoubleClick(object sender, EventArgs e)
		{
			var index = LuaListView.CurrentCell?.RowIndex;
			if (index < LuaImp.ScriptList.Count)
			{
				var file = LuaImp.ScriptList[index.Value];
				ToggleLuaScript(file);
				UpdateDialog();
			}
		}

		private void ToggleLuaScript(LuaFile file)
		{
			if (file.IsSeparator)
			{
				return;
			}

			file.Toggle();
			_lastScriptUsed = file;
			if (file.Enabled && file.Thread is null)
			{
				LuaImp.RegisteredFunctions.RemoveForFile(file, Emulator); // First remove any existing registered functions for this file
				EnableLuaFile(file);
			}
			else if (!file.Enabled && file.Thread is not null)
			{
				DisableLuaScript(file);
				// there used to be a call here which did a redraw of the Gui/OSD, which included a call to `Tools.UpdateToolsAfter` --yoshi
			}

			LuaListView.Refresh();
		}

		private void DisableLuaScript(LuaFile file)
		{
			if (file.IsSeparator) return;

			file.State = LuaFile.RunState.Disabled;

			if (file.Thread is not null)
			{
				LuaImp.CallExitEvent(file);
				LuaImp.RegisteredFunctions.RemoveForFile(file, Emulator);
				file.Stop();
			}
		}

		private void RefreshLuaScript(LuaFile file)
		{
			ToggleLuaScript(file);
			ToggleLuaScript(file);
		}

		[RestoreDefaults]
		private void RestoreDefaults()
		{
			Settings = new LuaConsoleSettings();
			LuaListView.AllColumns.Clear();
			SetColumns();
			splitContainer1.SplitterDistance = _defaultSplitDistance;
			UpdateDialog();
		}
	}
}
