using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBoxMPR : UserControl, IDialogParent
	{
		private const string BranchNumberColumnName = "BranchNumberColumn";
		private const string FrameColumnName = "FrameColumn";
		private const string UserTextColumnName = "TextColumn";

		private readonly ScreenshotForm _screenshot = new ScreenshotForm();

		private ITasMovie Movie => TastudioMPR.CurrentTasMovie;
		private ITasBranchCollection Branches => Movie.Branches;

		private IMainFormForTools MainForm => TastudioMPR.MainForm;
		private TasBranch _backupBranch;
		private BranchUndo _branchUndo = BranchUndo.None;

		private enum BranchUndo
		{
			Load, Update, Text, Remove, None
		}

		public Action<int> LoadedCallback { get; set; }

		public Action<int> SavedCallback { get; set; }

		public Action<int> RemovedCallback { get; set; }

		public TAStudioMPR TastudioMPR { get; set; }

		public IDialogController DialogController => TastudioMPR.MainForm;

		public BookmarksBranchesBoxMPR()
		{
			InitializeComponent();
			UndoBranchButton.Image = Resources.Undo;
			JumpToBranchButton.Image = Resources.JumpTo;
			UpdateBranchButton.Image = Resources.Reboot;
			AddWithTextBranchButton.Image = Resources.AddEdit;
			AddBranchButton.Image = Resources.Add;
			LoadBranchButton.Image = Resources.Debugger;
			AddBranchContextMenu.Image = Resources.Add;
			AddBranchWithTextContextMenuItem.Image = Resources.AddEdit;
			LoadBranchContextMenuItem.Image = Resources.Debugger;
			UpdateBranchContextMenuItem.Image = Resources.Reboot;
			EditBranchTextContextMenuItem.Image = Resources.Pencil;
			JumpToBranchContextMenuItem.Image = Resources.JumpTo;
			UndoBranchToolStripMenuItem.Image = Resources.Undo;
			RemoveBranchContextMenuItem.Image = Resources.Delete;
			SetupColumns();
			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
		}

		private void SetupColumns()
		{
			BranchView.AllColumns.Clear();
			BranchView.AllColumns.Add(new(name: BranchNumberColumnName, widthUnscaled: 30, text: "#"));
			BranchView.AllColumns.Add(new(name: FrameColumnName, widthUnscaled: 64, text: "Frame"));
			BranchView.AllColumns.Add(new(name: UserTextColumnName, widthUnscaled: 90, text: "UserText"));
		}

		private void QueryItemText(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY)
		{
			text = "";

			// This could happen if the control is told to redraw while Tastudio is rebooting, as we would not have a TasMovie just yet
			if (TastudioMPR.CurrentTasMovie == null)
			{
				return;
			}

			text = column.Name switch
			{
				BranchNumberColumnName => (index + 1).ToString(),
				FrameColumnName => Branches[index].Frame.ToString(),
				UserTextColumnName => Branches[index].UserText,
				_ => text
			};
		}

		private void QueryItemBkColor(int index, RollColumn column, ref Color color)
		{
			// This could happen if the control is told to redraw while Tastudio is rebooting, as we would not have a TasMovie just yet
			if (TastudioMPR.CurrentTasMovie == null)
			{
				return;
			}

			var branch = Branches[index];
			if (branch != null)
			{
				var record = Movie[branch.Frame];
				if (index == Branches.Current)
				{
					color = TastudioMPR.Palette.CurrentFrame_InputLog;
				}
				else if (record.Lagged.HasValue)
				{
					color = record.Lagged.Value
						? TastudioMPR.Palette.LagZone_InputLog
						: TastudioMPR.Palette.GreenZone_InputLog;
				}
			}

			// Highlight the branch cell a little, if hovering over it
			if (BranchView.CurrentCell is { RowIndex: int targetRow, Column.Name: BranchNumberColumnName }
				&& index == targetRow && column.Name is BranchNumberColumnName)
			{
				color = Color.FromArgb((byte)(color.A - 24), (byte)(color.R - 24), (byte)(color.G - 24), (byte)(color.B - 24));
			}
		}

		// used to capture a reserved state immediately on branch creation
		// while also not doing a double state
		private class BufferedStatable : IStatable
		{
			private readonly byte[] _bufferedState;

			public BufferedStatable(byte[] state)
				=> _bufferedState = state;

			public bool AvoidRewind => true;

			public void SaveStateBinary(BinaryWriter writer)
				=> writer.Write(_bufferedState);

			public void LoadStateBinary(BinaryReader reader)
				=> throw new NotImplementedException();
		}

		/// <summary>
		/// Add a new branch.
		/// </summary>
		public void Branch()
		{
			var branch = CreateBranch();
			Movie.Branches.NewBranchText = ""; // reset every time it's used
			Branches.Add(branch);
			BranchView.RowCount = Branches.Count;
			Branches.Current = Branches.Count - 1;
			Movie.TasSession.UpdateValues(TastudioMPR.Emulator.Frame, Branches.Current);
			Movie.TasStateManager.Capture(TastudioMPR.Emulator.Frame, new BufferedStatable(branch.CoreData));
			BranchView.ScrollToIndex(Branches.Current);
			BranchView.DeselectAll();
			Select(Branches.Current, true);
			BranchView.Refresh();
			TastudioMPR.RefreshDialog();
			MainForm.UpdateStatusSlots();
		}

		public TasBranch SelectedBranch
			=> BranchView.AnyRowsSelected ? Branches[BranchView.FirstSelectedRowIndex] : null;

		private TasBranch CreateBranch()
		{
			return new()
			{
				Frame = TastudioMPR.Emulator.Frame,
				CoreData = TastudioMPR.Emulator.AsStatable().CloneSavestate(),
				InputLog = Movie.GetLogEntries().Clone(),
				CoreFrameBuffer = MainForm.MakeScreenshotImage(),
				OSDFrameBuffer = MainForm.CaptureOSD(),
				ChangeLog = new(Movie),
				TimeStamp = DateTime.Now,
				Markers = Movie.Markers.DeepClone(),
				UserText = Movie.Branches.NewBranchText
			};
		}

		private void LoadBranch(TasBranch branch)
		{
			if (TastudioMPR.Settings.OldControlSchemeForBranches && !TastudioMPR.TasPlaybackBoxMPR.RecordingMode)
			{
				JumpToBranchToolStripMenuItem_Click(null, null);
				return;
			}

			Movie.LoadBranch(branch);
			TastudioMPR.LoadState(new(branch.Frame, new MemoryStream(branch.CoreData, false)));
			// set the controller state to the previous frame for input display purposes
			int previousFrame = Movie.Emulator.Frame - 1;
			TastudioMPR.MovieSession.MovieController.SetFrom(Movie.GetInputState(previousFrame));

			Movie.TasStateManager.Capture(TastudioMPR.Emulator.Frame, TastudioMPR.Emulator.AsStatable());
			QuickBmpFile.Copy(new BitmapBufferVideoProvider(branch.CoreFrameBuffer), TastudioMPR.VideoProvider);

			if (TastudioMPR.Settings.OldControlSchemeForBranches && TastudioMPR.TasPlaybackBoxMPR.RecordingMode)
				Movie.Truncate(branch.Frame);

			MainForm.PauseOnFrame = null;
			TastudioMPR.RefreshDialog();
		}

		private bool LoadSelectedBranch()
		{
			if (SelectedBranch == null) return false;
			Branches.Current = BranchView.FirstSelectedRowIndex;
			LoadBranch(SelectedBranch);
			BranchView.Refresh();
			TastudioMPR.MainForm.AddOnScreenMessage($"Loaded branch {Branches.Current + 1}");
			return true;
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RemoveBranchContextMenuItem.Enabled = SelectedBranch != null;

			UpdateBranchContextMenuItem.Enabled =
				LoadBranchContextMenuItem.Enabled =
				EditBranchTextContextMenuItem.Enabled =
				JumpToBranchContextMenuItem.Enabled =
					BranchView.SelectedRows.CountIsExactly(1);
		}

		private void AddBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			SavedCallback?.Invoke(Branches.Count - 1);
			TastudioMPR.MainForm.AddOnScreenMessage($"Added branch {Branches.Current + 1}");
		}

		private void AddBranchWithTexToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Branch();
			EditBranchTextPopUp(Branches.Current);
			SavedCallback?.Invoke(Branches.Count - 1);
			TastudioMPR.MainForm.AddOnScreenMessage($"Added branch {Branches.Current + 1}");
		}

		private bool PrepareHistoryAndLoadSelectedBranch()
		{
			_backupBranch = CreateBranch();

			var currentHashes = Branches.Select(b => b.Uuid.GetHashCode()).ToList();
			do
			{
				_backupBranch.Uuid = Guid.NewGuid();
			}
			while (currentHashes.Contains(_backupBranch.Uuid.GetHashCode()));

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Load";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Load");
			_branchUndo = BranchUndo.Load;

			if (!BranchView.AnyRowsSelected) return false; // why'd we do all that then

			var success = LoadSelectedBranch();
			LoadedCallback?.Invoke(BranchView.FirstSelectedRowIndex);
			return success;
		}

		private void LoadBranchToolStripMenuItem_Click(object sender, EventArgs e)
			=> PrepareHistoryAndLoadSelectedBranch();

		private void UpdateBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch == null)
			{
				return;
			}

			Branches.Current = BranchView.FirstSelectedRowIndex;

			_backupBranch = SelectedBranch.Clone();
			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Update";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Update");
			_branchUndo = BranchUndo.Update;

			BranchView.ScrollToIndex(Branches.Current);
			var branch = CreateBranch();
			Branches.Replace(SelectedBranch, branch);
			Movie.TasStateManager.Capture(TastudioMPR.Emulator.Frame, new BufferedStatable(branch.CoreData));
			TastudioMPR.RefreshDialog();
			SavedCallback?.Invoke(Branches.Current);
			TastudioMPR.MainForm.AddOnScreenMessage($"Saved branch {Branches.Current + 1}");
		}

		private void EditBranchTextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch == null)
			{
				return;
			}

			var index = BranchView.FirstSelectedRowIndex;
			string oldText = SelectedBranch.UserText;

			if (EditBranchTextPopUp(index))
			{
				_backupBranch = SelectedBranch.Clone();
				_backupBranch.UserText = oldText;
				UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
				UndoBranchToolStripMenuItem.Text = "Undo Branch Text Edit";
				toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Text Edit");
				_branchUndo = BranchUndo.Text;

				TastudioMPR.MainForm.AddOnScreenMessage($"Edited branch {index + 1}");
			}
		}

		private void JumpToBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (BranchView.AnyRowsSelected) TastudioMPR.GoToFrame(Branches[BranchView.FirstSelectedRowIndex].Frame);
		}

		private void RemoveBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!BranchView.AnyRowsSelected)
			{
				return;
			}

			var indices = BranchView.SelectedRows.ToList();
			var branches = Branches.ToList();
			foreach (var index in indices)
			{
				_backupBranch =  branches[index].Clone();
				Branches.Remove(branches[index]);
				RemovedCallback?.Invoke(index);
				TastudioMPR.MainForm.AddOnScreenMessage($"Removed branch {index + 1}");

				if (index == Branches.Current)
				{
					Branches.Current = -1;
				}
				else if (index < Branches.Current)
				{
					Branches.Current--;
				}
			}

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = true;
			UndoBranchToolStripMenuItem.Text = "Undo Branch Removal";
			toolTip1.SetToolTip(UndoBranchButton, "Undo Branch Removal");
			_branchUndo = BranchUndo.Remove;

			BranchView.RowCount = Branches.Count;
			TastudioMPR.RefreshDialog(refreshBranches: false);
			MainForm.UpdateStatusSlots();
		}

		private void UndoBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_branchUndo == BranchUndo.Load)
			{
				LoadBranch(_backupBranch);
				LoadedCallback?.Invoke(Branches.IndexOf(_backupBranch));
				TastudioMPR.MainForm.AddOnScreenMessage("Branch Load canceled");
			}
			else if (_branchUndo == BranchUndo.Update)
			{
				var branch = Branches.SingleOrDefault(b => b.Uuid == _backupBranch.Uuid);
				if (branch != null)
				{
					Branches.Replace(branch, _backupBranch);
					SavedCallback?.Invoke(Branches.IndexOf(_backupBranch));
					TastudioMPR.MainForm.AddOnScreenMessage("Branch Update canceled");
				}
			}
			else if (_branchUndo == BranchUndo.Text)
			{
				var branch = Branches.SingleOrDefault(b => b.Uuid == _backupBranch.Uuid);
				if (branch != null)
				{
					branch.UserText = _backupBranch.UserText;
				}

				TastudioMPR.MainForm.AddOnScreenMessage("Branch Text Edit canceled");
			}
			else if (_branchUndo == BranchUndo.Remove)
			{
				Branches.Add(_backupBranch);
				BranchView.RowCount = Branches.Count;
				SavedCallback?.Invoke(Branches.IndexOf(_backupBranch));
				TastudioMPR.MainForm.AddOnScreenMessage("Branch Removal canceled");
			}

			UndoBranchToolStripMenuItem.Enabled = UndoBranchButton.Enabled = false;
			BranchView.Refresh();
			TastudioMPR.RefreshDialog();
		}

		public void AddBranchExternal()
		{
			AddBranchToolStripMenuItem_Click(null, null);
		}

		public bool LoadBranchExternal(int slot = -1)
		{
			if (TastudioMPR.AxisEditingMode)
			{
				return false;
			}

			if (slot != -1)
			{
				if (Branches[slot] != null)
				{
					BranchView.DeselectAll();
					Select(slot, true);
				}
				else
				{
					NonExistentBranchMessage(slot);
					return false;
				}
			}

			return PrepareHistoryAndLoadSelectedBranch();
		}

		public void UpdateBranchExternal(int slot = -1)
		{
			if (TastudioMPR.AxisEditingMode)
			{
				return;
			}

			if (slot != -1)
			{
				if (Branches[slot] != null)
				{
					BranchView.DeselectAll();
					Select(slot, true);
				}
				else
				{
					//NonExistentBranchMessage(slot); // some people can't get used to creating branches explicitly with unusual hotkeys
					AddBranchExternal(); // so just make a new branch, even though the index may be wrong
					return;
				}
			}

			UpdateBranchToolStripMenuItem_Click(null, null);
		}

		public void RemoveBranchExternal()
		{
			RemoveBranchToolStripMenuItem_Click(null, null);
		}

		public void SelectBranchExternal(int slot)
		{
			if (TastudioMPR.AxisEditingMode)
			{
				return;
			}

			if (Branches[slot] != null)
			{
				Select(slot, true);
				BranchView.Refresh();
			}
			else
			{
				NonExistentBranchMessage(slot);
			}
		}

		public void SelectBranchExternal(bool next)
		{
			if (SelectedBranch == null)
			{
				if (Branches.Current != -1)
				{
					Select(Branches.Current, true);
					BranchView.Refresh();
				}
				return;
			}

			var sel = BranchView.FirstSelectedRowIndex;
			if (next)
			{
				if (Branches[sel + 1] != null)
				{
					Select(sel, false);
					Select(sel + 1, true);
				}
			}
			else // previous
			{
				if (Branches[sel - 1] != null)
				{
					Select(sel, false);
					Select(sel - 1, true);
				}
			}

			BranchView.Refresh();
		}

		private void UpdateButtons()
		{
			UpdateBranchButton.Enabled =
				LoadBranchButton.Enabled =
				JumpToBranchButton.Enabled =
					BranchView.SelectedRows.CountIsExactly(1);
		}

		private void Select(int index, bool value)
		{
			BranchView.SelectRow(index, value);
			UpdateButtons();
		}

		public void NonExistentBranchMessage(int slot)
		{
			TastudioMPR.MainForm.AddOnScreenMessage($"Branch {slot + 1} does not exist");
			TastudioMPR.MainForm.AddOnScreenMessage($"Use {TastudioMPR.Config!.HotkeyBindings["Add Branch"]} to add branches");
		}

		public void UpdateValues()
		{
			BranchView.RowCount = Branches.Count;
		}

		public void Restart()
		{
			BranchView.RowCount = Branches.Count;

			if (BranchView.RowCount == 0)
			{
				SetupColumns();
			}

			BranchView.Refresh();
		}

		public void UpdateTextColumnWidth()
		{
			if (Branches.Count > 0)
			{
				var longestBranchText = Branches
					.OrderBy(b => b.UserText?.Length ?? 0)
					.Last()
					.UserText;

				BranchView.ExpandColumnToFitText(UserTextColumnName, longestBranchText);
			}
		}

		public bool EditBranchTextPopUp(int index)
		{
			var branch = Branches[index];
			if (branch == null)
			{
				return false;
			}

			var i = new InputPrompt
			{
				Text = $"Text for branch {index + 1}",
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = branch.UserText
			};

			i.FollowMousePointer();
			if (i.ShowDialogOnScreen().IsOk())
			{
				branch.UserText = i.PromptText;
				UpdateTextColumnWidth();
				UpdateValues();
				return true;
			}

			return false;
		}

		private void BranchView_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (BranchView.CurrentCell is { RowIndex: not null, Column.Name: BranchNumberColumnName })
				{
					BranchView.DragCurrentCell();
				}
			}
		}

		private void BranchView_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				BranchView.ReleaseCurrentCell();
			}
		}

		private void BranchView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (TastudioMPR.Settings.LoadBranchOnDoubleClick)
			{
				PrepareHistoryAndLoadSelectedBranch();
			}
		}

		private void BranchView_MouseMove(object sender, MouseEventArgs e)
		{
			if (BranchView.CurrentCell?.RowIndex == null || BranchView.CurrentCell.Column == null)
			{
				_screenshot.FadeOut();
			}
			else if (BranchView.CurrentCell.Column.Name == BranchNumberColumnName)
			{
				BranchView.Refresh();
			}
		}

		private void BranchView_MouseLeave(object sender, EventArgs e)
		{
			_screenshot.FadeOut();
		}

		private void BranchView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell.IsDataCell() && e.OldCell.RowIndex < Branches.Count)
			{
				var guid = Branches.Current > Branches.Count
					? Guid.Empty
					: Branches[Branches.Current].Uuid;

				Branches.Swap(e.OldCell.RowIndex.Value, e.NewCell.RowIndex.Value);
				int newIndex = Branches.IndexOfHash(guid);
				Branches.Current = newIndex;
				Select(newIndex, true);
			}
		}

		private void BranchView_PointedCellChanged(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell?.RowIndex != null && e.NewCell.Column != null && e.NewCell.RowIndex < Branches.Count)
			{
				if (BranchView.CurrentCell is { RowIndex: int targetRow, Column.Name: BranchNumberColumnName }
					&& targetRow < Branches.Count)
				{
					var branch = Branches[targetRow];
					var bb = branch.OSDFrameBuffer;
					var width = bb.Width;
					Point location = PointToScreen(Location);
					var bottom = location.Y + bb.Height;
					location.Offset(-width, 0);

					if (location.X < 0)
					{
						// show on the right of branch control
						location.Offset(width + Width, 0);
					}

					location.Y = Math.Max(0, location.Y);
					var screen = Screen.AllScreens.First(s => s.WorkingArea.Contains(location));
					var h = screen.WorkingArea.Bottom - bottom;

					if (h < 0)
					{
						// move up to become fully visible
						location.Y += h;
					}

					_screenshot.UpdateValues(
						bb,
						branch.UserText,
						location,
						width: width,
						height: bb.Height,
						Graphics.FromHwnd(Handle).MeasureString);
					_screenshot.FadeIn();
				}
				else
				{
					_screenshot.FadeOut();
				}
			}
			else
			{
				_screenshot.FadeOut();
			}
		}

		private void BranchView_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}
	}
}
