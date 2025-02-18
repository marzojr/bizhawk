using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioMPR
	{
		// Input Painting
		private string _startBoolDrawColumn = "";
		private string _startAxisDrawColumn = "";
		private bool _drewAxis;
		private bool _boolPaintState;
		private int _axisPaintState;
		private int _axisBackupState;
		private bool _patternPaint;
		private bool _startCursorDrag;
		private bool _startSelectionDrag;
		private bool _selectionDragState;
		private bool _suppressContextMenu;
		private int _startRow;
		private int _paintingMinFrame = -1;
		private bool _playbackInterrupted; // Occurs when the emulator is unpaused and the user click and holds mouse down to begin delivering input

		// Editing analog input
		private string _axisEditColumn = "";
		private int _axisEditRow = -1;
		private string _axisTypedValue;
		private int _axisEditYPos = -1;
		private int AxisEditRow
		{
			set
			{
				_axisEditRow = value;
				foreach (InputRoll tasView in TasViews)
				{
					if (tasView.Focused && tasView.AnyRowsSelected)
					{
						tasView.SuspendHotkeys = AxisEditingMode;
					}
				}
			}
		}

		public bool AxisEditingMode => _axisEditRow != -1;

		private readonly List<int> _extraAxisRows = new List<int>();

		// Right-click dragging
		private string[] _rightClickInput;
		private string[] _rightClickOverInput;
		private int _rightClickFrame = -1;
		private int _rightClickLastFrame = -1;
		private bool _rightClickShift, _rightClickControl, _rightClickAlt;
		private bool _leftButtonHeld;

		private bool MouseButtonHeld => _rightClickFrame != -1 || _leftButtonHeld;

		private bool _triggerAutoRestore; // If true, autorestore will be called on mouse up
		private bool? _autoRestorePaused;
		private int? _seekStartFrame;
		private bool _unpauseAfterSeeking;

		private readonly Dictionary<string, bool> _alternateRowColor = new();

		private ControllerDefinition ControllerType => MovieSession.MovieController.Definition;

		public bool WasRecording { get; set; }
		public AutoPatternBool[] BoolPatterns;
		public AutoPatternAxis[] AxisPatterns;

		public void JumpToGreenzone(bool OnLeftMouseDown = false)
		{
			if (Emulator.Frame > CurrentTasMovie.LastEditedFrame)
			{
				GoToLastEmulatedFrameIfNecessary(CurrentTasMovie.LastEditedFrame, OnLeftMouseDown);
			}
		}

		private void StartSeeking(int? frame, bool fromMiddleClick = false)
		{
			if (!frame.HasValue)
			{
				return;
			}

			if (!fromMiddleClick)
			{
				if (MainForm.PauseOnFrame != null)
				{
					StopSeeking(true); // don't restore rec mode just yet, as with heavy editing checkbox updating causes lag
				}
				_seekStartFrame = Emulator.Frame;
			}

			MainForm.PauseOnFrame = frame.Value;
			int? diff = MainForm.PauseOnFrame - _seekStartFrame;

			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;
			TastudioPlayMode(); // suspend rec mode until seek ends, to allow mouse editing
			MainForm.UnpauseEmulator();

			if (diff > TasView1.VisibleRows)
			{
				MessageStatusLabel.Text = "Seeking...";
				ProgressBar.Visible = true;
			}
		}

		public void StopSeeking(bool skipRecModeCheck = false)
		{
			if (WasRecording && !skipRecModeCheck)
			{
				TastudioRecordMode();
				WasRecording = false;
			}

			MainForm.PauseOnFrame = null;
			if (_unpauseAfterSeeking)
			{
				MainForm.UnpauseEmulator();
				_unpauseAfterSeeking = false;
			}

			if (CurrentTasMovie != null)
			{
				RefreshDialog();
				UpdateProgressBar();
			}
		}

		private Bitmap ts_v_arrow_green_blue => Properties.Resources.ts_v_arrow_green_blue;
		private Bitmap ts_h_arrow_green_blue => Properties.Resources.ts_h_arrow_green_blue;
		private Bitmap ts_v_arrow_blue => Properties.Resources.ts_v_arrow_blue;
		private Bitmap ts_h_arrow_blue => Properties.Resources.ts_h_arrow_blue;
		private Bitmap ts_v_arrow_green => Properties.Resources.ts_v_arrow_green;
		private Bitmap ts_h_arrow_green => Properties.Resources.ts_h_arrow_green;

		private Bitmap icon_marker => Properties.Resources.icon_marker;
		private Bitmap icon_anchor_lag => Properties.Resources.icon_anchor_lag;
		private Bitmap icon_anchor => Properties.Resources.icon_anchor;

		private void TasView_QueryItemIcon(int index, RollColumn column, ref Bitmap bitmap, ref int offsetX, ref int offsetY)
		{
			if (!_engaged || _initializing)
			{
				return;
			}

			var overrideIcon = QueryItemIconCallback?.Invoke(index, column.Name);

			if (overrideIcon != null)
			{
				bitmap = overrideIcon;
				return;
			}

			var columnName = column.Name;

			if (columnName == CursorColumnName)
			{
				if (TasView1.HorizontalOrientation)
				{
					offsetX = -1;
					offsetY = 5;
				}

				if (index == Emulator.Frame)
				{
					bitmap = index == MainForm.PauseOnFrame
						? TasView1.HorizontalOrientation ? ts_v_arrow_green_blue : ts_h_arrow_green_blue
						: TasView1.HorizontalOrientation ? ts_v_arrow_blue : ts_h_arrow_blue;
				}
				else if (index == LastPositionFrame)
				{
					bitmap = TasView1.HorizontalOrientation ?
						ts_v_arrow_green :
						ts_h_arrow_green;
				}
			}
			else if (columnName == FrameColumnName)
			{
				offsetX = -3;
				offsetY = 1;

				if (Settings.DenoteMarkersWithIcons && CurrentTasMovie.Markers.IsMarker(index))
				{
					bitmap = icon_marker;
				}
				else if (Settings.DenoteStatesWithIcons)
				{
					var record = CurrentTasMovie[index];
					if (record.HasState) bitmap = record.Lagged is true ? icon_anchor_lag : icon_anchor;
				}
			}
		}

		private void TasView_QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			if (!_engaged || _initializing)
			{
				return;
			}

			Color? overrideColor = QueryItemBgColorCallback?.Invoke(index, column.Name);

			if (overrideColor.HasValue)
			{
				color = overrideColor.Value;
				return;
			}

			string columnName = column.Name;

			if (columnName == CursorColumnName)
			{
				color = Color.FromArgb(0xFE, 0xFF, 0xFF);
			}

			if (columnName == FrameColumnName)
			{
				if (Emulator.Frame != index && Settings.DenoteMarkersWithBGColor && CurrentTasMovie.Markers.IsMarker(index))
				{
					color = Palette.Marker_FrameCol;
				}
				else
				{
					color = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF);
				}
			}
			else if (AxisEditingMode
				&& (index == _axisEditRow || _extraAxisRows.Contains(index))
				&& columnName == _axisEditColumn)
			{
				color = Palette.AnalogEdit_Col;
			}

			if (_alternateRowColor.GetValueOrPut(
				columnName,
				columnName1 =>
				{
					var playerNumber = ControllerDefinition.PlayerNumber(columnName1);
					return playerNumber % 2 is 0 && playerNumber is not 0;
				}))
			{
				color = Color.FromArgb(0x0D, 0x00, 0x00, 0x00);
			}
		}

		private void TasView_QueryRowBkColor(int index, ref Color color)
		{
			if (!_engaged || _initializing)
			{
				return;
			}

			var record = CurrentTasMovie[index];

			if (MainForm.IsSeeking && MainForm.PauseOnFrame == index)
			{
				color = Palette.CurrentFrame_InputLog;
			}
			else if (!MainForm.IsSeeking && Emulator.Frame == index)
			{
				color = Palette.CurrentFrame_InputLog;
			}
			else if (record.Lagged.HasValue)
			{
				if (!record.HasState && Settings.DenoteStatesWithBGColor)
				{
					color = record.Lagged.Value
						? Palette.LagZone_InputLog
						: Palette.GreenZone_InputLog;
				}
				else
				{
					color = record.Lagged.Value
						? Palette.LagZone_InputLog_Stated
						: Palette.GreenZone_InputLog_Stated;
				}
			}
			else if (record.WasLagged.HasValue)
			{
				color = record.WasLagged.Value
					? Palette.LagZone_InputLog_Invalidated
					: Palette.GreenZone_InputLog_Invalidated;
			}
			else
			{
				color = Color.FromArgb(0xFF, 0xFE, 0xEE);
			}
		}

		private readonly string[] _formatCache = Enumerable.Range(1, 10).Select(i => $"D{i}").ToArray();

		/// <returns><paramref name="index"/> with leading zeroes such that every frame in the movie will be printed with the same number of digits</returns>
		private string FrameToStringPadded(int index)
			=> index.ToString(_formatCache[Math.Max(
				4,
				NumberExtensions.Log10(Math.Max(CurrentTasMovie.InputLogLength, 1)))]);

		private void TasView_QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			if (!_engaged || _initializing)
			{
				text = "";
				return;
			}

			var overrideText = QueryItemTextCallback?.Invoke(index, column.Name);
			if (overrideText != null)
			{
				text = overrideText;
				return;
			}

			try
			{
				text = "";
				var columnName = column.Name;

				if (columnName == CursorColumnName)
				{
					int branchIndex = CurrentTasMovie.Branches.IndexOfFrame(index);
					if (branchIndex != -1)
					{
						text = (branchIndex + 1).ToString();
					}
				}
				else if (columnName == FrameColumnName)
				{
					offsetX = TasView1.HorizontalOrientation ? 2 : 7;
					text = FrameToStringPadded(index);
				}
				else
				{
					// Display typed float value (string "-" can't be parsed, so CurrentTasMovie.DisplayValue can't return it)
					if (index == _axisEditRow && columnName == _axisEditColumn)
					{
						text = _axisTypedValue;
					}
					else if (index < CurrentTasMovie.InputLogLength)
					{
						text = CurrentTasMovie.DisplayValue(index, columnName);
						if (column.Type == ColumnType.Axis)
						{
							// feos: this could be cached, but I don't notice any slowdown this way either
							if (text == ((float) ControllerType.Axes[columnName].Neutral).ToString(NumberFormatInfo.InvariantInfo))
							{
								text = "";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				text = "";
				DialogController.ShowMessageBox($"oops\n{ex}");
			}
		}

		private bool TasView_QueryFrameLag(int index, bool hideWasLag)
		{
			var lag = CurrentTasMovie[index];
			return (lag.Lagged.HasValue && lag.Lagged.Value) || (hideWasLag && lag.WasLagged.HasValue && lag.WasLagged.Value);
		}

		private void TasView_ColumnClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var tasView = sender as InputRoll;

			if (tasView.AnyRowsSelected)
			{
				var columnName = e.Column!.Name;

				if (columnName == FrameColumnName)
				{
					CurrentTasMovie.Markers.Add(tasView.SelectionEndIndex!.Value, "");
				}
				else if (columnName != CursorColumnName)
				{
					var frame = tasView.AnyRowsSelected ? tasView.FirstSelectedRowIndex : 0;
					var buttonName = tasView.CurrentCell.Column!.Name;

					if (ControllerType.BoolButtons.Contains(buttonName))
					{
						if (ModifierKeys != Keys.Alt)
						{
							// nifty taseditor logic
							bool allPressed = true;
							foreach (var index in tasView.SelectedRows)
							{
								if (index == CurrentTasMovie.FrameCount // last movie frame can't have input, but can be selected
									|| !CurrentTasMovie.BoolIsPressed(index, buttonName))
								{
									allPressed = false;
									break;
								}
							}
							CurrentTasMovie.SetBoolStates(frame, tasView.SelectedRows.Count(), buttonName, !allPressed);
						}
						else
						{
							BoolPatterns[ControllerType.BoolButtons.IndexOf(buttonName)].Reset();
							foreach (var index in tasView.SelectedRows)
							{
								CurrentTasMovie.SetBoolState(index, buttonName, BoolPatterns[ControllerType.BoolButtons.IndexOf(buttonName)].GetNextValue());
							}
						}
					}
					else
					{
						// feos: there's no default value other than neutral, and we can't go arbitrary here, so do nothing for now
						// autohold is ignored for axes too for the same reasons: lack of demand + ambiguity
					}

					_triggerAutoRestore = true;
					TastudioPlayMode(true);
					JumpToGreenzone();
				}

				RefreshDialog();
			}
		}

		private void TasView_ColumnRightClick(object sender, InputRoll.ColumnClickEventArgs e)
		{
			var tasView = sender as InputRoll;
			var col = e.Column!;
			if (col.Name is FrameColumnName or CursorColumnName) return;

			col.Emphasis = !col.Emphasis;
			UpdateAutoFire(col.Name, col.Emphasis);
			tasView.Refresh();
		}

		private void UpdateAutoFire()
		{
			for (int i = 2; i < TasView1.AllColumns.Count; i++)
			{
				UpdateAutoFire(TasView1.AllColumns[i].Name, TasView1.AllColumns[i].Emphasis);
			}
		}

		public void UpdateAutoFire(string button, bool? isOn)
		{
			// No value means don't change whether it's on or off.
			isOn ??= TasView1.AllColumns.Find(c => c.Name == button).Emphasis;

			// use custom pattern if set
			bool useCustom = customPatternToolStripMenuItem.Checked;
			// else, set autohold or fire based on setting
			bool autoHold = autoHoldToolStripMenuItem.Checked; // !autoFireToolStripMenuItem.Checked

			if (ControllerType.BoolButtons.Contains(button))
			{
				InputManager.StickyHoldController.SetButtonHold(button, false);
				InputManager.StickyAutofireController.SetButtonAutofire(button, false);
				if (!isOn.Value) return;

				if (useCustom)
				{
					InputManager.StickyAutofireController.SetButtonAutofire(button, true, BoolPatterns[ControllerType.BoolButtons.IndexOf(button)]);
				}
				else if (autoHold)
				{
					InputManager.StickyHoldController.SetButtonHold(button, true);
				}
				else
				{
					InputManager.StickyAutofireController.SetButtonAutofire(button, true);
				}
			}
			else
			{
				InputManager.StickyHoldController.SetAxisHold(button, null);
				InputManager.StickyAutofireController.SetAxisAutofire(button, null);
				if (!isOn.Value) return;

				int holdValue = ControllerType.Axes[button].Range.EndInclusive; // it's not clear what value to use for auto-hold, just use max i guess
				if (useCustom)
				{
					InputManager.StickyAutofireController.SetAxisAutofire(button, holdValue, AxisPatterns[ControllerType.Axes.IndexOf(button)]);
				}
				else if (autoHold)
				{
					InputManager.StickyHoldController.SetAxisHold(button, holdValue);
				}
				else
				{
					InputManager.StickyAutofireController.SetAxisAutofire(button, holdValue);
				}
			}
		}

		private void TasView_ColumnReordered(object sender, InputRoll.ColumnReorderedEventArgs e)
		{
			CurrentTasMovie.FlagChanges();
		}

		private void TasView_MouseEnter(object sender, EventArgs e)
		{
			if (ContainsFocus)
			{
				var tasView = sender as InputRoll;
				tasView.Select();
			}
		}

		private void TasView_MouseDown(object sender, MouseEventArgs e)
		{
			var tasView = sender as InputRoll;

			// Clicking with left while right is held or vice versa does weird stuff
			if (MouseButtonHeld)
			{
				return;
			}

			// only on mouse button down, check that the pointed to cell is the correct one (can be wrong due to scroll while playing)
			tasView._programmaticallyChangingRow = true;
			tasView.PointMouseToNewCell();

			if (e.Button == MouseButtons.Middle)
			{
				if (MainForm.EmulatorPaused)
				{
					var record = CurrentTasMovie[LastPositionFrame];
					if (!record.Lagged.HasValue && LastPositionFrame > Emulator.Frame)
					{
						StartSeeking(LastPositionFrame, true);
						return;
					}
				}

				MainForm.TogglePause();
				return;
			}

			if (tasView.CurrentCell is not { RowIndex: int frame, Column: RollColumn targetCol }) return;

			var buttonName = targetCol.Name;
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;

			if (e.Button == MouseButtons.Left)
			{
				_leftButtonHeld = true;
				_paintingMinFrame = frame;

				// SuuperW: Exit axis editing mode, or re-enter mouse editing
				if (AxisEditingMode)
				{
					if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
					{
						_extraAxisRows.Clear();
						_extraAxisRows.AddRange(tasView.SelectedRows);
						_startSelectionDrag = true;
						_selectionDragState = tasView.IsRowSelected(frame);
						return;
					}
					if (_axisEditColumn != buttonName
						|| !(_axisEditRow == frame || _extraAxisRows.Contains(frame)))
					{
						_extraAxisRows.Clear();
						AxisEditRow = -1;
						SetTasViewRowCount();
					}
					else
					{
						if (_extraAxisRows.Contains(frame))
						{
							_extraAxisRows.Clear();
							AxisEditRow = frame;
							SetTasViewRowCount();
						}

						_axisEditYPos = e.Y;
						_axisPaintState = CurrentTasMovie.GetAxisState(frame, buttonName);

						_triggerAutoRestore = true;
						TastudioPlayMode(true);
						return;
					}
				}

				if (targetCol.Name is CursorColumnName)
				{
					_startCursorDrag = true;
					GoToFrame(frame, fromLua: false, fromRewinding: false, OnLeftMouseDown: true);
				}
				else if (targetCol.Name is FrameColumnName)
				{
					if (ModifierKeys == Keys.Alt && CurrentTasMovie.Markers.IsMarker(frame))
					{
						// TODO
						tasView.DragCurrentCell();
					}
					else
					{
						_startSelectionDrag = true;
						_selectionDragState = tasView.IsRowSelected(frame);
					}
				}
				else if (targetCol.Type is not ColumnType.Text) // User changed input
				{
					_playbackInterrupted = !MainForm.EmulatorPaused;
					MainForm.PauseEmulator();

					// Pausing the emulator is insufficient to actually stop frame advancing as the frame advance hotkey can
					// still take effect. This can lead to desyncs by simultaneously changing input and frame advancing.
					// So we want to block all frame advance operations while the user is changing input in the piano roll
					MainForm.BlockFrameAdvance = true;

					if (ControllerType.BoolButtons.Contains(buttonName))
					{
						_patternPaint = false;
						_startBoolDrawColumn = buttonName;

						var altOrShift4State = ModifierKeys & (Keys.Alt | Keys.Shift);
						if (altOrShift4State is Keys.Alt
							|| (applyPatternToPaintedInputToolStripMenuItem.Checked
								&& (!onlyOnAutoFireColumnsToolStripMenuItem.Checked || targetCol.Emphasis)))
						{
							BoolPatterns[ControllerType.BoolButtons.IndexOf(buttonName)].Reset();
							_patternPaint = true;
							_startRow = frame;
							_boolPaintState = !CurrentTasMovie.BoolIsPressed(frame, buttonName);
						}
						else if (altOrShift4State is Keys.Shift)
						{
							if (!tasView.AnyRowsSelected) return;

							var iFirstSelectedRow = tasView.FirstSelectedRowIndex;
							var (firstSel, lastSel) = frame <= iFirstSelectedRow
								? (frame, iFirstSelectedRow)
								: (iFirstSelectedRow, frame);

							bool allPressed = true;
							for (var i = firstSel; i <= lastSel; i++)
							{
								if (i == CurrentTasMovie.FrameCount // last movie frame can't have input, but can be selected
									|| !CurrentTasMovie.BoolIsPressed(i, buttonName))
								{
									allPressed = false;
									break;
								}
							}
							CurrentTasMovie.SetBoolStates(firstSel, lastSel - firstSel + 1, buttonName, !allPressed);
							_boolPaintState = CurrentTasMovie.BoolIsPressed(lastSel, buttonName);
							_triggerAutoRestore = true;
							TastudioPlayMode(true);
							RefreshDialog();
						}
#if false // to match previous behaviour
						else if (altOrShift4State is not 0)
						{
							// TODO: Pattern drawing from selection to current cell
						}
#endif
						else
						{
							CurrentTasMovie.ChangeLog.BeginNewBatch($"Paint Bool {buttonName} from frame {frame}");

							CurrentTasMovie.ToggleBoolState(frame, buttonName);
							_boolPaintState = CurrentTasMovie.BoolIsPressed(frame, buttonName);
							_triggerAutoRestore = true;
							TastudioPlayMode(true);
							RefreshDialog();
						}
					}
					else
					{
						if (frame >= CurrentTasMovie.InputLogLength)
						{
							CurrentTasMovie.SetAxisState(frame, buttonName, ControllerType.Axes[buttonName].Neutral);
							RefreshDialog();
						}

						_axisPaintState = CurrentTasMovie.GetAxisState(frame, buttonName);
						if (applyPatternToPaintedInputToolStripMenuItem.Checked && (!onlyOnAutoFireColumnsToolStripMenuItem.Checked
							|| targetCol.Emphasis))
						{
							AxisPatterns[ControllerType.Axes.IndexOf(buttonName)].Reset();
							CurrentTasMovie.SetAxisState(frame, buttonName, AxisPatterns[ControllerType.Axes.IndexOf(buttonName)].GetNextValue());
							_patternPaint = true;
						}
						else
						{
							_patternPaint = false;
						}


						if (e.Clicks != 2 && !Settings.SingleClickAxisEdit)
						{
							CurrentTasMovie.ChangeLog.BeginNewBatch($"Paint Axis {buttonName} from frame {frame}");
							_startAxisDrawColumn = buttonName;
						}
						else // Double-click enters axis editing mode
						{
							if (_axisEditColumn == buttonName && _axisEditRow == frame)
							{
								AxisEditRow = -1;
							}
							else
							{
								CurrentTasMovie.ChangeLog.BeginNewBatch($"Axis Edit: {frame}");
								_axisEditColumn = buttonName;
								AxisEditRow = frame;
								_axisTypedValue = "";
								_axisEditYPos = e.Y;
								_axisBackupState = CurrentTasMovie.GetAxisState(_axisEditRow, _axisEditColumn);
							}

							RefreshDialog();
						}
					}
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				if (targetCol.Name is FrameColumnName && frame < CurrentTasMovie.InputLogLength)
				{
					_rightClickControl = (ModifierKeys | Keys.Control) == ModifierKeys;
					_rightClickShift = (ModifierKeys | Keys.Shift) == ModifierKeys;
					_rightClickAlt = (ModifierKeys | Keys.Alt) == ModifierKeys;
					if (tasView.IsRowSelected(frame))
					{
						_rightClickInput = new string[tasView.SelectedRows.Count()];
						_rightClickFrame = tasView.SelectionStartIndex!.Value;
						try
						{
							CurrentTasMovie.GetLogEntries().CopyTo(_rightClickFrame, _rightClickInput, 0, _rightClickInput.Length);
						}
						catch { }
						if (_rightClickControl && _rightClickShift)
						{
							_rightClickFrame += _rightClickInput.Length;
						}
					}
					else
					{
						_rightClickInput = new string[1];
						_rightClickInput[0] = CurrentTasMovie.GetInputLogEntry(frame);
						_rightClickFrame = frame;
					}

					_rightClickLastFrame = -1;

					if (_rightClickAlt || _rightClickControl || _rightClickShift)
					{
						JumpToGreenzone();

						// TODO: Turn off ChangeLog.IsRecording and handle the GeneralUndo here.
						string undoStepName = "Right-Click Edit:";
						if (_rightClickShift)
						{
							undoStepName += " Extend Input";
							if (_rightClickControl)
							{
								undoStepName += ", Insert";
							}
						}
						else
						{
							if (_rightClickControl)
							{
								undoStepName += " Copy";
							}
							else // _rightClickAlt
							{
								undoStepName += " Move";
							}
						}

						CurrentTasMovie.ChangeLog.BeginNewBatch(undoStepName);
					}
				}
			}
		}

		private void ClearLeftMouseStates()
		{
			_startCursorDrag = false;
			_startSelectionDrag = false;
			_startBoolDrawColumn = "";
			_startAxisDrawColumn = "";
			_drewAxis = false;
			_paintingMinFrame = -1;
			foreach (InputRoll tasView in TasViews)
			{
				tasView.ReleaseCurrentCell();
			}
			// Exit axis editing if value was changed with cursor
			if (AxisEditingMode && _axisPaintState != CurrentTasMovie.GetAxisState(_axisEditRow, _axisEditColumn))
			{
				AxisEditRow = -1;
				_triggerAutoRestore = true;
				TastudioPlayMode(true);
				JumpToGreenzone();
				DoTriggeredAutoRestoreIfNeeded();
				RefreshDialog();
			}
			_axisPaintState = 0;
			_axisEditYPos = -1;
			_leftButtonHeld = false;

			if (!AxisEditingMode)
			{
				CurrentTasMovie.ChangeLog?.EndBatch();
			}

			MainForm.BlockFrameAdvance = false;
		}

		private void TasView_MouseUp(object sender, MouseEventArgs e)
		{
			var tasView = sender as InputRoll;

			if (e.Button == MouseButtons.Right && !tasView.IsPointingAtColumnHeader
				&& !_suppressContextMenu && !_leftButtonHeld && tasView.AnyRowsSelected)
			{
				if (CurrentTasMovie.FrameCount < tasView.SelectionEndIndex)
				{
					// trying to be smart here
					// if a loaded branch log is shorter than selection, keep selection until you attempt to call context menu
					// you might need it when you load again the branch where this frame exists
					tasView.DeselectAll();
					SetTasViewRowCount();
				}
				else
				{
					var offset = new Point(0);
					var topLeft = Cursor.Position;
					var bottomRight = new Point(
						topLeft.X + RightClickMenu.Width,
						topLeft.Y + RightClickMenu.Height);
					var screen = Screen.AllScreens.First(s => s.WorkingArea.Contains(topLeft));
					// if we don't fully fit, move to the other side of the pointer
					if (bottomRight.X > screen.WorkingArea.Right)
						offset.X -= RightClickMenu.Width;
					if (bottomRight.Y > screen.WorkingArea.Bottom)
						offset.Y -= RightClickMenu.Height;
					topLeft.Offset(offset);
					// if the screen is insultingly tiny, best we can do is avoid negative pos
					RightClickMenu.Show(
						Math.Max(0, topLeft.X),
						Math.Max(0, topLeft.Y));
				}
			}
			else if (e.Button == MouseButtons.Left)
			{
				if (AxisEditingMode && (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift))
				{
					_leftButtonHeld = false;
					_startSelectionDrag = false;
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(_startBoolDrawColumn) || _drewAxis)
					{
						// If painting up, we have altered frames without loading states (for smoothness)
						// So now we have to ensure that all the edited frames are invalidated
						GoToLastEmulatedFrameIfNecessary(_paintingMinFrame);
					}

					ClearLeftMouseStates();
				}

				DoTriggeredAutoRestoreIfNeeded();
			}

			if (e.Button == MouseButtons.Right)
			{
				if (_rightClickFrame != -1)
				{
					_rightClickInput = null;
					_rightClickOverInput = null;
					_rightClickFrame = -1;
					CurrentTasMovie.ChangeLog.EndBatch();
				}
			}

			_suppressContextMenu = false;
		}

		private void TasView_MouseWheel(object sender, MouseEventArgs e)
		{

			var tasView = sender as InputRoll;

			if (tasView.RightButtonHeld && tasView?.CurrentCell.RowIndex.HasValue == true)
			{
				_suppressContextMenu = true;
				int notch = e.Delta / 120;
				if (notch > 1)
				{
					notch *= 2;
				}

				// warning: tastudio rewind hotkey/button logic is copy pasted from here!
				if (MainForm.IsSeeking && !MainForm.EmulatorPaused)
				{
					MainForm.PauseOnFrame -= notch;

					// that's a weird condition here, but for whatever reason it works best
					if (notch > 0 && Emulator.Frame >= MainForm.PauseOnFrame)
					{
						MainForm.PauseEmulator();
						StopSeeking();
						GoToFrame(Emulator.Frame - notch);
					}

					RefreshDialog();
				}
				else
				{
					// needed for AutoAdjustInput() when it removes was-lag frames
					MainForm.HoldFrameAdvance = true;

					GoToFrame(Emulator.Frame - notch);
				}
			}
		}

		private void TasView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			var tasView = sender as InputRoll;

			if (tasView.CurrentCell?.Column is not { Name: var columnName }) return;

			if (e.Button == MouseButtons.Left)
			{
				if (!AxisEditingMode && tasView.CurrentCell.RowIndex is not null && columnName is FrameColumnName)
				{
					var existingMarker = CurrentTasMovie.Markers.FirstOrDefault(m => m.Frame == tasView.CurrentCell.RowIndex.Value);

					if (existingMarker != null)
					{
						MarkerControlMPR.EditMarkerPopUp(existingMarker);
					}
					else
					{
						ClearLeftMouseStates();
						MarkerControlMPR.AddMarker(tasView.CurrentCell.RowIndex.Value);
					}
				}
			}
		}

		private void TasView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			var tasView = sender as InputRoll;

			toolTip1.SetToolTip(tasView, null);

			if (e.NewCell.RowIndex is null)
			{
				return;
			}

			if (!MouseButtonHeld)
			{
				return;
			}

			if (_paintingMinFrame >= 0)
			{
				_paintingMinFrame = Math.Min(_paintingMinFrame, e.NewCell.RowIndex.Value);
			}

			// skip rerecord counting on drawing entirely, mouse down is enough
			// avoid introducing another global
			bool wasCountingRerecords = CurrentTasMovie.IsCountingRerecords;
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;

			int startVal, endVal;
			int frame = e.NewCell.RowIndex.Value;
			if (e.OldCell.RowIndex < e.NewCell.RowIndex)
			{
				startVal = e.OldCell.RowIndex.Value;
				endVal = e.NewCell.RowIndex.Value;
				if (_patternPaint)
				{
					endVal--;
				}
			}
			else
			{
				startVal = e.NewCell.RowIndex.Value;
				endVal = e.OldCell.RowIndex ?? e.NewCell.RowIndex.Value;
				if (_patternPaint)
				{
					endVal = _startRow;
				}
			}

			if (_startCursorDrag && !MainForm.IsSeeking)
			{
				GoToFrame(e.NewCell.RowIndex.Value);
			}
			else if (_startSelectionDrag)
			{
				for (var i = startVal; i <= endVal; i++)
				{
					if (!tasView.IsRowSelected(i))
						tasView.SelectRow(i, _selectionDragState);
					if (AxisEditingMode && (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift))
					{
						if (_selectionDragState)
						{
							_extraAxisRows.Add(i);
						}
						else
						{
							_extraAxisRows.Remove(i);
						}
					}
				}

				SetSplicer();
			}
			else if (_rightClickFrame != -1)
			{
				if (frame > CurrentTasMovie.InputLogLength - _rightClickInput.Length)
				{
					frame = CurrentTasMovie.InputLogLength - _rightClickInput.Length;
				}

				if (_rightClickShift)
				{
					if (_rightClickControl) // Insert
					{
						// If going backwards, delete!
						bool shouldInsert = true;
						if (startVal < _rightClickFrame)
						{
							// Cloning to a previous frame makes no sense.
							startVal = _rightClickFrame - 1;
						}

						if (startVal < _rightClickLastFrame)
						{
							shouldInsert = false;
						}

						if (shouldInsert)
						{
							for (int i = startVal + 1; i <= endVal; i++)
							{
								CurrentTasMovie.InsertInput(i, _rightClickInput[(i - _rightClickFrame).Mod(_rightClickInput.Length)]);
							}
						}
						else
						{
							CurrentTasMovie.RemoveFrames(startVal + 1, endVal + 1);
						}

						_rightClickLastFrame = frame;
					}
					else // Overwrite
					{
						for (int i = startVal; i <= endVal; i++)
						{
							CurrentTasMovie.SetFrame(i, _rightClickInput[(i - _rightClickFrame).Mod(_rightClickInput.Length)]);
						}
					}
				}
				else
				{
					if (_rightClickControl)
					{
						for (int i = 0; i < _rightClickInput.Length; i++) // Re-set initial range, just to verify it's still there.
						{
							CurrentTasMovie.SetFrame(_rightClickFrame + i, _rightClickInput[i]);
						}

						if (_rightClickOverInput != null) // Restore overwritten input from previous movement
						{
							for (int i = 0; i < _rightClickOverInput.Length; i++)
							{
								CurrentTasMovie.SetFrame(_rightClickLastFrame + i, _rightClickOverInput[i]);
							}
						}
						else
						{
							_rightClickOverInput = new string[_rightClickInput.Length];
						}

						_rightClickLastFrame = frame; // Set new restore log
						CurrentTasMovie.GetLogEntries().CopyTo(frame, _rightClickOverInput, 0, _rightClickOverInput.Length);

						for (int i = 0; i < _rightClickInput.Length; i++) // Place copied input
						{
							CurrentTasMovie.SetFrame(frame + i, _rightClickInput[i]);
						}
					}
					else if (_rightClickAlt)
					{
						int shiftBy = _rightClickFrame - frame;
						string[] shiftInput = new string[Math.Abs(shiftBy)];
						int shiftFrom = frame;
						if (shiftBy < 0)
						{
							shiftFrom = _rightClickFrame + _rightClickInput.Length;
						}

						CurrentTasMovie.GetLogEntries().CopyTo(shiftFrom, shiftInput, 0, shiftInput.Length);
						int shiftTo = shiftFrom + (_rightClickInput.Length * Math.Sign(shiftBy));
						for (int i = 0; i < shiftInput.Length; i++)
						{
							CurrentTasMovie.SetFrame(shiftTo + i, shiftInput[i]);
						}

						for (int i = 0; i < _rightClickInput.Length; i++)
						{
							CurrentTasMovie.SetFrame(frame + i, _rightClickInput[i]);
						}

						_rightClickFrame = frame;
					}
				}

				if (_rightClickAlt || _rightClickControl || _rightClickShift)
				{
					_triggerAutoRestore = true;
					TastudioPlayMode(true);
					JumpToGreenzone();
					_suppressContextMenu = true;
				}
			}

			// Left-click
			else if (tasView.IsPaintDown && !string.IsNullOrEmpty(_startBoolDrawColumn))
			{
				CurrentTasMovie.IsCountingRerecords = false;

				for (int i = startVal; i <= endVal; i++) // Inclusive on both ends (drawing up or down)
				{
					bool setVal = _boolPaintState;

					if (_patternPaint && _boolPaintState)
					{
						if (CurrentTasMovie[frame].Lagged.HasValue && CurrentTasMovie[frame].Lagged.Value)
						{
							setVal = CurrentTasMovie.BoolIsPressed(i - 1, _startBoolDrawColumn);
						}
						else
						{
							setVal = BoolPatterns[ControllerType.BoolButtons.IndexOf(_startBoolDrawColumn)].GetNextValue();
						}
					}

					CurrentTasMovie.SetBoolState(i, _startBoolDrawColumn, setVal); // Notice it uses new row, old column, you can only paint across a single column

					if (!_triggerAutoRestore)
					{
						TastudioPlayMode(true);
						JumpToGreenzone();
					}
				}
			}

			else if (tasView.IsPaintDown && !string.IsNullOrEmpty(_startAxisDrawColumn))
			{
				CurrentTasMovie.IsCountingRerecords = false;

				for (int i = startVal; i <= endVal; i++) // Inclusive on both ends (drawing up or down)
				{
					var setVal = _axisPaintState;
					if (_patternPaint)
					{
						if (CurrentTasMovie[frame].Lagged.HasValue && CurrentTasMovie[frame].Lagged.Value)
						{
							setVal = CurrentTasMovie.GetAxisState(i - 1, _startAxisDrawColumn);
						}
						else
						{
							setVal = AxisPatterns[ControllerType.Axes.IndexOf(_startAxisDrawColumn)].GetNextValue();
						}
					}

					CurrentTasMovie.SetAxisState(i, _startAxisDrawColumn, setVal); // Notice it uses new row, old column, you can only paint across a single column
				}

				_drewAxis = true;
			}

			CurrentTasMovie.IsCountingRerecords = wasCountingRerecords;

			if (MouseButtonHeld)
			{
				tasView.MakeIndexVisible(tasView.CurrentCell.RowIndex.Value); // todo: limit scrolling speed
			}

			SetTasViewRowCount();
		}

		private void TasView_MouseMove(object sender, MouseEventArgs e)
		{
			// For axis editing
			if (AxisEditingMode)
			{
				int increment = (_axisEditYPos - e.Y) / 4;
				if (_axisEditYPos == -1)
				{
					return;
				}

				var value = (_axisPaintState + increment).ConstrainWithin(ControllerType.Axes[_axisEditColumn].Range);
				CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, value);
				_axisTypedValue = value.ToString();
				RefreshDialog();
			}
		}

		private void TasView_SelectedIndexChanged(object sender, EventArgs e)
		{
			SetSplicer();
			CurrentTasView = sender as InputRoll;
		}

		public void AnalogIncrementByOne()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Up));
			}
		}

		public void AnalogDecrementByOne()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Down));
			}
		}

		public void AnalogIncrementByTen()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Up | Keys.Shift));
			}
		}

		public void AnalogDecrementByTen()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Down | Keys.Shift));
			}
		}

		public void AnalogMax()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Right));
			}
		}

		public void AnalogMin()
		{
			if (AxisEditingMode)
			{
				EditAnalogProgrammatically(new KeyEventArgs(Keys.Left));
			}
		}

		public void EditAnalogProgrammatically(KeyEventArgs e)
		{
			if (!AxisEditingMode)
			{
				return;
			}

			int value = CurrentTasMovie.GetAxisState(_axisEditRow, _axisEditColumn);
			int prev = value;
			string prevTyped = _axisTypedValue;

			var range = ControllerType.Axes[_axisEditColumn];

			// feos: typing past max digits overwrites existing value, not touching the sign
			// but doesn't handle situations where the range is like -50 through 100, where minimum is negative and has less digits
			// it just uses 3 as maxDigits there too, leaving room for typing impossible values (that are still ignored by the game and then clamped)
			int maxDigits = range.MaxDigits;
			int curDigits = _axisTypedValue.Length;
			string curMinus;
			if (_axisTypedValue.StartsWith('-'))
			{
				curDigits -= 1;
				curMinus = "-";
			}
			else
			{
				curMinus = "";
			}

			if (e.KeyCode == Keys.Right)
			{
				value = range.Max;
				_axisTypedValue = value.ToString(NumberFormatInfo.InvariantInfo);
			}
			else if (e.KeyCode == Keys.Left)
			{
				value = range.Min;
				_axisTypedValue = value.ToString(NumberFormatInfo.InvariantInfo);
			}
			else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
			{
				if (curDigits >= maxDigits)
				{
					_axisTypedValue = curMinus;
				}

				_axisTypedValue += e.KeyCode - Keys.D0;
			}
			else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
			{
				if (curDigits >= maxDigits)
				{
					_axisTypedValue = curMinus;
				}

				_axisTypedValue += e.KeyCode - Keys.NumPad0;
			}
			else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
			{
				_axisTypedValue = _axisTypedValue.StartsWith('-')
					? _axisTypedValue.Substring(startIndex: 1)
					: $"-{_axisTypedValue}";
			}
			else if (e.KeyCode == Keys.Back)
			{
				if (_axisTypedValue.Length is 0) // Very first key press is backspace?
				{
					_axisTypedValue = value.ToString(NumberFormatInfo.InvariantInfo);
				}

				_axisTypedValue = _axisTypedValue.Substring(startIndex: 0, length: _axisTypedValue.Length - 1); // drop last char
				if (!int.TryParse(_axisTypedValue, out value)) value = 0;
			}
			else if (e.KeyCode == Keys.Enter)
			{
				_axisEditYPos = -1;
				AxisEditRow = -1;
			}
			else if (e.KeyCode == Keys.Escape)
			{
				_axisEditYPos = -1;

				if (_axisBackupState != _axisPaintState)
				{
					CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, _axisBackupState);
					_triggerAutoRestore = Emulator.Frame > _axisEditRow;
					TastudioPlayMode(true);
					JumpToGreenzone();
					DoTriggeredAutoRestoreIfNeeded();
				}

				AxisEditRow = -1;
			}
			else
			{
				int changeBy = 0;
				if (e.KeyCode == Keys.Up)
				{
					changeBy = 1;
				}
				else if (e.KeyCode == Keys.Down)
				{
					changeBy = -1;
				}

				if (ModifierKeys == Keys.Shift)
				{
					changeBy *= 10;
				}

				value += changeBy;
				if (changeBy != 0)
				{
					_axisTypedValue = value.ToString(NumberFormatInfo.InvariantInfo);
				}
			}

			if (!AxisEditingMode)
			{
				CurrentTasMovie.ChangeLog.EndBatch();
			}
			else
			{
				if (_axisTypedValue.Length is 0)
				{
					if (prevTyped != "")
					{
						value = ControllerType.Axes[_axisEditColumn].Neutral;
						CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, value);
					}
				}
				else
				{
					if (int.TryParse(_axisTypedValue, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out value)) // String "-" can't be parsed.
					{
						value = value.ConstrainWithin(range.Range);

						CurrentTasMovie.SetAxisState(_axisEditRow, _axisEditColumn, value);
					}
				}

				foreach (int row in _extraAxisRows)
				{
					CurrentTasMovie.SetAxisState(row, _axisEditColumn, value);
				}

				if (value != prev) // Auto-restore
				{
					_triggerAutoRestore = Emulator.Frame > _axisEditRow;
					TastudioPlayMode(true);
					JumpToGreenzone();
					DoTriggeredAutoRestoreIfNeeded();
				}
			}

			RefreshDialog();
		}

		private void TasView_KeyDown(object sender, KeyEventArgs e)
		{
			// taseditor uses Ctrl for selection and Shift for frame cursor
			if (e.IsShift(Keys.PageUp))
			{
				GoToPreviousMarker();
			}
			else if (e.IsShift(Keys.PageDown))
			{
				GoToNextMarker();
			}
			else if (e.IsShift(Keys.Home))
			{
				GoToFrame(0);
			}
			else if (e.IsShift(Keys.End))
			{
				GoToFrame(CurrentTasMovie.InputLogLength - 1);
			}

			if (AxisEditingMode
				&& e.KeyCode != Keys.Right
				&& e.KeyCode != Keys.Left
				&& e.KeyCode != Keys.Up
				&& e.KeyCode != Keys.Down)
			{
				EditAnalogProgrammatically(e);
			}

			RefreshDialog();
		}
	}
}
