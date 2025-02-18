using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioMPR
	{
		private static readonly FilesystemFilterSet MoviesFSFilterSet = new(
			new FilesystemFilter("All Available Files", MovieService.MovieExtensions.Reverse().ToArray()),
			FilesystemFilter.TAStudioProjects,
			FilesystemFilter.BizHawkMovies);

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveBackupMenuItem.Enabled = SaveBk2BackupMenuItem.Enabled = !string.IsNullOrWhiteSpace(CurrentTasMovie.Filename) && CurrentTasMovie.Filename != DefaultTasProjName();
			saveSelectionToMacroToolStripMenuItem.Enabled =
				placeMacroAtSelectionToolStripMenuItem.Enabled =
				recentMacrosToolStripMenuItem.Enabled =
				TasView1.AnyRowsSelected;
		}

		private void NewFromSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			NewFromNowMenuItem.Enabled =
				CurrentTasMovie.InputLogLength > 0
				&& !CurrentTasMovie.StartsFromSaveRam;

			NewFromCurrentSaveRamMenuItem.Enabled =
				CurrentTasMovie.InputLogLength > 0
				&& SaveRamEmulator != null;
		}

		private void StartNewProjectFromNowMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSaveChanges())
			{
				var newProject = CurrentTasMovie.ConvertToSavestateAnchoredMovie(
					Emulator.Frame, StatableEmulator.CloneSavestate());

				MainForm.PauseEmulator();
				LoadMovie(newProject, true);
			}
		}

		private void StartANewProjectFromSaveRamMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSaveChanges())
			{
				var saveRam = SaveRamEmulator?.CloneSaveRam() ?? throw new Exception("No SaveRam");
				GoToFrame(TasView1.AnyRowsSelected ? TasView1.FirstSelectedRowIndex : 0);
				var newProject = CurrentTasMovie.ConvertToSaveRamAnchoredMovie(saveRam);
				MainForm.PauseEmulator();
				LoadMovie(newProject, true);
			}
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentSubMenu.ReplaceDropDownItems(Settings.RecentTas.RecentMenu(this, DummyLoadProject, "Project"));

		private void NewTasMenuItem_Click(object sender, EventArgs e) => StartNewTasMovie();

		private void OpenTasMenuItem_Click(object sender, EventArgs e)
		{
			if (!AskSaveChanges()) return;
			var filename = CurrentTasMovie.Filename;
			if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
			{
				filename = "";
			}
			var result = this.ShowFileOpenDialog(
				filter: MoviesFSFilterSet,
				initDir: Config!.PathEntries.MovieAbsolutePath(),
				initFileName: filename);
			if (result is not null) LoadMovieFile(result, askToSave: false);
		}

		/// <summary>
		/// Load the movie with the given filename within TAStudio.
		/// </summary>
		public bool LoadMovieFile(string filename, bool askToSave = true)
		{
			if (askToSave && !AskSaveChanges()) return false;
			if (filename.EndsWithOrdinal(MovieService.TasMovieExtension))
			{
				return LoadFileWithFallback(filename);
			}
			if (filename.EndsWithOrdinal(MovieService.StandardMovieExtension))
			{
				if (!DialogController.ShowMessageBox2(
					caption: "Convert movie",
					icon: EMsgBoxIcon.Question,
					text: "This is a regular movie, a new project must be created from it to use in TAStudio\nProceed?",
					useOKCancel: true))
				{
					return false;
				}

				return LoadFileWithFallback(filename);
			}
			DialogController.ShowMessageBox(
				caption: "Movie load error",
				icon: EMsgBoxIcon.Error,
				text: "This is not a BizHawk movie!");
			return false;
		}

		private void SaveTasMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(CurrentTasMovie.Filename) || CurrentTasMovie.Filename == DefaultTasProjName())
			{
				SaveAsTas();
			}
			else
			{
				SaveTas();
			}
			if (Settings.BackupPerFileSave)
			{
				SaveTas(saveBackup: true);
			}
		}

		private void SaveAsTasMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsTas();
			if (Settings.BackupPerFileSave)
			{
				SaveTas(saveBackup: true);
			}
		}

		private void SaveBackupMenuItem_Click(object sender, EventArgs e)
		{
			SaveTas(saveBackup: true);
		}

		private void SaveBk2BackupMenuItem_Click(object sender, EventArgs e)
		{
			SaveTas(saveAsBk2: true, saveBackup: true);
		}

		private void SaveSelectionToMacroMenuItem_Click(object sender, EventArgs e)
		{
			if (!TasView1.Focused && TasView1.AnyRowsSelected)
			{
				return;
			}

			if (TasView1.SelectionEndIndex == CurrentTasMovie.InputLogLength)
			{
				TasView1.SelectRow(CurrentTasMovie.InputLogLength, false);
			}

			var file = SaveFileDialog(
				null,
				MacroInputTool.SuggestedFolder(Config, Game),
				MacroInputTool.MacrosFSFilterSet,
				this
			);

			if (file != null)
			{
				var selectionStart = TasView1.SelectionStartIndex!.Value;
				new MovieZone(
					Emulator,
					Tools,
					MovieSession,
					start: selectionStart,
					length: TasView1.SelectionEndIndex!.Value - selectionStart + 1)
					.Save(file.FullName);

				Config.RecentMacros.Add(file.FullName);
			}
		}

		private void PlaceMacroAtSelectionMenuItem_Click(object sender, EventArgs e)
		{
			if (!CurrentTasView.Focused && CurrentTasView.AnyRowsSelected)
			{
				return;
			}

			var file = OpenFileDialog(
				null,
				MacroInputTool.SuggestedFolder(Config, Game),
				MacroInputTool.MacrosFSFilterSet
			);

			if (file != null)
			{
				DummyLoadMacro(file.FullName);
				Config.RecentMacros.Add(file.FullName);
			}
		}

		private void RecentMacrosMenuItem_DropDownOpened(object sender, EventArgs e)
			=> recentMacrosToolStripMenuItem.ReplaceDropDownItems(Config!.RecentMacros.RecentMenu(this, DummyLoadMacro, "Macro", noAutoload: true));

		private void ToBk2MenuItem_Click(object sender, EventArgs e)
		{
			_autosaveTimer.Stop();

			if (Emulator.HasCycleTiming() && !CurrentTasMovie.IsAtEnd())
			{
				DialogController.ShowMessageBox("This core requires emulation to be on the last frame when writing the movie, otherwise movie length will appear incorrect.", "Warning", EMsgBoxIcon.Warning);
			}

			string filename = CurrentTasMovie.Filename;
			if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
			{
				filename = SuggestedTasProjName();
			}

			var fileInfo = new FileInfo(Path.ChangeExtension(filename, Bk2Movie.Extension));
			if (fileInfo.Exists)
			{
				fileInfo = SaveFileDialog(currentFile: fileInfo.Name, path: Config!.PathEntries.MovieAbsolutePath(), new FilesystemFilterSet(FilesystemFilter.BizHawkMovies), this);
			}

			if (fileInfo is not null)
			{
				MessageStatusLabel.Text = "Exporting to .bk2...";
				MessageStatusLabel.Owner.Update();
				Cursor = Cursors.WaitCursor;
				var bk2 = CurrentTasMovie.ToBk2();
				bk2.Filename = fileInfo.FullName;
				bk2.Attach(Emulator); // required to be able to save the cycle count for ICycleTiming emulators
				bk2.Save();
				MessageStatusLabel.Text = $"{bk2.Name} exported.";
				Cursor = Cursors.Default;
			}
			else
			{
				MessageStatusLabel.Text = "bk2 export cancelled.";
			}

			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Start();
			}
		}

		private void EditSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DeselectMenuItem.Enabled =
				SelectBetweenMarkersMenuItem.Enabled =
				CopyMenuItem.Enabled =
				CutMenuItem.Enabled =
				ClearFramesMenuItem.Enabled =
				DeleteFramesMenuItem.Enabled =
				CloneFramesMenuItem.Enabled =
				CloneFramesXTimesMenuItem.Enabled =
				TruncateMenuItem.Enabled =
				InsertFrameMenuItem.Enabled =
				InsertNumFramesMenuItem.Enabled =
				TasView1.AnyRowsSelected;

			ReselectClipboardMenuItem.Enabled =
				PasteMenuItem.Enabled =
				PasteInsertMenuItem.Enabled = TasView1.AnyRowsSelected
				&& (Clipboard.GetDataObject()?.GetDataPresent(DataFormats.StringFormat) ?? false);

			ClearGreenzoneMenuItem.Enabled =
				CurrentTasMovie != null && CurrentTasMovie.TasStateManager.Count > 1;

			GreenzoneICheckSeparator.Visible =
				StateHistoryIntegrityCheckMenuItem.Visible =
				VersionInfo.DeveloperBuild;

			UndoMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Undo"];
			RedoMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Redo"];
			SelectBetweenMarkersMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Sel. bet. Markers"];
			SelectAllMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select All"];
			ReselectClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Reselect Clip."];
			ClearFramesMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Clear Frames"];
			DeleteFramesMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Delete Frames"];
			InsertFrameMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Insert Frame"];
			InsertNumFramesMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Insert # Frames"];
			CloneFramesMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Clone Frames"];
			CloneFramesXTimesMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Clone # Times"];
		}

		private void UndoMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentTasMovie.ChangeLog.Undo() < Emulator.Frame)
			{
				GoToFrame(CurrentTasMovie.ChangeLog.PreviousUndoFrame);
			}
			else
			{
				RefreshDialog();
			}

			// Currently I don't have a way to easily detect when CanUndo changes, so this button should be enabled always.
			// UndoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanUndo;
			RedoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanRedo;
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentTasMovie.ChangeLog.Redo() < Emulator.Frame)
			{
				GoToFrame(CurrentTasMovie.ChangeLog.PreviousRedoFrame);
			}
			else
			{
				RefreshDialog();
			}

			// Currently I don't have a way to easily detect when CanUndo changes, so this button should be enabled always.
			// UndoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanUndo;
			RedoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanRedo;
		}

		private void ShowUndoHistoryMenuItem_Click(object sender, EventArgs e)
		{
			_undoForm = new UndoHistoryFormMPR(this) { Owner = this };
			_undoForm.Show();
			_undoForm.UpdateValues();
		}

		private void DeselectMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasView.DeselectAll();
			CurrentTasView.Refresh();
		}

		/// <remarks>TODO merge w/ Deselect?</remarks>
		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.SelectAll();
				tasView.Refresh();
			}
		}

		private void SelectBetweenMarkersMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{

				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					var selectionEnd = tasView.SelectionEndIndex ?? 0;
					var prevMarker = CurrentTasMovie.Markers.PreviousOrCurrent(selectionEnd);
					var nextMarker = CurrentTasMovie.Markers.Next(selectionEnd);

					int prev = prevMarker?.Frame ?? 0;
					int next = nextMarker?.Frame ?? CurrentTasMovie.InputLogLength;

					tasView.DeselectAll();
					for (int i = prev; i < next; i++)
					{
						tasView.SelectRow(i, true);
					}

					SetSplicer();
					tasView.Refresh();
				}
			}
		}
		private void ReselectClipboardMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					tasView.DeselectAll();
					foreach (var item in _tasClipboard)
					{
						tasView.SelectRow(item.Frame, true);
					}

					SetSplicer();
					tasView.Refresh();
				}
			}
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				_tasClipboard.Clear();
				var list = CurrentTasView.SelectedRows.ToArray();
				var sb = new StringBuilder();

				foreach (var index in list)
				{
					var input = CurrentTasMovie.GetInputState(index);
					if (input == null)
					{
						break;
					}

					_tasClipboard.Add(new TasClipboardEntry(index, input));
					var logEntry = Bk2LogEntryGenerator.GenerateLogEntry(input);
					sb.AppendLine(Settings.CopyIncludesFrameNo ? $"{FrameToStringPadded(index)} {logEntry}" : logEntry);
				}

				Clipboard.SetDataObject(sb.ToString());
				SetSplicer();
			}
		}

		//private void PasteMenuItem_Click(object sender, EventArgs e)
		//{
		//	int tasViewIndex = TasViews.IndexOf(CurrentTasView);

		//	if (CurrentTasView.AnyRowsSelected)
		//	{
		//		// TODO: if highlighting 2 rows and pasting 3, only paste 2 of them
		//		// FCEUX Taseditor doesn't do this, but I think it is the expected behavior in editor programs

		//		// TODO: copy paste from PasteInsertMenuItem_Click!
		//		IDataObject data = Clipboard.GetDataObject();
		//		if (data != null && data.GetDataPresent(DataFormats.StringFormat))
		//		{
		//			string input = (string) data.GetData(DataFormats.StringFormat);
		//			if (!string.IsNullOrWhiteSpace(input))
		//			{
		//				string[] lines = input.Split('\n');
		//				if (lines.Length > 0)
		//				{
		//					_tasClipboard.Clear();
		//					int linesToPaste = lines.Length;
		//					if (lines[lines.Length - 1].Length is 0) linesToPaste--;
		//					for (int i = 0; i < linesToPaste; i++)
		//					{
		//						var line = ControllerFromMnemonicStr(lines[i]);
		//						if (line == null)
		//						{
		//							return;
		//						}

		//						_tasClipboard.Add(new TasClipboardEntry(i, line));
		//					}

		//					var rollbackFrame = CurrentTasMovie.CopyOverInput(CurrentTasView.SelectionStartIndex ?? 0, _tasClipboard.Select(static x => x.ControllerState));
		//					if (rollbackFrame > 0)
		//					{
		//						GoToLastEmulatedFrameIfNecessary(rollbackFrame);
		//						DoAutoRestore();
		//					}

		//					FullRefresh();
		//				}
		//			}
		//		}
		//	}
		//}
		private void PasteMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				// TODO: if highlighting 2 rows and pasting 3, only paste 2 of them
				// FCEUX Taseditor doesn't do this, but I think it is the expected behavior in editor programs

				// TODO: copy paste from PasteInsertMenuItem_Click!
				IDataObject data = Clipboard.GetDataObject();
				if (data != null && data.GetDataPresent(DataFormats.StringFormat))
				{
					string input = (string) data.GetData(DataFormats.StringFormat);
					if (!string.IsNullOrWhiteSpace(input))
					{
						string[] lines = input.Split('\n');
						if (lines.Length > 0)
						{
							_tasClipboard.Clear();
							int linesToPaste = lines.Length;
							if (lines[lines.Length - 1].Length is 0) linesToPaste--;
							for (int i = 0; i < linesToPaste; i++)
							{
								var line = ControllerFromMnemonicStr(lines[i]);
								if (line == null)
								{
									return;
								}

								_tasClipboard.Add(new TasClipboardEntry(i, line));
							}

							int startOffset = 1; //starts with "|" 
							for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
							{
								startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
								startOffset += 1; //add 1 for pipe
							}
							int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

							var framesToInsert = CurrentTasView.SelectedRows;
							var insertionFrame = Math.Min((CurrentTasView.SelectionEndIndex ?? 0) + 1, CurrentTasMovie.InputLogLength);
							var needsToRollback = CurrentTasView.SelectionStartIndex < Emulator.Frame;

							var inputLog = framesToInsert
								.Select(frame => CurrentTasMovie.GetInputLogEntry(frame))
								.ToList();

							var rollbackFrame = 0;
							//CurrentTasMovie.InsertInputMPR(insertionFrame, inputLog, startOffset, currentControlLength);
							rollbackFrame = CurrentTasMovie.CopyOverInputMPR(CurrentTasView.SelectionStartIndex ?? 0, _tasClipboard.Select(static x => x.ControllerState), startOffset, currentControlLength);


							if (rollbackFrame > 0)
							{
								GoToLastEmulatedFrameIfNecessary(rollbackFrame);
								DoAutoRestore();
							}

							FullRefresh();
						}
					}
				}
			}
		}

		private void PasteToDestMenuItem_Click(object sender, EventArgs e)
		{
			var tasViewSourceName = sender.ToString();
			InputRoll sourceTasView = TasViews.Find(t => t.Name == tasViewSourceName);
			var tasViewSourceIndex = TasViews.IndexOf(sourceTasView); //also used to identify controller controller			

			int tasViewDestIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected && sourceTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|" 
				for (int k = 0; k < tasViewDestIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewDestIndex].Count;

				var framesToInsert = sourceTasView.SelectedRows;
				var insertionFrame = Math.Min((CurrentTasView.SelectionEndIndex ?? 0) + 1, CurrentTasMovie.InputLogLength);
				var needsToRollback = CurrentTasView.SelectionStartIndex < Emulator.Frame;

				var inputLog = framesToInsert
					.Select(frame => CurrentTasMovie.GetInputLogEntry(frame))
					.ToList();

				var rollbackFrame = 0;

				int sourceStartOffset = 1; //starts with "|" 
				for (int k = 0; k < tasViewSourceIndex; k++) //add up inputs to get start string offset
				{
					sourceStartOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					sourceStartOffset += 1; //add 1 for pipe
				}
				int sourceCurrentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewDestIndex].Count;

				if (Emulator.ControllerDefinition.ControlsOrdered[tasViewSourceIndex].Count != Emulator.ControllerDefinition.ControlsOrdered[tasViewDestIndex].Count)
				{
					//("Cannot copy to another TasView where the controller definitions are not equal");
					return;
				}

				rollbackFrame = CurrentTasMovie.CopyOverDestInputMPR(
					CurrentTasView.SelectionStartIndex ?? 0,
					inputLog,
					startOffset,
					currentControlLength,
					sourceStartOffset);

				if (rollbackFrame > 0)
				{
					GoToLastEmulatedFrameIfNecessary(rollbackFrame);
					DoAutoRestore();
				}

				FullRefresh();
			}
		}

		private void PasteInsertMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					// copy paste from PasteMenuItem_Click!
					IDataObject data = Clipboard.GetDataObject();
					if (data != null && data.GetDataPresent(DataFormats.StringFormat))
					{
						string input = (string) data.GetData(DataFormats.StringFormat);
						if (!string.IsNullOrWhiteSpace(input))
						{
							string[] lines = input.Split('\n');
							if (lines.Length > 0)
							{
								_tasClipboard.Clear();
								int linesToPaste = lines.Length;
								if (lines[lines.Length - 1].Length is 0) linesToPaste--;
								for (int i = 0; i < linesToPaste; i++)
								{
									var line = ControllerFromMnemonicStr(lines[i]);
									if (line == null)
									{
										return;
									}

									_tasClipboard.Add(new TasClipboardEntry(i, line));
								}

								var selectionStart = TasView1.SelectionStartIndex;
								var needsToRollback = selectionStart < Emulator.Frame;
								CurrentTasMovie.InsertInput(selectionStart ?? 0, _tasClipboard.Select(static x => x.ControllerState));
								if (needsToRollback)
								{
									GoToLastEmulatedFrameIfNecessary(selectionStart!.Value);
									DoAutoRestore();
								}

								FullRefresh();
							}
						}
					}
				}
			}
		}

		//private void CutMenuItem_Click(object sender, EventArgs e)
		//{
		//	foreach (InputRoll tasView in TasViews)
		//	{
		//		if (tasView.Focused && tasView.AnyRowsSelected)
		//		{
		//			var selectionStart = TasView1.SelectionStartIndex;
		//			var needsToRollback = selectionStart < Emulator.Frame;
		//			var rollBackFrame = selectionStart ?? 0;

		//			_tasClipboard.Clear();
		//			var list = TasView1.SelectedRows.ToArray();
		//			var sb = new StringBuilder();

		//			foreach (var index in list) // copy of CopyMenuItem_Click()
		//			{
		//				var input = CurrentTasMovie.GetInputState(index);
		//				if (input == null)
		//				{
		//					break;
		//				}

		//				_tasClipboard.Add(new TasClipboardEntry(index, input));
		//				sb.AppendLine(Bk2LogEntryGenerator.GenerateLogEntry(input));
		//			}

		//			Clipboard.SetDataObject(sb.ToString());
		//			CurrentTasMovie.RemoveFrames(list);
		//			SetSplicer();

		//			if (needsToRollback)
		//			{
		//				GoToLastEmulatedFrameIfNecessary(rollBackFrame);
		//				DoAutoRestore();
		//			}

		//			FullRefresh();
		//		}
		//	}
		//}

		private void CutMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|"
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				var selectionStart = CurrentTasView.SelectionStartIndex;
				var needsToRollback = selectionStart < Emulator.Frame;
				var rollBackFrame = selectionStart ?? 0;

				_tasClipboard.Clear();
				var list = CurrentTasView.SelectedRows.ToArray();
				var sb = new StringBuilder();

				foreach (var index in list) // copy of CopyMenuItem_Click()
				{
					var input = CurrentTasMovie.GetInputState(index);
					if (input == null)
					{
						break;
					}

					_tasClipboard.Add(new TasClipboardEntry(index, input));
					sb.AppendLine(Bk2LogEntryGenerator.GenerateLogEntry(input));
				}

				Clipboard.SetDataObject(sb.ToString());
				CurrentTasMovie.RemoveFramesMPR(CurrentTasView.SelectedRows.ToArray(), startOffset, currentControlLength);

				//CurrentTasMovie.RemoveFrames(list);

				SetSplicer();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
					DoAutoRestore();
				}

				FullRefresh();
			}
		}



		private void ClearFramesMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|"
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				var firstWithInput = FirstNonEmptySelectedFrame;
				bool needsToRollback = firstWithInput.HasValue && firstWithInput < Emulator.Frame;
				var rollBackFrame = TasViews[tasViewIndex].SelectionStartIndex ?? 0;

				CurrentTasMovie.ChangeLog.BeginNewBatch($"Clear frames {TasViews[tasViewIndex].SelectionStartIndex}-{TasViews[tasViewIndex].SelectionEndIndex}");
				foreach (int frame in TasViews[tasViewIndex].SelectedRows)
				{
					CurrentTasMovie.ClearFrameMPR(frame, startOffset, currentControlLength);
				}

				CurrentTasMovie.ChangeLog.EndBatch();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
					DoAutoRestore();
				}

				FullRefresh();
			}
		}

		private void DeleteFramesMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|"
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				var selectionStart = CurrentTasView.SelectionStartIndex;
				var needsToRollback = selectionStart < Emulator.Frame;
				var rollBackFrame = selectionStart ?? 0;
				if (rollBackFrame >= CurrentTasMovie.InputLogLength)
				{
					// Cannot delete non-existent frames
					FullRefresh();
					return;
				}

				CurrentTasMovie.RemoveFramesMPR(CurrentTasView.SelectedRows.ToArray(), startOffset, currentControlLength);
				SetTasViewRowCount();
				SetSplicer();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
					DoAutoRestore();
				}

				FullRefresh();
			}
		}


		private void CloneFramesMenuItem_Click(object sender, EventArgs e)
		{
			CloneFramesXTimes(1);
		}

		private void CloneFramesXTimesMenuItem_Click(object sender, EventArgs e)
		{
			using var framesPrompt = new FramesPrompt("Clone # Times", "Insert times to clone:");
			if (framesPrompt.ShowDialogOnScreen().IsOk())
			{
				CloneFramesXTimes(framesPrompt.Frames);
			}
		}

		private void CloneFramesXTimes(int timesToClone)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|" 
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				for (int j = 0; j < timesToClone; j++)
				{
					var framesToInsert = CurrentTasView.SelectedRows;
					var insertionFrame = Math.Min((CurrentTasView.SelectionEndIndex ?? 0) + 1, CurrentTasMovie.InputLogLength);
					var needsToRollback = CurrentTasView.SelectionStartIndex < Emulator.Frame;

					var inputLog = framesToInsert
						.Select(frame => CurrentTasMovie.GetInputLogEntry(frame))
						.ToList();

					//A clone operation will insert the selected stuff for currently selected frames.
					//All the other inputs besides the current controls need to stay the same.
					//the current controller group need to shift down with the clone.

					CurrentTasMovie.InsertInputMPR(insertionFrame, inputLog, startOffset, currentControlLength);

					if (needsToRollback)
					{
						GoToLastEmulatedFrameIfNecessary(insertionFrame);
						DoAutoRestore();
					}

					FullRefresh();
				}
			}
		}

		private void InsertFrameMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|"
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				var selectionStart = CurrentTasView.SelectionStartIndex;
				var insertionFrame = selectionStart ?? 0;
				var needsToRollback = selectionStart < Emulator.Frame;

				CurrentTasMovie.InsertEmptyFrameMPR(insertionFrame, startOffset, currentControlLength);

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(insertionFrame);
					DoAutoRestore();
				}

				FullRefresh();

			}

		}
		private void InsertNumFramesMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|" 
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				var insertionFrame = CurrentTasView.SelectionStartIndex ?? 0;
				using var framesPrompt = new FramesPrompt();
				if (framesPrompt.ShowDialogOnScreen().IsOk())
				{
					InsertNumFramesMPR(insertionFrame, framesPrompt.Frames);
				}
			}
		}

		private void TruncateMenuItem_Click(object sender, EventArgs e)
		{
			int tasViewIndex = TasViews.IndexOf(CurrentTasView);

			if (CurrentTasView.AnyRowsSelected)
			{
				int startOffset = 1; //starts with "|"									 
				for (int k = 0; k < tasViewIndex; k++) //add up inputs to get start string offset
				{
					startOffset += Emulator.ControllerDefinition.ControlsOrdered[k].Count;
					startOffset += 1; //add 1 for pipe
				}
				int currentControlLength = Emulator.ControllerDefinition.ControlsOrdered[tasViewIndex].Count;

				var rollbackFrame = CurrentTasView.SelectionEndIndex ?? 0;
				var needsToRollback = CurrentTasView.SelectionStartIndex < Emulator.Frame;

				CurrentTasMovie.TruncateFramesMPR(rollbackFrame, startOffset, currentControlLength);
				MarkerControlMPR.MarkerInputRoll.TruncateSelection(CurrentTasMovie.Markers.Count - 1);

				if (needsToRollback)
				{
					GoToFrame(rollbackFrame);
				}

				FullRefresh();
			}
		}

		private void SetMarkersMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					var selectedRows = tasView.SelectedRows.ToList();
					if (selectedRows.Count > 50)
					{
						var result = DialogController.ShowMessageBox2("Are you sure you want to add more than 50 markers?", "Add markers", EMsgBoxIcon.Question, useOKCancel: true);
						if (!result)
						{
							return;
						}
					}

					foreach (var index in selectedRows)
					{
						MarkerControlMPR.AddMarker(index);
					}
				}
			}
		}
		private void SetMarkerWithTextMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					MarkerControlMPR.AddMarker(tasView.AnyRowsSelected ? tasView.FirstSelectedRowIndex : 0, true);
				}
			}
		}
		private void RemoveMarkersMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					CurrentTasMovie.Markers.RemoveAll(m => tasView.IsRowSelected(m.Frame));
					MarkerControlMPR.UpdateMarkerCount();
					RefreshDialog();
				}
			}
		}

		private void ClearGreenzoneMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasMovie.TasStateManager.Clear();
			RefreshDialog();
		}

		private void StateHistoryIntegrityCheckMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.DeterministicEmulation)
			{
				if (!DialogController.ShowMessageBox2("The emulator is not deterministic. It might fail even if the difference isn't enough to cause a desync.\nContinue with check?", "Not Deterministic"))
				{
					return;
				}
			}

			GoToFrame(0);
			int lastState = 0;
			int goToFrame = CurrentTasMovie.TasStateManager.Last;
			do
			{
				MainForm.FrameAdvance();

				byte[] greenZone = CurrentTasMovie.TasStateManager[Emulator.Frame];
				if (greenZone.Length > 0)
				{
					byte[] state = StatableEmulator.CloneSavestate();

					if (!state.SequenceEqual(greenZone))
					{
						if (DialogController.ShowMessageBox2($"Bad data between frames {lastState} and {Emulator.Frame}. Save the relevant state (raw data)?", "Integrity Failed!"))
						{
							var result = this.ShowFileSaveDialog(initDir: Config!.PathEntries.ToolsAbsolutePath(), initFileName: "integrity.fresh");
							if (result is not null)
							{
								File.WriteAllBytes(result, state);
								var path = Path.ChangeExtension(result, ".greenzoned");
								File.WriteAllBytes(path, greenZone);
							}
						}

						return;
					}

					lastState = Emulator.Frame;
				}
			}
			while (Emulator.Frame < goToFrame);

			DialogController.ShowMessageBox("Integrity Check passed");
		}

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutopauseAtEndOfMovieMenuItem.Checked = Settings.AutoPause;
			AutosaveAsBk2MenuItem.Checked = Settings.AutosaveAsBk2;
			AutosaveAsBackupFileMenuItem.Checked = Settings.AutosaveAsBackupFile;
			BackupPerFileSaveMenuItem.Checked = Settings.BackupPerFileSave;
			SingleClickAxisEditMenuItem.Checked = Settings.SingleClickAxisEdit;
			OldControlSchemeForBranchesMenuItem.Checked = Settings.OldControlSchemeForBranches;
			LoadBranchOnDoubleclickMenuItem.Checked = Settings.LoadBranchOnDoubleClick;
			BindMarkersToInputMenuItem.Checked = CurrentTasMovie.BindMarkersToInput;
			CopyIncludesFrameNoMenuItem.Checked = Settings.CopyIncludesFrameNo;
			AutoadjustInputMenuItem.Checked = Settings.AutoadjustInput;
		}

		private void SetMaxUndoLevelsMenuItem_Click(object sender, EventArgs e)
		{
			using var prompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Number of Undo Levels to keep",
				InitialValue = CurrentTasMovie.ChangeLog.MaxSteps.ToString()
			};

			var result = MainForm.DoWithTempMute(() => prompt.ShowDialogOnScreen());
			if (result.IsOk())
			{
				int val = 0;
				try
				{
					val = int.Parse(prompt.PromptText);
				}
				catch
				{
					DialogController.ShowMessageBox("Invalid Entry.", "Input Error", EMsgBoxIcon.Error);
				}

				if (val > 0)
				{
					Settings.MaxUndoSteps = CurrentTasMovie.ChangeLog.MaxSteps = val;
				}
			}
		}

		private void CopyIncludesFrameNoMenuItem_Click(object sender, EventArgs e)
			=> Settings.CopyIncludesFrameNo = !Settings.CopyIncludesFrameNo;

		private void AutoadjustInputMenuItem_Click(object sender, EventArgs e)
			=> Settings.AutoadjustInput = !Settings.AutoadjustInput;

		private void SetAutosaveIntervalMenuItem_Click(object sender, EventArgs e)
		{
			using var prompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Autosave Interval in seconds\nSet to 0 to disable",
				InitialValue = (Settings.AutosaveInterval / 1000).ToString()
			};

			var result = MainForm.DoWithTempMute(() => prompt.ShowDialogOnScreen());
			if (result.IsOk())
			{
				uint val = uint.Parse(prompt.PromptText) * 1000;
				Settings.AutosaveInterval = val;
				if (val > 0)
				{
					_autosaveTimer.Interval = (int) val;
					_autosaveTimer.Start();
				}
			}
		}

		private void AutosaveAsBk2MenuItem_Click(object sender, EventArgs e)
			=> Settings.AutosaveAsBk2 = !Settings.AutosaveAsBk2;

		private void AutosaveAsBackupFileMenuItem_Click(object sender, EventArgs e)
			=> Settings.AutosaveAsBackupFile = !Settings.AutosaveAsBackupFile;

		private void BackupPerFileSaveMenuItem_Click(object sender, EventArgs e)
			=> Settings.BackupPerFileSave = !Settings.BackupPerFileSave;

		private void ApplyPatternToPaintedInputMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			onlyOnAutoFireColumnsToolStripMenuItem.Enabled = applyPatternToPaintedInputToolStripMenuItem.Checked;
		}

		private void SingleClickAxisEditMenuItem_Click(object sender, EventArgs e)
			=> Settings.SingleClickAxisEdit = !Settings.SingleClickAxisEdit;

		private void BindMarkersToInputMenuItem_Click(object sender, EventArgs e)
		{
			Settings.BindMarkersToInput = CurrentTasMovie.BindMarkersToInput = BindMarkersToInputMenuItem.Checked;
		}

		private void AutoPauseAtEndMenuItem_Click(object sender, EventArgs e)
			=> Settings.AutoPause = !Settings.AutoPause;

		private void AutoHoldMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (autoHoldToolStripMenuItem.Checked)
			{
				autoFireToolStripMenuItem.Checked = false;
				customPatternToolStripMenuItem.Checked = false;

				if (!keepSetPatternsToolStripMenuItem.Checked)
				{
					UpdateAutoFire();
				}
			}
		}

		private void AutoFireMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (autoFireToolStripMenuItem.Checked)
			{
				autoHoldToolStripMenuItem.Checked = false;
				customPatternToolStripMenuItem.Checked = false;

				if (!keepSetPatternsToolStripMenuItem.Checked)
				{
					UpdateAutoFire();
				}
			}
		}

		private void CustomPatternMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (customPatternToolStripMenuItem.Checked)
			{
				autoHoldToolStripMenuItem.Checked = false;
				autoFireToolStripMenuItem.Checked = false;

				if (!keepSetPatternsToolStripMenuItem.Checked)
				{
					UpdateAutoFire();
				}
			}
		}

		private void SetCustomsMenuItem_Click(object sender, EventArgs e)
		{
			// Exceptions in PatternsForm are not caught by the debugger, I have no idea why.
			// Exceptions in UndoForm are caught, which makes it weirder.
			var pForm = new PatternsFormMPR(this) { Owner = this };
			pForm.Show();
		}

		private void OldControlSchemeForBranchesMenuItem_Click(object sender, EventArgs e)
			=> Settings.OldControlSchemeForBranches = !Settings.OldControlSchemeForBranches;

		private void LoadBranchOnDoubleClickMenuItem_Click(object sender, EventArgs e)
			=> Settings.LoadBranchOnDoubleClick = !Settings.LoadBranchOnDoubleClick;

		private void HeaderMenuItem_Click(object sender, EventArgs e)
		{
			using MovieHeaderEditor form = new(CurrentTasMovie, Config)
			{
				Owner = Owner,
				Location = this.ChildPointToScreen(TasView1)
			};
			form.ShowDialogOnScreen();
		}

		private void StateHistorySettingsMenuItem_Click(object sender, EventArgs e)
		{
			using GreenzoneSettings form = new(
				DialogController,
				new ZwinderStateManagerSettings(CurrentTasMovie.TasStateManager.Settings),
				(s, k) => { CurrentTasMovie.TasStateManager.UpdateSettings(s, k); },
				false)
			{
				Owner = Owner,
				Location = this.ChildPointToScreen(TasView1)
			};
			form.ShowDialogOnScreen();
		}

		private void CommentsMenuItem_Click(object sender, EventArgs e)
		{
			using EditCommentsForm form = new(CurrentTasMovie, false)
			{
				Owner = Owner,
				StartPosition = FormStartPosition.Manual,
				Location = this.ChildPointToScreen(TasView1)
			};
			form.ShowDialogOnScreen();
		}

		private void SubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			using EditSubtitlesForm form = new(
				DialogController,
				CurrentTasMovie,
				Config!.PathEntries,
				readOnly: false)
			{
				Owner = Owner,
				StartPosition = FormStartPosition.Manual,
				Location = this.ChildPointToScreen(TasView1)
			};
			form.ShowDialogOnScreen();
		}

		private void DefaultStateSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using GreenzoneSettings form = new(
				DialogController,
				new ZwinderStateManagerSettings(Config.Movies.DefaultTasStateManagerSettings),
				(s, k) => { Config.Movies.DefaultTasStateManagerSettings = s; },
				true)
			{
				Owner = Owner,
				Location = this.ChildPointToScreen(TasView1)
			};
			form.ShowDialogOnScreen();
		}

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RotateMenuItem.ShortcutKeyDisplayString = TasView1.RotateHotkeyStr;
		}

		private void HideLagFramesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			HideLagFrames0.Checked = TasView1.LagFramesToHide == 0;
			HideLagFrames1.Checked = TasView1.LagFramesToHide == 1;
			HideLagFrames2.Checked = TasView1.LagFramesToHide == 2;
			HideLagFrames3.Checked = TasView1.LagFramesToHide == 3;
			hideWasLagFramesToolStripMenuItem.Checked = TasView1.HideWasLagFrames;
		}

		private void IconsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			DenoteStatesWithIconsToolStripMenuItem.Checked = Settings.DenoteStatesWithIcons;
			DenoteStatesWithBGColorToolStripMenuItem.Checked = Settings.DenoteStatesWithBGColor;
			DenoteMarkersWithIconsToolStripMenuItem.Checked = Settings.DenoteMarkersWithIcons;
			DenoteMarkersWithBGColorToolStripMenuItem.Checked = Settings.DenoteMarkersWithBGColor;
		}

		private void FollowCursorMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				alwaysScrollToolStripMenuItem.Checked = Settings.FollowCursorAlwaysScroll;
				scrollToViewToolStripMenuItem.Checked = false;
				scrollToTopToolStripMenuItem.Checked = false;
				scrollToBottomToolStripMenuItem.Checked = false;
				scrollToCenterToolStripMenuItem.Checked = false;
				if (tasView.ScrollMethod == "near")
				{
					scrollToViewToolStripMenuItem.Checked = true;
				}
				else if (tasView.ScrollMethod == "top")
				{
					scrollToTopToolStripMenuItem.Checked = true;
				}
				else if (tasView.ScrollMethod == "bottom")
				{
					scrollToBottomToolStripMenuItem.Checked = true;
				}
				else
				{
					scrollToCenterToolStripMenuItem.Checked = true;
				}
			}
		}

		private void RotateMenuItem_Click(object sender, EventArgs e)
		{
			TasView1.HorizontalOrientation = !TasView1.HorizontalOrientation;
			CurrentTasMovie.FlagChanges();
		}

		private void HideLagFramesX_Click(object sender, EventArgs e)
		{
			TasView1.LagFramesToHide = (int) ((ToolStripMenuItem) sender).Tag;
			MaybeFollowCursor();
			RefreshDialog();
		}

		private void HideWasLagFramesMenuItem_Click(object sender, EventArgs e)
			=> TasView1.HideWasLagFrames = !TasView1.HideWasLagFrames;

		private void AlwaysScrollMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.AlwaysScroll = Settings.FollowCursorAlwaysScroll = alwaysScrollToolStripMenuItem.Checked;
			}
		}

		private void ScrollToViewMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.ScrollMethod = Settings.FollowCursorScrollMethod = "near";
			}
		}
		private void ScrollToTopMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.ScrollMethod = Settings.FollowCursorScrollMethod = "top";
			}
		}
		private void ScrollToBottomMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.ScrollMethod = Settings.FollowCursorScrollMethod = "bottom";
			}
		}
		private void ScrollToCenterMenuItem_Click(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				tasView.ScrollMethod = Settings.FollowCursorScrollMethod = "center";
			}
		}
		private void DenoteStatesWithIconsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteStatesWithIcons = DenoteStatesWithIconsToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void DenoteStatesWithBGColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteStatesWithBGColor = DenoteStatesWithBGColorToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void DenoteMarkersWithIconsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteMarkersWithIcons = DenoteMarkersWithIconsToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void DenoteMarkersWithBGColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteMarkersWithBGColor = DenoteMarkersWithBGColorToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void ColorSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using TAStudioColorSettingsFormMPR form = new(Palette, p => Settings.Palette = p)
			{
				Owner = Owner,
				StartPosition = FormStartPosition.Manual,
				Location = this.ChildPointToScreen(TasView1)
			};
			form.ShowDialogOnScreen();
		}

		private void WheelScrollSpeedMenuItem_Click(object sender, EventArgs e)
		{
			var inputPrompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Frames per tick:",
				InitialValue = TasView1.ScrollSpeed.ToString()
			};
			if (!this.ShowDialogWithTempMute(inputPrompt).IsOk()) return;
			foreach (InputRoll tasView in TasViews)
			{
				tasView.ScrollSpeed = int.Parse(inputPrompt.PromptText);
			}
			Settings.ScrollSpeed = TasView1.ScrollSpeed;
		}

		private void SetUpToolStripColumns()
		{
			ColumnsSubMenu.DropDownItems.Clear();

			var columns = TasView1.AllColumns
				.Where(static c => !string.IsNullOrWhiteSpace(c.Text) && c.Name is not "FrameColumn")
				.ToList();

			int workingHeight = Screen.FromControl(this).WorkingArea.Height;
			int rowHeight = ColumnsSubMenu.Height + 4;
			int maxRows = workingHeight / rowHeight;
			int keyCount = columns.Count(c => c.Name.StartsWithOrdinal("Key "));
			int keysMenusCount = (int) Math.Ceiling((double) keyCount / maxRows);

			var keysMenus = new ToolStripMenuItem[keysMenusCount];

			for (int i = 0; i < keysMenus.Length; i++)
			{
				keysMenus[i] = new ToolStripMenuItem();
			}

			var playerMenus = new ToolStripMenuItem[Emulator.ControllerDefinition.PlayerCount + 1];
			playerMenus[0] = ColumnsSubMenu;

			for (int i = 1; i < playerMenus.Length; i++)
			{
				playerMenus[i] = new ToolStripMenuItem($"Player {i}");
			}

			foreach (var column in columns)
			{
				var menuItem = new ToolStripMenuItem
				{
					Text = $"{column.Text} ({column.Name})",
					Checked = column.Visible,
					CheckOnClick = true,
					Tag = column.Name
				};

				menuItem.CheckedChanged += (o, ev) =>
				{
					ToolStripMenuItem sender = (ToolStripMenuItem) o;
					TasView1.AllColumns.Find(c => c.Name == (string) sender.Tag).Visible = sender.Checked;
					TasView1.AllColumns.ColumnsChanged();
					CurrentTasMovie.FlagChanges();
					TasView1.Refresh();
					ColumnsSubMenu.ShowDropDown();
					((ToolStripMenuItem) sender.OwnerItem).ShowDropDown();
				};

				if (column.Name.StartsWithOrdinal("Key "))
				{
					keysMenus
						.First(m => m.DropDownItems.Count < maxRows)
						.DropDownItems
						.Add(menuItem);
				}
				else
				{
					int player;

					if (column.Name.Length >= 2 && column.Name.StartsWith('P') && char.IsNumber(column.Name, 1))
					{
						player = int.Parse(column.Name[1].ToString());
					}
					else
					{
						player = 0;
					}

					playerMenus[player].DropDownItems.Add(menuItem);
				}
			}

			foreach (var menu in keysMenus)
			{
				string text = $"Keys ({menu.DropDownItems[0].Tag} - {menu.DropDownItems[menu.DropDownItems.Count - 1].Tag})";
				menu.Text = text.Replace("Key ", "");
				ColumnsSubMenu.DropDownItems.Add(menu);
			}

			for (int i = 1; i < playerMenus.Length; i++)
			{
				if (playerMenus[i].HasDropDownItems)
				{
					ColumnsSubMenu.DropDownItems.Add(playerMenus[i]);
				}
			}

			for (int i = 1; i < playerMenus.Length; i++)
			{
				if (playerMenus[i].HasDropDownItems)
				{
					ColumnsSubMenu.DropDownItems.Add(new ToolStripSeparator());
					break;
				}
			}

			if (keysMenus.Length > 0)
			{
				var item = new ToolStripMenuItem("Show Keys")
				{
					CheckOnClick = true,
					Checked = false
				};

				foreach (var menu in keysMenus)
				{
					var dummyObject1 = menu;
					item.CheckedChanged += (o, ev) =>
					{
						foreach (ToolStripMenuItem menuItem in dummyObject1.DropDownItems)
						{
							menuItem.Checked = !menuItem.Checked;
						}

						CurrentTasMovie.FlagChanges();
						TasView1.AllColumns.ColumnsChanged();
						TasView1.Refresh();
					};

					ColumnsSubMenu.DropDownItems.Add(item);
				}
			}

			for (int i = 1; i < playerMenus.Length; i++)
			{
				if (playerMenus[i].HasDropDownItems)
				{
					var item = new ToolStripMenuItem($"Show Player {i}")
					{
						CheckOnClick = true,
						Checked = playerMenus[i].DropDownItems.OfType<ToolStripMenuItem>().Any(mi => mi.Checked)
					};

					ToolStripMenuItem dummyObject = playerMenus[i];
					item.CheckedChanged += (o, ev) =>
					{
						// TODO: preserve underlying button checked state and make this a master visibility control
						foreach (ToolStripMenuItem menuItem in dummyObject.DropDownItems)
						{
							menuItem.Checked = item.Checked;
						}
						dummyObject.Visible = item.Checked;

						CurrentTasMovie.FlagChanges();
						TasView1.AllColumns.ColumnsChanged();
						TasView1.Refresh();
					};

					ColumnsSubMenu.DropDownItems.Add(item);
				}
			}
		}

		// ReSharper disable once UnusedMember.Local
		[RestoreDefaults]
		private void RestoreDefaults() //changed top three function order. test for bugs
		{
			SetUpToolStripColumns();
			for (int i = 0; i < TasViews.Count; i++)
			{
				SetUpColumnsPerControl(i);
				TasViews[i].Refresh();
			}
			CurrentTasMovie.FlagChanges();
			MainVertialSplit.SplitterDistance = _defaultMainSplitDistance;
			BranchesMarkersSplit.SplitterDistance = _defaultBranchMarkerSplitDistance;
		}

		private void RightClickMenu_Opened(object sender, EventArgs e)
		{
			foreach (InputRoll tasView in TasViews)
			{
				if (tasView.Focused && tasView.AnyRowsSelected)
				{
					SetMarkersContextMenuItem.Enabled =
					SelectBetweenMarkersContextMenuItem.Enabled =
					RemoveMarkersContextMenuItem.Enabled =
					DeselectContextMenuItem.Enabled =
					ClearContextMenuItem.Enabled =
					DeleteFramesContextMenuItem.Enabled =
					CloneContextMenuItem.Enabled =
					CloneXTimesContextMenuItem.Enabled =
					InsertFrameContextMenuItem.Enabled =
					InsertNumFramesContextMenuItem.Enabled =
					TruncateContextMenuItem.Enabled =
					tasView.AnyRowsSelected;

					pasteToolStripMenuItem.Enabled =
						pasteInsertToolStripMenuItem.Enabled =
						(Clipboard.GetDataObject()?.GetDataPresent(DataFormats.StringFormat) ?? false)
						&& tasView.AnyRowsSelected;

					var selectionIsSingleRow = tasView.SelectedRows.CountIsExactly(1);
					StartNewProjectFromNowMenuItem.Visible =
						selectionIsSingleRow
						&& tasView.IsRowSelected(Emulator.Frame)
						&& !CurrentTasMovie.StartsFromSaveRam;

					StartANewProjectFromSaveRamMenuItem.Visible =
						selectionIsSingleRow
						&& SaveRamEmulator != null
						&& !CurrentTasMovie.StartsFromSavestate;

					StartFromNowSeparator.Visible = StartNewProjectFromNowMenuItem.Visible || StartANewProjectFromSaveRamMenuItem.Visible;
					RemoveMarkersContextMenuItem.Enabled = CurrentTasMovie.Markers.Any(m => tasView.IsRowSelected(m.Frame)); // Disable the option to remove markers if no markers are selected (FCEUX does this).
					CancelSeekContextMenuItem.Enabled = MainForm.PauseOnFrame.HasValue;
					BranchContextMenuItem.Visible = tasView.CurrentCell?.RowIndex == Emulator.Frame;

					SelectBetweenMarkersContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Sel. bet. Markers"];
					ClearContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Clear Frames"];
					DeleteFramesContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Delete Frames"];
					InsertFrameContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Insert Frame"];
					InsertNumFramesContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Insert # Frames"];
					CloneContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Clone Frames"];
					CloneXTimesContextMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Clone # Times"];
				}
			}
		}

		private void CancelSeekContextMenuItem_Click(object sender, EventArgs e)
		{
			MainForm.PauseOnFrame = null;
			foreach (InputRoll tasView in TasViews)
			{
				tasView.Refresh();
			}
		}

		private void BranchContextMenuItem_Click(object sender, EventArgs e)
		{
			BookMarkControlMPR.Branch();
		}

		private void TASEditorManualOnlineMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.fceux.com/web/help/taseditor/");
		}

		private void ForumThreadMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://tasvideos.org/Forum/Topics/13505");
		}
	}
}
