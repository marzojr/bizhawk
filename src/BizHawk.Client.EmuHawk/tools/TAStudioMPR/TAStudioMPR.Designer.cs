using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class TAStudioMPR
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.TASMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewFromSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewFromNowMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewFromCurrentSaveRamMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OpenTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveAsTASMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveBackupMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveBk2BackupMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.saveSelectionToMacroToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.placeMacroAtSelectionToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.recentMacrosToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator22 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.toolStripSeparator20 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ToBk2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.UndoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RedoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.showUndoHistoryToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectionUndoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectionRedoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator5 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DeselectMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectBetweenMarkersMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ReselectClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.CopyMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PasteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.PasteInsertMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CutMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ClearFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DeleteFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertFrameMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertNumFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneFramesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneFramesXTimesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.TruncateMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ClearGreenzoneMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.GreenzoneICheckSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.StateHistoryIntegrityCheckMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ConfigSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SetMaxUndoLevelsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CopyIncludesFrameNoMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator26 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.autosaveToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SetAutosaveIntervalMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AutosaveAsBk2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AutosaveAsBackupFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.BackupPerFileSaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator9 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.AutoadjustInputMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.applyPatternToPaintedInputToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.onlyOnAutoFireColumnsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SingleClickAxisEditMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.UseInputKeysItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.BindMarkersToInputMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OldControlSchemeForBranchesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.LoadBranchOnDoubleclickMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OsdInBranchScreenshotsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator14 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.AutopauseAtEndOfMovieMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.sepToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.autoHoldFireToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.keepSetPatternsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.sepToolStripMenuItem1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.autoHoldToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.autoFireToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.customPatternToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.setpToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.setCustomsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SetFontMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.disableTasViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MetaSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HeaderMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StateHistorySettingsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CommentsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SubtitlesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator21 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DefaultStateSettingsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RotateMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HideLagFramesSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HideLagFrames0 = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HideLagFrames1 = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HideLagFrames2 = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HideLagFrames3 = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator12 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.hideWasLagFramesToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.iconsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DenoteStatesWithIconsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DenoteStatesWithBGColorToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DenoteMarkersWithIconsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DenoteMarkersWithBGColorToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ColorSettingsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator23 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.followCursorToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.alwaysScrollToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator24 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.scrollToViewToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.scrollToTopToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.scrollToBottomToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.scrollToCenterToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator25 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.wheelScrollSpeedToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ColumnsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator19 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.HelpSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.TASEditorManualOnlineMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ForumThreadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.aboutToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator10 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.EnableTooltipsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.TasView1 = new BizHawk.Client.EmuHawk.InputRoll();
            this.TasStatusStrip = new BizHawk.WinForms.Controls.StatusStripEx();
            this.MessageStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.SplicerStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.TasPlaybackBoxMPR = new BizHawk.Client.EmuHawk.PlaybackBoxMPR();
            this.MarkerControlMPR = new BizHawk.Client.EmuHawk.MarkerControlMPR();
            this.RightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SetMarkersContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SetMarkerWithTextContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RemoveMarkersContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator15 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DeselectContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SelectBetweenMarkersContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator16 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.UngreenzoneContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CancelSeekContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator17 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.copyToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.pasteToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.pasteInsertToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.cutToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.separateToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ClearContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.DeleteFramesContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertFrameContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.InsertNumFramesContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.CloneXTimesContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator18 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.TruncateContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.BranchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StartFromNowSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.StartNewProjectFromNowMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StartANewProjectFromSaveRamMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.pasteSelectedRowsFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.BookMarkControlMPR = new BizHawk.Client.EmuHawk.BookmarksBranchesBoxMPR();
            this.BranchesMarkersSplit = new System.Windows.Forms.SplitContainer();
            this.MainVertialSplit = new System.Windows.Forms.SplitContainer();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.TASMenu.SuspendLayout();
            this.TasStatusStrip.SuspendLayout();
            this.RightClickMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BranchesMarkersSplit)).BeginInit();
            this.BranchesMarkersSplit.Panel1.SuspendLayout();
            this.BranchesMarkersSplit.Panel2.SuspendLayout();
            this.BranchesMarkersSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainVertialSplit)).BeginInit();
            this.MainVertialSplit.Panel1.SuspendLayout();
            this.MainVertialSplit.Panel2.SuspendLayout();
            this.MainVertialSplit.SuspendLayout();
            this.SuspendLayout();
            // 
            // TASMenu
            // 
            this.TASMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditSubMenu,
            this.ConfigSubMenu,
            this.MetaSubMenu,
            this.SettingsSubMenu,
            this.ColumnsSubMenu,
            this.HelpSubMenu});
            this.TASMenu.TabIndex = 0;
            this.TASMenu.MenuActivate += new System.EventHandler(this.TASMenu_MenuActivate);
            this.TASMenu.MenuDeactivate += new System.EventHandler(this.TASMenu_MenuDeactivate);
            // 
            // FileSubMenu
            // 
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewTASMenuItem,
            this.NewFromSubMenu,
            this.OpenTASMenuItem,
            this.SaveTASMenuItem,
            this.SaveAsTASMenuItem,
            this.SaveBackupMenuItem,
            this.SaveBk2BackupMenuItem,
            this.RecentSubMenu,
            this.toolStripSeparator1,
            this.saveSelectionToMacroToolStripMenuItem,
            this.placeMacroAtSelectionToolStripMenuItem,
            this.recentMacrosToolStripMenuItem,
            this.toolStripSeparator20,
            this.ToBk2MenuItem});
            this.FileSubMenu.Text = "&File";
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // NewTASMenuItem
            // 
            this.NewTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.NewTASMenuItem.Text = "&New";
            this.NewTASMenuItem.Click += new System.EventHandler(this.NewTasMenuItem_Click);
            // 
            // NewFromSubMenu
            // 
            this.NewFromSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewFromNowMenuItem,
            this.NewFromCurrentSaveRamMenuItem});
            this.NewFromSubMenu.Text = "New From";
            this.NewFromSubMenu.DropDownOpened += new System.EventHandler(this.NewFromSubMenu_DropDownOpened);
            // 
            // NewFromNowMenuItem
            // 
            this.NewFromNowMenuItem.Text = "&Now";
            this.NewFromNowMenuItem.Click += new System.EventHandler(this.StartNewProjectFromNowMenuItem_Click);
            // 
            // NewFromCurrentSaveRamMenuItem
            // 
            this.NewFromCurrentSaveRamMenuItem.Text = "&Current SaveRam";
            this.NewFromCurrentSaveRamMenuItem.Click += new System.EventHandler(this.StartANewProjectFromSaveRamMenuItem_Click);
            // 
            // OpenTASMenuItem
            // 
            this.OpenTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.OpenTASMenuItem.Text = "&Open";
            this.OpenTASMenuItem.Click += new System.EventHandler(this.OpenTasMenuItem_Click);
            // 
            // SaveTASMenuItem
            // 
            this.SaveTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.SaveTASMenuItem.Text = "&Save";
            this.SaveTASMenuItem.Click += new System.EventHandler(this.SaveTasMenuItem_Click);
            // 
            // SaveAsTASMenuItem
            // 
            this.SaveAsTASMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.SaveAsTASMenuItem.Text = "Save As";
            this.SaveAsTASMenuItem.Click += new System.EventHandler(this.SaveAsTasMenuItem_Click);
            // 
            // SaveBackupMenuItem
            // 
            this.SaveBackupMenuItem.Text = "Save Backup";
            this.SaveBackupMenuItem.Click += new System.EventHandler(this.SaveBackupMenuItem_Click);
            // 
            // SaveBk2BackupMenuItem
            // 
            this.SaveBk2BackupMenuItem.Text = "Save Bk2 Backup";
            this.SaveBk2BackupMenuItem.Click += new System.EventHandler(this.SaveBk2BackupMenuItem_Click);
            // 
            // RecentSubMenu
            // 
            this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
            this.RecentSubMenu.Text = "Recent";
            this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
            // 
            // saveSelectionToMacroToolStripMenuItem
            // 
            this.saveSelectionToMacroToolStripMenuItem.Text = "Save Selection to Macro";
            this.saveSelectionToMacroToolStripMenuItem.Click += new System.EventHandler(this.SaveSelectionToMacroMenuItem_Click);
            // 
            // placeMacroAtSelectionToolStripMenuItem
            // 
            this.placeMacroAtSelectionToolStripMenuItem.Text = "Place Macro at Selection";
            this.placeMacroAtSelectionToolStripMenuItem.Click += new System.EventHandler(this.PlaceMacroAtSelectionMenuItem_Click);
            // 
            // recentMacrosToolStripMenuItem
            // 
            this.recentMacrosToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator22});
            this.recentMacrosToolStripMenuItem.Text = "Recent Macros";
            this.recentMacrosToolStripMenuItem.DropDownOpened += new System.EventHandler(this.RecentMacrosMenuItem_DropDownOpened);
            // 
            // ToBk2MenuItem
            // 
            this.ToBk2MenuItem.Text = "&Export to Bk2";
            this.ToBk2MenuItem.Click += new System.EventHandler(this.ToBk2MenuItem_Click);
            // 
            // EditSubMenu
            // 
            this.EditSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UndoMenuItem,
            this.RedoMenuItem,
            this.showUndoHistoryToolStripMenuItem,
            this.SelectionUndoMenuItem,
            this.SelectionRedoMenuItem,
            this.toolStripSeparator5,
            this.DeselectMenuItem,
            this.SelectBetweenMarkersMenuItem,
            this.SelectAllMenuItem,
            this.ReselectClipboardMenuItem,
            this.toolStripSeparator7,
            this.CopyMenuItem,
            this.PasteMenuItem,
            this.PasteInsertMenuItem,
            this.CutMenuItem,
            this.toolStripSeparator8,
            this.ClearFramesMenuItem,
            this.DeleteFramesMenuItem,
            this.InsertFrameMenuItem,
            this.InsertNumFramesMenuItem,
            this.CloneFramesMenuItem,
            this.CloneFramesXTimesMenuItem,
            this.toolStripSeparator6,
            this.TruncateMenuItem,
            this.ClearGreenzoneMenuItem,
            this.GreenzoneICheckSeparator,
            this.StateHistoryIntegrityCheckMenuItem});
            this.EditSubMenu.Text = "&Edit";
            this.EditSubMenu.DropDownOpened += new System.EventHandler(this.EditSubMenu_DropDownOpened);
            // 
            // UndoMenuItem
            // 
            this.UndoMenuItem.Text = "&Undo";
            this.UndoMenuItem.Click += new System.EventHandler(this.UndoMenuItem_Click);
            // 
            // RedoMenuItem
            // 
            this.RedoMenuItem.Enabled = false;
            this.RedoMenuItem.Text = "&Redo";
            this.RedoMenuItem.Click += new System.EventHandler(this.RedoMenuItem_Click);
            // 
            // showUndoHistoryToolStripMenuItem
            // 
            this.showUndoHistoryToolStripMenuItem.Text = "Show Undo History";
            this.showUndoHistoryToolStripMenuItem.Click += new System.EventHandler(this.ShowUndoHistoryMenuItem_Click);
            // 
            // SelectionUndoMenuItem
            // 
            this.SelectionUndoMenuItem.Enabled = false;
            this.SelectionUndoMenuItem.Text = "Selection Undo";
            // 
            // SelectionRedoMenuItem
            // 
            this.SelectionRedoMenuItem.Enabled = false;
            this.SelectionRedoMenuItem.Text = "Selection Redo";
            // 
            // DeselectMenuItem
            // 
            this.DeselectMenuItem.Text = "Deselect";
            this.DeselectMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
            // 
            // SelectBetweenMarkersMenuItem
            // 
            this.SelectBetweenMarkersMenuItem.Text = "Select between Markers";
            this.SelectBetweenMarkersMenuItem.Click += new System.EventHandler(this.SelectBetweenMarkersMenuItem_Click);
            // 
            // SelectAllMenuItem
            // 
            this.SelectAllMenuItem.ShortcutKeyDisplayString = "";
            this.SelectAllMenuItem.Text = "Select &All";
            this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
            // 
            // ReselectClipboardMenuItem
            // 
            this.ReselectClipboardMenuItem.Text = "Reselect Clipboard";
            this.ReselectClipboardMenuItem.Click += new System.EventHandler(this.ReselectClipboardMenuItem_Click);
            // 
            // CopyMenuItem
            // 
            this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.CopyMenuItem.Text = "Copy";
            this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // PasteMenuItem
            // 
            this.PasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            this.PasteMenuItem.Text = "&Paste";
            this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // PasteInsertMenuItem
            // 
            this.PasteInsertMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.V)));
            this.PasteInsertMenuItem.Text = "&Paste Insert";
            this.PasteInsertMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
            // 
            // CutMenuItem
            // 
            this.CutMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.CutMenuItem.Text = "&Cut";
            this.CutMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
            // 
            // ClearFramesMenuItem
            // 
            this.ClearFramesMenuItem.ShortcutKeyDisplayString = "";
            this.ClearFramesMenuItem.Text = "Clear";
            this.ClearFramesMenuItem.Click += new System.EventHandler(this.ClearFramesMenuItem_Click);
            // 
            // DeleteFramesMenuItem
            // 
            this.DeleteFramesMenuItem.Text = "&Delete";
            this.DeleteFramesMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
            // 
            // InsertFrameMenuItem
            // 
            this.InsertFrameMenuItem.Text = "&Insert";
            this.InsertFrameMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
            // 
            // InsertNumFramesMenuItem
            // 
            this.InsertNumFramesMenuItem.ShortcutKeyDisplayString = "";
            this.InsertNumFramesMenuItem.Text = "Insert # of Frames";
            this.InsertNumFramesMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
            // 
            // CloneFramesMenuItem
            // 
            this.CloneFramesMenuItem.Text = "&Clone";
            this.CloneFramesMenuItem.Click += new System.EventHandler(this.CloneFramesMenuItem_Click);
            // 
            // CloneFramesXTimesMenuItem
            // 
            this.CloneFramesXTimesMenuItem.Text = "Clone # Times";
            this.CloneFramesXTimesMenuItem.Click += new System.EventHandler(this.CloneFramesXTimesMenuItem_Click);
            // 
            // TruncateMenuItem
            // 
            this.TruncateMenuItem.Text = "&Truncate Movie";
            this.TruncateMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
            // 
            // ClearGreenzoneMenuItem
            // 
            this.ClearGreenzoneMenuItem.Text = "&Clear Savestate History";
            this.ClearGreenzoneMenuItem.Click += new System.EventHandler(this.ClearGreenzoneMenuItem_Click);
            // 
            // StateHistoryIntegrityCheckMenuItem
            // 
            this.StateHistoryIntegrityCheckMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.I)));
            this.StateHistoryIntegrityCheckMenuItem.Text = "State History Integrity Check";
            this.StateHistoryIntegrityCheckMenuItem.Click += new System.EventHandler(this.StateHistoryIntegrityCheckMenuItem_Click);
            // 
            // ConfigSubMenu
            // 
            this.ConfigSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetMaxUndoLevelsMenuItem,
            this.CopyIncludesFrameNoMenuItem,
            this.toolStripSeparator26,
            this.autosaveToolStripMenuItem,
            this.BackupPerFileSaveMenuItem,
            this.toolStripSeparator9,
            this.AutoadjustInputMenuItem,
            this.applyPatternToPaintedInputToolStripMenuItem,
            this.onlyOnAutoFireColumnsToolStripMenuItem,
            this.SingleClickAxisEditMenuItem,
            this.UseInputKeysItem,
            this.toolStripSeparator4,
            this.BindMarkersToInputMenuItem,
            this.OldControlSchemeForBranchesMenuItem,
            this.LoadBranchOnDoubleclickMenuItem,
            this.OsdInBranchScreenshotsMenuItem,
            this.toolStripSeparator14,
            this.AutopauseAtEndOfMovieMenuItem,
            this.sepToolStripMenuItem,
            this.autoHoldFireToolStripMenuItem,
            this.SetFontMenuItem,
            this.disableTasViewToolStripMenuItem});
            this.ConfigSubMenu.Text = "&Config";
            this.ConfigSubMenu.DropDownOpened += new System.EventHandler(this.ConfigSubMenu_DropDownOpened);
            // 
            // SetMaxUndoLevelsMenuItem
            // 
            this.SetMaxUndoLevelsMenuItem.Text = "Set max Undo Levels";
            this.SetMaxUndoLevelsMenuItem.Click += new System.EventHandler(this.SetMaxUndoLevelsMenuItem_Click);
            // 
            // CopyIncludesFrameNoMenuItem
            // 
            this.CopyIncludesFrameNoMenuItem.Text = "Include Frame # When Copying Input";
            this.CopyIncludesFrameNoMenuItem.Click += new System.EventHandler(this.CopyIncludesFrameNoMenuItem_Click);
            // 
            // autosaveToolStripMenuItem
            // 
            this.autosaveToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetAutosaveIntervalMenuItem,
            this.AutosaveAsBk2MenuItem,
            this.AutosaveAsBackupFileMenuItem});
            this.autosaveToolStripMenuItem.Text = "Autosave";
            // 
            // SetAutosaveIntervalMenuItem
            // 
            this.SetAutosaveIntervalMenuItem.Text = "Set Autosave Interval";
            this.SetAutosaveIntervalMenuItem.Click += new System.EventHandler(this.SetAutosaveIntervalMenuItem_Click);
            // 
            // AutosaveAsBk2MenuItem
            // 
            this.AutosaveAsBk2MenuItem.Text = "Autosave As Bk2";
            this.AutosaveAsBk2MenuItem.Click += new System.EventHandler(this.AutosaveAsBk2MenuItem_Click);
            // 
            // AutosaveAsBackupFileMenuItem
            // 
            this.AutosaveAsBackupFileMenuItem.Text = "Autosave As Backup File";
            this.AutosaveAsBackupFileMenuItem.Click += new System.EventHandler(this.AutosaveAsBackupFileMenuItem_Click);
            // 
            // BackupPerFileSaveMenuItem
            // 
            this.BackupPerFileSaveMenuItem.Text = "Backup Per File Save";
            this.BackupPerFileSaveMenuItem.Click += new System.EventHandler(this.BackupPerFileSaveMenuItem_Click);
            // 
            // AutoadjustInputMenuItem
            // 
            this.AutoadjustInputMenuItem.CheckOnClick = true;
            this.AutoadjustInputMenuItem.Text = "Auto-adjust Input according to Lag";
            this.AutoadjustInputMenuItem.Click += new System.EventHandler(this.AutoadjustInputMenuItem_Click);
            // 
            // applyPatternToPaintedInputToolStripMenuItem
            // 
            this.applyPatternToPaintedInputToolStripMenuItem.CheckOnClick = true;
            this.applyPatternToPaintedInputToolStripMenuItem.Text = "Apply Pattern to painted input";
            this.applyPatternToPaintedInputToolStripMenuItem.CheckedChanged += new System.EventHandler(this.ApplyPatternToPaintedInputMenuItem_CheckedChanged);
            // 
            // onlyOnAutoFireColumnsToolStripMenuItem
            // 
            this.onlyOnAutoFireColumnsToolStripMenuItem.Checked = true;
            this.onlyOnAutoFireColumnsToolStripMenuItem.CheckOnClick = true;
            this.onlyOnAutoFireColumnsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.onlyOnAutoFireColumnsToolStripMenuItem.Enabled = false;
            this.onlyOnAutoFireColumnsToolStripMenuItem.Text = "Only on Auto-Fire columns";
            // 
            // SingleClickAxisEditMenuItem
            // 
            this.SingleClickAxisEditMenuItem.Enabled = false;
            this.SingleClickAxisEditMenuItem.Text = "Enter Axis Edit mode by single click";
            this.SingleClickAxisEditMenuItem.Visible = false;
            this.SingleClickAxisEditMenuItem.Click += new System.EventHandler(this.SingleClickAxisEditMenuItem_Click);
            // 
            // UseInputKeysItem
            // 
            this.UseInputKeysItem.Enabled = false;
            this.UseInputKeysItem.Text = "Use Input keys for Column Set";
            this.UseInputKeysItem.Visible = false;
            // 
            // BindMarkersToInputMenuItem
            // 
            this.BindMarkersToInputMenuItem.Checked = true;
            this.BindMarkersToInputMenuItem.CheckOnClick = true;
            this.BindMarkersToInputMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.BindMarkersToInputMenuItem.Text = "Bind Markers to Input";
            this.BindMarkersToInputMenuItem.Click += new System.EventHandler(this.BindMarkersToInputMenuItem_Click);
            // 
            // OldControlSchemeForBranchesMenuItem
            // 
            this.OldControlSchemeForBranchesMenuItem.Text = "Old control scheme for Branches";
            this.OldControlSchemeForBranchesMenuItem.Click += new System.EventHandler(this.OldControlSchemeForBranchesMenuItem_Click);
            // 
            // LoadBranchOnDoubleclickMenuItem
            // 
            this.LoadBranchOnDoubleclickMenuItem.Text = "Load Branch on double-click";
            this.LoadBranchOnDoubleclickMenuItem.Click += new System.EventHandler(this.LoadBranchOnDoubleClickMenuItem_Click);
            // 
            // OsdInBranchScreenshotsMenuItem
            // 
            this.OsdInBranchScreenshotsMenuItem.Enabled = false;
            this.OsdInBranchScreenshotsMenuItem.Text = "OSD in Branch screenshots";
            this.OsdInBranchScreenshotsMenuItem.Visible = false;
            // 
            // AutopauseAtEndOfMovieMenuItem
            // 
            this.AutopauseAtEndOfMovieMenuItem.Text = "Autopause at end of Movie";
            this.AutopauseAtEndOfMovieMenuItem.Click += new System.EventHandler(this.AutoPauseAtEndMenuItem_Click);
            // 
            // autoHoldFireToolStripMenuItem
            // 
            this.autoHoldFireToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.keepSetPatternsToolStripMenuItem,
            this.sepToolStripMenuItem1,
            this.autoHoldToolStripMenuItem,
            this.autoFireToolStripMenuItem,
            this.customPatternToolStripMenuItem,
            this.setpToolStripMenuItem,
            this.setCustomsToolStripMenuItem});
            this.autoHoldFireToolStripMenuItem.Text = "Auto Hold/Fire";
            // 
            // keepSetPatternsToolStripMenuItem
            // 
            this.keepSetPatternsToolStripMenuItem.CheckOnClick = true;
            this.keepSetPatternsToolStripMenuItem.Text = "Keep set patterns";
            // 
            // autoHoldToolStripMenuItem
            // 
            this.autoHoldToolStripMenuItem.Checked = true;
            this.autoHoldToolStripMenuItem.CheckOnClick = true;
            this.autoHoldToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoHoldToolStripMenuItem.Text = "Auto-Hold";
            this.autoHoldToolStripMenuItem.CheckedChanged += new System.EventHandler(this.AutoHoldMenuItem_CheckedChanged);
            // 
            // autoFireToolStripMenuItem
            // 
            this.autoFireToolStripMenuItem.CheckOnClick = true;
            this.autoFireToolStripMenuItem.Text = "Auto-Fire";
            this.autoFireToolStripMenuItem.CheckedChanged += new System.EventHandler(this.AutoFireMenuItem_CheckedChanged);
            // 
            // customPatternToolStripMenuItem
            // 
            this.customPatternToolStripMenuItem.CheckOnClick = true;
            this.customPatternToolStripMenuItem.Text = "Custom Pattern";
            this.customPatternToolStripMenuItem.CheckedChanged += new System.EventHandler(this.CustomPatternMenuItem_CheckedChanged);
            // 
            // setCustomsToolStripMenuItem
            // 
            this.setCustomsToolStripMenuItem.Text = "Set Customs...";
            this.setCustomsToolStripMenuItem.Click += new System.EventHandler(this.SetCustomsMenuItem_Click);
            // 
            // SetFontMenuItem
            // 
            this.SetFontMenuItem.Text = "Set Font";
            this.SetFontMenuItem.Click += new System.EventHandler(this.SetFontMenuItem_Click);
            // 
            // disableTasViewToolStripMenuItem
            // 
            this.disableTasViewToolStripMenuItem.Name = "disableTasViewToolStripMenuItem";
            this.disableTasViewToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.disableTasViewToolStripMenuItem.Text = "Toggle Disable TasView1";
            this.disableTasViewToolStripMenuItem.Click += new System.EventHandler(this.disableTasViewToolStripMenuItem_Click);
            // 
            // MetaSubMenu
            // 
            this.MetaSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HeaderMenuItem,
            this.StateHistorySettingsMenuItem,
            this.CommentsMenuItem,
            this.SubtitlesMenuItem,
            this.toolStripSeparator21,
            this.DefaultStateSettingsMenuItem});
            this.MetaSubMenu.Text = "&Metadata";
            // 
            // HeaderMenuItem
            // 
            this.HeaderMenuItem.Text = "&Header...";
            this.HeaderMenuItem.Click += new System.EventHandler(this.HeaderMenuItem_Click);
            // 
            // StateHistorySettingsMenuItem
            // 
            this.StateHistorySettingsMenuItem.Text = "&Savestate History Settings...";
            this.StateHistorySettingsMenuItem.Click += new System.EventHandler(this.StateHistorySettingsMenuItem_Click);
            // 
            // CommentsMenuItem
            // 
            this.CommentsMenuItem.Text = "&Comments...";
            this.CommentsMenuItem.Click += new System.EventHandler(this.CommentsMenuItem_Click);
            // 
            // SubtitlesMenuItem
            // 
            this.SubtitlesMenuItem.Text = "&Subtitles...";
            this.SubtitlesMenuItem.Click += new System.EventHandler(this.SubtitlesMenuItem_Click);
            // 
            // DefaultStateSettingsMenuItem
            // 
            this.DefaultStateSettingsMenuItem.Text = "&Default State History Settings...";
            this.DefaultStateSettingsMenuItem.Click += new System.EventHandler(this.DefaultStateSettingsMenuItem_Click);
            // 
            // SettingsSubMenu
            // 
            this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RotateMenuItem,
            this.HideLagFramesSubMenu,
            this.iconsToolStripMenuItem,
            this.ColorSettingsMenuItem,
            this.toolStripSeparator23,
            this.followCursorToolStripMenuItem,
            this.toolStripSeparator25,
            this.wheelScrollSpeedToolStripMenuItem});
            this.SettingsSubMenu.Text = "&Settings";
            this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.SettingsSubMenu_DropDownOpened);
            // 
            // RotateMenuItem
            // 
            this.RotateMenuItem.Text = "Rotate";
            this.RotateMenuItem.Click += new System.EventHandler(this.RotateMenuItem_Click);
            // 
            // HideLagFramesSubMenu
            // 
            this.HideLagFramesSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HideLagFrames0,
            this.HideLagFrames1,
            this.HideLagFrames2,
            this.HideLagFrames3,
            this.toolStripSeparator12,
            this.hideWasLagFramesToolStripMenuItem});
            this.HideLagFramesSubMenu.Text = "Hide Lag Frames";
            this.HideLagFramesSubMenu.DropDownOpened += new System.EventHandler(this.HideLagFramesSubMenu_DropDownOpened);
            // 
            // HideLagFrames0
            // 
            this.HideLagFrames0.Checked = true;
            this.HideLagFrames0.CheckOnClick = true;
            this.HideLagFrames0.CheckState = System.Windows.Forms.CheckState.Checked;
            this.HideLagFrames0.Tag = 0;
            this.HideLagFrames0.Text = "Don\'t Hide";
            this.HideLagFrames0.Click += new System.EventHandler(this.HideLagFramesX_Click);
            // 
            // HideLagFrames1
            // 
            this.HideLagFrames1.CheckOnClick = true;
            this.HideLagFrames1.Tag = 1;
            this.HideLagFrames1.Text = "1 (30 fps)";
            this.HideLagFrames1.Click += new System.EventHandler(this.HideLagFramesX_Click);
            // 
            // HideLagFrames2
            // 
            this.HideLagFrames2.Tag = 2;
            this.HideLagFrames2.Text = "2 (20 fps)";
            this.HideLagFrames2.Click += new System.EventHandler(this.HideLagFramesX_Click);
            // 
            // HideLagFrames3
            // 
            this.HideLagFrames3.CheckOnClick = true;
            this.HideLagFrames3.Tag = 3;
            this.HideLagFrames3.Text = "3 (15fps)";
            this.HideLagFrames3.Click += new System.EventHandler(this.HideLagFramesX_Click);
            // 
            // hideWasLagFramesToolStripMenuItem
            // 
            this.hideWasLagFramesToolStripMenuItem.CheckOnClick = true;
            this.hideWasLagFramesToolStripMenuItem.Text = "Hide WasLag Frames";
            this.hideWasLagFramesToolStripMenuItem.Click += new System.EventHandler(this.HideWasLagFramesMenuItem_Click);
            // 
            // iconsToolStripMenuItem
            // 
            this.iconsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DenoteStatesWithIconsToolStripMenuItem,
            this.DenoteStatesWithBGColorToolStripMenuItem,
            this.DenoteMarkersWithIconsToolStripMenuItem,
            this.DenoteMarkersWithBGColorToolStripMenuItem});
            this.iconsToolStripMenuItem.Text = "Icons";
            this.iconsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.IconsMenuItem_DropDownOpened);
            // 
            // DenoteStatesWithIconsToolStripMenuItem
            // 
            this.DenoteStatesWithIconsToolStripMenuItem.CheckOnClick = true;
            this.DenoteStatesWithIconsToolStripMenuItem.Text = "Denote States With Icons";
            this.DenoteStatesWithIconsToolStripMenuItem.Click += new System.EventHandler(this.DenoteStatesWithIconsToolStripMenuItem_Click);
            // 
            // DenoteStatesWithBGColorToolStripMenuItem
            // 
            this.DenoteStatesWithBGColorToolStripMenuItem.CheckOnClick = true;
            this.DenoteStatesWithBGColorToolStripMenuItem.Text = "Denote States With BG Color";
            this.DenoteStatesWithBGColorToolStripMenuItem.Click += new System.EventHandler(this.DenoteStatesWithBGColorToolStripMenuItem_Click);
            // 
            // DenoteMarkersWithIconsToolStripMenuItem
            // 
            this.DenoteMarkersWithIconsToolStripMenuItem.CheckOnClick = true;
            this.DenoteMarkersWithIconsToolStripMenuItem.Text = "Denote Markers With Icons";
            this.DenoteMarkersWithIconsToolStripMenuItem.Click += new System.EventHandler(this.DenoteMarkersWithIconsToolStripMenuItem_Click);
            // 
            // DenoteMarkersWithBGColorToolStripMenuItem
            // 
            this.DenoteMarkersWithBGColorToolStripMenuItem.CheckOnClick = true;
            this.DenoteMarkersWithBGColorToolStripMenuItem.Text = "Denote Markers With BG Color";
            this.DenoteMarkersWithBGColorToolStripMenuItem.Click += new System.EventHandler(this.DenoteMarkersWithBGColorToolStripMenuItem_Click);
            // 
            // ColorSettingsMenuItem
            // 
            this.ColorSettingsMenuItem.Text = "Edit TAStudio Colors...";
            this.ColorSettingsMenuItem.Click += new System.EventHandler(this.ColorSettingsMenuItem_Click);
            // 
            // followCursorToolStripMenuItem
            // 
            this.followCursorToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.alwaysScrollToolStripMenuItem,
            this.toolStripSeparator24,
            this.scrollToViewToolStripMenuItem,
            this.scrollToTopToolStripMenuItem,
            this.scrollToBottomToolStripMenuItem,
            this.scrollToCenterToolStripMenuItem});
            this.followCursorToolStripMenuItem.Text = "Follow Cursor";
            this.followCursorToolStripMenuItem.DropDownOpened += new System.EventHandler(this.FollowCursorMenuItem_DropDownOpened);
            // 
            // alwaysScrollToolStripMenuItem
            // 
            this.alwaysScrollToolStripMenuItem.CheckOnClick = true;
            this.alwaysScrollToolStripMenuItem.Text = "Always Scroll";
            this.alwaysScrollToolStripMenuItem.Click += new System.EventHandler(this.AlwaysScrollMenuItem_Click);
            // 
            // scrollToViewToolStripMenuItem
            // 
            this.scrollToViewToolStripMenuItem.Checked = true;
            this.scrollToViewToolStripMenuItem.CheckOnClick = true;
            this.scrollToViewToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.scrollToViewToolStripMenuItem.Text = "Scroll to View";
            this.scrollToViewToolStripMenuItem.Click += new System.EventHandler(this.ScrollToViewMenuItem_Click);
            // 
            // scrollToTopToolStripMenuItem
            // 
            this.scrollToTopToolStripMenuItem.CheckOnClick = true;
            this.scrollToTopToolStripMenuItem.Text = "Scroll to Top";
            this.scrollToTopToolStripMenuItem.Click += new System.EventHandler(this.ScrollToTopMenuItem_Click);
            // 
            // scrollToBottomToolStripMenuItem
            // 
            this.scrollToBottomToolStripMenuItem.CheckOnClick = true;
            this.scrollToBottomToolStripMenuItem.Text = "Scroll to Bottom";
            this.scrollToBottomToolStripMenuItem.Click += new System.EventHandler(this.ScrollToBottomMenuItem_Click);
            // 
            // scrollToCenterToolStripMenuItem
            // 
            this.scrollToCenterToolStripMenuItem.CheckOnClick = true;
            this.scrollToCenterToolStripMenuItem.Text = "Scroll to Center";
            this.scrollToCenterToolStripMenuItem.Click += new System.EventHandler(this.ScrollToCenterMenuItem_Click);
            // 
            // wheelScrollSpeedToolStripMenuItem
            // 
            this.wheelScrollSpeedToolStripMenuItem.Text = "Wheel Scroll Speed...";
            this.wheelScrollSpeedToolStripMenuItem.Click += new System.EventHandler(this.WheelScrollSpeedMenuItem_Click);
            // 
            // ColumnsSubMenu
            // 
            this.ColumnsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator19});
            this.ColumnsSubMenu.Text = "&Columns";
            // 
            // HelpSubMenu
            // 
            this.HelpSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TASEditorManualOnlineMenuItem,
            this.ForumThreadMenuItem,
            this.aboutToolStripMenuItem,
            this.toolStripSeparator10,
            this.EnableTooltipsMenuItem});
            this.HelpSubMenu.Text = "&Help";
            // 
            // TASEditorManualOnlineMenuItem
            // 
            this.TASEditorManualOnlineMenuItem.Text = "TAS Editor Manual Online...";
            this.TASEditorManualOnlineMenuItem.Click += new System.EventHandler(this.TASEditorManualOnlineMenuItem_Click);
            // 
            // ForumThreadMenuItem
            // 
            this.ForumThreadMenuItem.Text = "Forum Thread...";
            this.ForumThreadMenuItem.Click += new System.EventHandler(this.ForumThreadMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Enabled = false;
            this.aboutToolStripMenuItem.Text = "&About";
            // 
            // EnableTooltipsMenuItem
            // 
            this.EnableTooltipsMenuItem.Enabled = false;
            this.EnableTooltipsMenuItem.Text = "&Enable Tooltips";
            // 
            // TasView1
            // 
            this.TasView1.AllowColumnReorder = false;
            this.TasView1.AllowColumnResize = false;
            this.TasView1.AllowMassNavigationShortcuts = false;
            this.TasView1.AllowRightClickSelection = false;
            this.TasView1.AlwaysScroll = false;
            this.TasView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.TasView1.CellHeightPadding = 0;
            this.TasView1.ChangeSelectionWhenPaging = false;
            this.TasView1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TasView1.FullRowSelect = true;
            this.TasView1.HorizontalOrientation = false;
            this.TasView1.InputPaintingMode = true;
            this.TasView1.LetKeysModifySelection = true;
            this.TasView1.Location = new System.Drawing.Point(3, 20);
            this.TasView1.Name = "TasView1";
            this.TasView1.Rotatable = true;
            this.TasView1.RowCount = 0;
            this.TasView1.ScrollSpeed = 1;
            this.TasView1.Size = new System.Drawing.Size(250, 550);
            this.TasView1.TabIndex = 1;
            this.TasView1.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.TasView_ColumnClick);
            this.TasView1.ColumnRightClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.TasView_ColumnRightClick);
            this.TasView1.SelectedIndexChanged += new System.EventHandler(this.TasView_SelectedIndexChanged);
            this.TasView1.RightMouseScrolled += new BizHawk.Client.EmuHawk.InputRoll.RightMouseScrollEventHandler(this.TasView_MouseWheel);
            this.TasView1.ColumnReordered += new BizHawk.Client.EmuHawk.InputRoll.ColumnReorderedEventHandler(this.TasView_ColumnReordered);
            this.TasView1.CellDropped += new BizHawk.Client.EmuHawk.InputRoll.CellDroppedEvent(this.TasView_CellDropped);
            this.TasView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TasView_KeyDown);
            this.TasView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseDoubleClick);
            this.TasView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseDown);
            this.TasView1.MouseEnter += new System.EventHandler(this.TasView_MouseEnter);
            this.TasView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseMove);
            this.TasView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TasView_MouseUp);
            // 
            // TasStatusStrip
            // 
            this.TasStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MessageStatusLabel,
            this.ProgressBar,
            this.toolStripStatusLabel2,
            this.SplicerStatusLabel});
            this.TasStatusStrip.Location = new System.Drawing.Point(0, 616);
            this.TasStatusStrip.Name = "TasStatusStrip";
            this.TasStatusStrip.TabIndex = 4;
            // 
            // MessageStatusLabel
            // 
            this.MessageStatusLabel.Name = "MessageStatusLabel";
            this.MessageStatusLabel.Size = new System.Drawing.Size(103, 17);
            this.MessageStatusLabel.Text = "TAStudio engaged";
            // 
            // ProgressBar
            // 
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(821, 17);
            this.toolStripStatusLabel2.Spring = true;
            // 
            // SplicerStatusLabel
            // 
            this.SplicerStatusLabel.Name = "SplicerStatusLabel";
            this.SplicerStatusLabel.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.SplicerStatusLabel.Size = new System.Drawing.Size(20, 17);
            // 
            // TasPlaybackBoxMPR
            // 
            this.TasPlaybackBoxMPR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TasPlaybackBoxMPR.Location = new System.Drawing.Point(4, 4);
            this.TasPlaybackBoxMPR.Name = "TasPlaybackBoxMPR";
            this.TasPlaybackBoxMPR.Size = new System.Drawing.Size(200, 108);
            this.TasPlaybackBoxMPR.TabIndex = 5;
            this.TasPlaybackBoxMPR.TastudioMPR = null;
            // 
            // MarkerControlMPR
            // 
            this.MarkerControlMPR.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MarkerControlMPR.Location = new System.Drawing.Point(0, 0);
            this.MarkerControlMPR.Name = "MarkerControlMPR";
            this.MarkerControlMPR.Size = new System.Drawing.Size(200, 260);
            this.MarkerControlMPR.TabIndex = 6;
            this.MarkerControlMPR.TastudioMPR = null;
            // 
            // RightClickMenu
            // 
            this.RightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetMarkersContextMenuItem,
            this.SetMarkerWithTextContextMenuItem,
            this.RemoveMarkersContextMenuItem,
            this.toolStripSeparator15,
            this.DeselectContextMenuItem,
            this.SelectBetweenMarkersContextMenuItem,
            this.toolStripSeparator16,
            this.UngreenzoneContextMenuItem,
            this.CancelSeekContextMenuItem,
            this.toolStripSeparator17,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.pasteInsertToolStripMenuItem,
            this.cutToolStripMenuItem,
            this.pasteSelectedRowsFromToolStripMenuItem,
            this.separateToolStripMenuItem,
            this.ClearContextMenuItem,
            this.DeleteFramesContextMenuItem,
            this.InsertFrameContextMenuItem,
            this.InsertNumFramesContextMenuItem,
            this.CloneContextMenuItem,
            this.CloneXTimesContextMenuItem,
            this.toolStripSeparator18,
            this.TruncateContextMenuItem,
            this.BranchContextMenuItem,
            this.StartFromNowSeparator,
            this.StartNewProjectFromNowMenuItem,
            this.StartANewProjectFromSaveRamMenuItem});
            this.RightClickMenu.Name = "RightClickMenu";
            this.RightClickMenu.Size = new System.Drawing.Size(253, 546);
            this.RightClickMenu.Opened += new System.EventHandler(this.RightClickMenu_Opened);
            // 
            // SetMarkersContextMenuItem
            // 
            this.SetMarkersContextMenuItem.Text = "Set Markers";
            this.SetMarkersContextMenuItem.Click += new System.EventHandler(this.SetMarkersMenuItem_Click);
            // 
            // SetMarkerWithTextContextMenuItem
            // 
            this.SetMarkerWithTextContextMenuItem.Text = "Set Marker with Text";
            this.SetMarkerWithTextContextMenuItem.Click += new System.EventHandler(this.SetMarkerWithTextMenuItem_Click);
            // 
            // RemoveMarkersContextMenuItem
            // 
            this.RemoveMarkersContextMenuItem.Text = "Remove Markers";
            this.RemoveMarkersContextMenuItem.Click += new System.EventHandler(this.RemoveMarkersMenuItem_Click);
            // 
            // DeselectContextMenuItem
            // 
            this.DeselectContextMenuItem.Text = "Deselect";
            this.DeselectContextMenuItem.Click += new System.EventHandler(this.DeselectMenuItem_Click);
            // 
            // SelectBetweenMarkersContextMenuItem
            // 
            this.SelectBetweenMarkersContextMenuItem.Text = "Select between Markers";
            this.SelectBetweenMarkersContextMenuItem.Click += new System.EventHandler(this.SelectBetweenMarkersMenuItem_Click);
            // 
            // UngreenzoneContextMenuItem
            // 
            this.UngreenzoneContextMenuItem.Text = "Clear Greenzone";
            this.UngreenzoneContextMenuItem.Click += new System.EventHandler(this.ClearGreenzoneMenuItem_Click);
            // 
            // CancelSeekContextMenuItem
            // 
            this.CancelSeekContextMenuItem.Text = "Cancel Seek";
            this.CancelSeekContextMenuItem.Click += new System.EventHandler(this.CancelSeekContextMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
            // 
            // pasteInsertToolStripMenuItem
            // 
            this.pasteInsertToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+V";
            this.pasteInsertToolStripMenuItem.Text = "Paste Insert";
            this.pasteInsertToolStripMenuItem.Click += new System.EventHandler(this.PasteInsertMenuItem_Click);
            // 
            // cutToolStripMenuItem
            // 
            this.cutToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+X";
            this.cutToolStripMenuItem.Text = "Cut";
            this.cutToolStripMenuItem.Click += new System.EventHandler(this.CutMenuItem_Click);
            // 
            // ClearContextMenuItem
            // 
            this.ClearContextMenuItem.Text = "Clear";
            this.ClearContextMenuItem.Click += new System.EventHandler(this.ClearFramesMenuItem_Click);
            // 
            // DeleteFramesContextMenuItem
            // 
            this.DeleteFramesContextMenuItem.Text = "Delete";
            this.DeleteFramesContextMenuItem.Click += new System.EventHandler(this.DeleteFramesMenuItem_Click);
            // 
            // InsertFrameContextMenuItem
            // 
            this.InsertFrameContextMenuItem.Text = "Insert";
            this.InsertFrameContextMenuItem.Click += new System.EventHandler(this.InsertFrameMenuItem_Click);
            // 
            // InsertNumFramesContextMenuItem
            // 
            this.InsertNumFramesContextMenuItem.Text = "Insert # of Frames";
            this.InsertNumFramesContextMenuItem.Click += new System.EventHandler(this.InsertNumFramesMenuItem_Click);
            // 
            // CloneContextMenuItem
            // 
            this.CloneContextMenuItem.Text = "Clone";
            this.CloneContextMenuItem.Click += new System.EventHandler(this.CloneFramesMenuItem_Click);
            // 
            // CloneXTimesContextMenuItem
            // 
            this.CloneXTimesContextMenuItem.Text = "Clone # Times";
            this.CloneXTimesContextMenuItem.Click += new System.EventHandler(this.CloneFramesXTimesMenuItem_Click);
            // 
            // TruncateContextMenuItem
            // 
            this.TruncateContextMenuItem.Text = "Truncate Movie";
            this.TruncateContextMenuItem.Click += new System.EventHandler(this.TruncateMenuItem_Click);
            // 
            // BranchContextMenuItem
            // 
            this.BranchContextMenuItem.Text = "&Branch";
            this.BranchContextMenuItem.Click += new System.EventHandler(this.BranchContextMenuItem_Click);
            // 
            // StartNewProjectFromNowMenuItem
            // 
            this.StartNewProjectFromNowMenuItem.Text = "Start a new project from Now";
            this.StartNewProjectFromNowMenuItem.Click += new System.EventHandler(this.StartNewProjectFromNowMenuItem_Click);
            // 
            // StartANewProjectFromSaveRamMenuItem
            // 
            this.StartANewProjectFromSaveRamMenuItem.Text = "Start a new project from SaveRam";
            this.StartANewProjectFromSaveRamMenuItem.Click += new System.EventHandler(this.StartANewProjectFromSaveRamMenuItem_Click);
            // 
            // pasteSelectedRowsFromToolStripMenuItem
            // 
            this.pasteSelectedRowsFromToolStripMenuItem.Name = "pasteSelectedRowsFromToolStripMenuItem";
            this.pasteSelectedRowsFromToolStripMenuItem.Size = new System.Drawing.Size(252, 22);
            this.pasteSelectedRowsFromToolStripMenuItem.Text = "Paste Selected Rows From";
            // 
            // BookMarkControlMPR
            // 
            this.BookMarkControlMPR.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BookMarkControlMPR.LoadedCallback = null;
            this.BookMarkControlMPR.Location = new System.Drawing.Point(0, 0);
            this.BookMarkControlMPR.Name = "BookMarkControlMPR";
            this.BookMarkControlMPR.RemovedCallback = null;
            this.BookMarkControlMPR.SavedCallback = null;
            this.BookMarkControlMPR.Size = new System.Drawing.Size(200, 210);
            this.BookMarkControlMPR.TabIndex = 8;
            this.BookMarkControlMPR.TastudioMPR = null;
            // 
            // BranchesMarkersSplit
            // 
            this.BranchesMarkersSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BranchesMarkersSplit.Location = new System.Drawing.Point(4, 116);
            this.BranchesMarkersSplit.Name = "BranchesMarkersSplit";
            this.BranchesMarkersSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // BranchesMarkersSplit.Panel1
            // 
            this.BranchesMarkersSplit.Panel1.Controls.Add(this.BookMarkControlMPR);
            // 
            // BranchesMarkersSplit.Panel2
            // 
            this.BranchesMarkersSplit.Panel2.Controls.Add(this.MarkerControlMPR);
            this.BranchesMarkersSplit.Size = new System.Drawing.Size(200, 474);
            this.BranchesMarkersSplit.SplitterDistance = 210;
            this.BranchesMarkersSplit.TabIndex = 9;
            this.BranchesMarkersSplit.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.BranchesMarkersSplit_SplitterMoved);
            // 
            // MainVertialSplit
            // 
            this.MainVertialSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainVertialSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.MainVertialSplit.Location = new System.Drawing.Point(2, 23);
            this.MainVertialSplit.Name = "MainVertialSplit";
            // 
            // MainVertialSplit.Panel1
            // 
            this.MainVertialSplit.Panel1.AutoScroll = true;
            this.MainVertialSplit.Panel1.Controls.Add(this.TasView1);
            // 
            // MainVertialSplit.Panel2
            // 
            this.MainVertialSplit.Panel2.Controls.Add(this.TasPlaybackBoxMPR);
            this.MainVertialSplit.Panel2.Controls.Add(this.BranchesMarkersSplit);
            this.MainVertialSplit.Size = new System.Drawing.Size(1059, 590);
            this.MainVertialSplit.SplitterDistance = 847;
            this.MainVertialSplit.TabIndex = 10;
            this.MainVertialSplit.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.MainVerticalSplit_SplitterMoved);
            // 
            // TAStudioMPR
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1061, 638);
            this.Controls.Add(this.MainVertialSplit);
            this.Controls.Add(this.TasStatusStrip);
            this.Controls.Add(this.TASMenu);
            this.KeyPreview = true;
            this.MainMenuStrip = this.TASMenu;
            this.MinimumSize = new System.Drawing.Size(200, 148);
            this.Name = "TAStudioMPR";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Tastudio_Closing);
            this.Load += new System.EventHandler(this.Tastudio_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.TAStudio_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
            this.TASMenu.ResumeLayout(false);
            this.TASMenu.PerformLayout();
            this.TasStatusStrip.ResumeLayout(false);
            this.TasStatusStrip.PerformLayout();
            this.RightClickMenu.ResumeLayout(false);
            this.BranchesMarkersSplit.Panel1.ResumeLayout(false);
            this.BranchesMarkersSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BranchesMarkersSplit)).EndInit();
            this.BranchesMarkersSplit.ResumeLayout(false);
            this.MainVertialSplit.Panel1.ResumeLayout(false);
            this.MainVertialSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainVertialSplit)).EndInit();
            this.MainVertialSplit.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private MenuStripEx TASMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsTASMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ConfigSubMenu;
		private InputRoll TasView1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertFrameMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneFramesXTimesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeleteFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertNumFramesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TruncateMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteInsertMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CutMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UndoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RedoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectionUndoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectionRedoMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator5;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeselectMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectBetweenMarkersMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReselectClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator9;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HelpSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EnableTooltipsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator10;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx aboutToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetMaxUndoLevelsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutoadjustInputMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UseInputKeysItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BindMarkersToInputMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OldControlSchemeForBranchesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OsdInBranchScreenshotsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator14;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutopauseAtEndOfMovieMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private StatusStripEx TasStatusStrip;
		private System.Windows.Forms.ToolStripStatusLabel MessageStatusLabel;
		public PlaybackBoxMPR TasPlaybackBoxMPR;
		private System.Windows.Forms.ToolStripStatusLabel SplicerStatusLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MetaSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HeaderMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CommentsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SubtitlesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StateHistorySettingsMenuItem;
		private MarkerControlMPR MarkerControlMPR;
		private System.Windows.Forms.ContextMenuStrip RightClickMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetMarkersContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveMarkersContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator15;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeselectContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectBetweenMarkersContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator16;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UngreenzoneContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator17;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DeleteFramesContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertFrameContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertNumFramesContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloneXTimesContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator18;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TruncateContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearGreenzoneMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx GreenzoneICheckSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StateHistoryIntegrityCheckMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ColumnsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator19;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator21;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DefaultStateSettingsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CancelSeekContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx StartFromNowSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StartNewProjectFromNowMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RotateMenuItem;
		private System.Windows.Forms.ToolStripProgressBar ProgressBar;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HideLagFramesSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HideLagFrames3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HideLagFrames0;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HideLagFrames1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HideLagFrames2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx copyToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx pasteToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx separateToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx pasteInsertToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx cutToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx showUndoHistoryToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx sepToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx autoHoldFireToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx keepSetPatternsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx sepToolStripMenuItem1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx autoHoldToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx autoFireToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx customPatternToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx setpToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx setCustomsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator12;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx hideWasLagFramesToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveSelectionToMacroToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx placeMacroAtSelectionToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator20;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToBk2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx recentMacrosToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator22;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator23;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx followCursorToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx alwaysScrollToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator24;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx scrollToViewToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx scrollToTopToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx scrollToBottomToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx scrollToCenterToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx applyPatternToPaintedInputToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx onlyOnAutoFireColumnsToolStripMenuItem;
		private BookmarksBranchesBoxMPR BookMarkControlMPR;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BranchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator25;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx wheelScrollSpeedToolStripMenuItem;
		private System.Windows.Forms.SplitContainer BranchesMarkersSplit;
		private System.Windows.Forms.SplitContainer MainVertialSplit;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StartANewProjectFromSaveRamMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx iconsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ColorSettingsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DenoteStatesWithIconsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DenoteStatesWithBGColorToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DenoteMarkersWithIconsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DenoteMarkersWithBGColorToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewFromSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewFromNowMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewFromCurrentSaveRamMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetMarkerWithTextContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator26;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TASEditorManualOnlineMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ForumThreadMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx autosaveToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetAutosaveIntervalMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutosaveAsBk2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AutosaveAsBackupFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BackupPerFileSaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveBackupMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveBk2BackupMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SingleClickAxisEditMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx LoadBranchOnDoubleclickMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetFontMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyIncludesFrameNoMenuItem;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem disableTasViewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteSelectedRowsFromToolStripMenuItem;
	}
}