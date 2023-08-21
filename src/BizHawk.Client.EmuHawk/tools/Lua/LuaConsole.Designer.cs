using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class LuaConsole
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
			this.ScriptListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ToggleScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PauseScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RemoveScriptContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.InsertSeperatorContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ScriptContextSeparator = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.StopAllScriptsContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ClearRegisteredFunctionsContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.menuStrip1 = new MenuStripEx();
			this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.NewSessionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OpenSessionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveSessionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveSessionAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator9 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RecentSessionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RecentScriptsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ScriptSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.NewScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OpenScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RefreshScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ToggleScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PauseScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RemoveScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DuplicateScriptMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ClearConsoleMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.InsertSeparatorMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MoveUpMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MoveDownMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SelectAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.StopAllScriptsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RegisteredFunctionsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DisableScriptsOnLoadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ReturnAllIfNoneSelectedMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ReloadWhenScriptFileChangesMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RegisterToTextEditorsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RegisterSublimeText2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RegisterNotePadMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.GenerateLuaCatsDefinitionMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.HelpSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FunctionsListMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OnlineDocsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OutputBox = new System.Windows.Forms.RichTextBox();
			this.ConsoleContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ClearConsoleContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SelectAllContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CopyContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator5 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RegisteredFunctionsContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ClearRegisteredFunctionsLogContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.InputBox = new System.Windows.Forms.TextBox();
			this.NumberOfScripts = new BizHawk.WinForms.Controls.LocLabelEx();
			this.OutputMessages = new BizHawk.WinForms.Controls.LocLabelEx();
			this.toolStrip1 = new ToolStripEx();
			this.NewScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.OpenScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.ToggleScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.RefreshScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.PauseToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.EditToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.RemoveScriptToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.DuplicateToolbarButton = new System.Windows.Forms.ToolStripButton();
			this.ClearConsoleToolbarButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.MoveUpToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
			this.InsertSeparatorToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator10 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.EraseToolbarItem = new System.Windows.Forms.ToolStripButton();
			this.LuaListView = new BizHawk.Client.EmuHawk.InputRoll();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.ScriptListContextMenu.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.ConsoleContextMenu.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// ScriptListContextMenu
			// 
			this.ScriptListContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToggleScriptContextItem,
            this.PauseScriptContextItem,
            this.EditScriptContextItem,
            this.RemoveScriptContextItem,
            this.InsertSeperatorContextItem,
            this.ScriptContextSeparator,
            this.StopAllScriptsContextItem,
            this.ClearRegisteredFunctionsContextItem});
			this.ScriptListContextMenu.Name = "contextMenuStrip1";
			this.ScriptListContextMenu.Size = new System.Drawing.Size(204, 164);
			this.ScriptListContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ScriptListContextMenu_Opening);
			// 
			// ToggleScriptContextItem
			// 
			this.ToggleScriptContextItem.Text = "&Toggle";
			this.ToggleScriptContextItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
			// 
			// PauseScriptContextItem
			// 
			this.PauseScriptContextItem.Text = "Pause or Resume";
			this.PauseScriptContextItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
			// 
			// EditScriptContextItem
			// 
			this.EditScriptContextItem.Text = "&Edit";
			this.EditScriptContextItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
			// 
			// RemoveScriptContextItem
			// 
			this.RemoveScriptContextItem.Text = "&Remove";
			this.RemoveScriptContextItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
			// 
			// InsertSeperatorContextItem
			// 
			this.InsertSeperatorContextItem.Text = "Insert Seperator";
			this.InsertSeperatorContextItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// StopAllScriptsContextItem
			// 
			this.StopAllScriptsContextItem.Text = "Stop All Scripts";
			this.StopAllScriptsContextItem.Click += new System.EventHandler(this.StopAllScriptsMenuItem_Click);
			// 
			// ClearRegisteredFunctionsContextItem
			// 
			this.ClearRegisteredFunctionsContextItem.Text = "Clear Registered Functions";
			this.ClearRegisteredFunctionsContextItem.Click += new System.EventHandler(this.ClearRegisteredFunctionsContextMenuItem_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.ScriptSubMenu,
            this.SettingsSubMenu,
            this.HelpSubMenu});
			this.menuStrip1.TabIndex = 1;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewSessionMenuItem,
            this.OpenSessionMenuItem,
            this.SaveSessionMenuItem,
            this.SaveSessionAsMenuItem,
            this.toolStripSeparator9,
            this.RecentSessionsSubMenu,
            this.RecentScriptsSubMenu});
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewSessionMenuItem
			// 
			this.NewSessionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
			this.NewSessionMenuItem.Text = "&New Session";
			this.NewSessionMenuItem.Click += new System.EventHandler(this.NewSessionMenuItem_Click);
			// 
			// OpenSessionMenuItem
			// 
			this.OpenSessionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
			this.OpenSessionMenuItem.Text = "&Open Session...";
			this.OpenSessionMenuItem.Click += new System.EventHandler(this.OpenSessionMenuItem_Click);
			// 
			// SaveSessionMenuItem
			// 
			this.SaveSessionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveSessionMenuItem.Text = "&Save Session";
			this.SaveSessionMenuItem.Click += new System.EventHandler(this.SaveSessionMenuItem_Click);
			// 
			// SaveSessionAsMenuItem
			// 
			this.SaveSessionAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveSessionAsMenuItem.Text = "Save Session &As...";
			this.SaveSessionAsMenuItem.Click += new System.EventHandler(this.SaveSessionAsMenuItem_Click);
			// 
			// RecentSessionsSubMenu
			// 
			this.RecentSessionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator8});
			this.RecentSessionsSubMenu.Text = "Recent Sessions";
			this.RecentSessionsSubMenu.DropDownOpened += new System.EventHandler(this.RecentSessionsSubMenu_DropDownOpened);
			// 
			// RecentScriptsSubMenu
			// 
			this.RecentScriptsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.RecentScriptsSubMenu.Text = "Recent Scripts";
			this.RecentScriptsSubMenu.DropDownOpened += new System.EventHandler(this.RecentScriptsSubMenu_DropDownOpened);
			// 
			// ScriptSubMenu
			// 
			this.ScriptSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewScriptMenuItem,
            this.OpenScriptMenuItem,
            this.RefreshScriptMenuItem,
            this.ToggleScriptMenuItem,
            this.PauseScriptMenuItem,
            this.EditScriptMenuItem,
            this.RemoveScriptMenuItem,
            this.DuplicateScriptMenuItem,
            this.ClearConsoleMenuItem,
            this.toolStripSeparator7,
            this.InsertSeparatorMenuItem,
            this.MoveUpMenuItem,
            this.MoveDownMenuItem,
            this.SelectAllMenuItem,
            this.toolStripSeparator6,
            this.StopAllScriptsMenuItem,
            this.RegisteredFunctionsMenuItem});
			this.ScriptSubMenu.Text = "&Script";
			this.ScriptSubMenu.DropDownOpened += new System.EventHandler(this.ScriptSubMenu_DropDownOpened);
			// 
			// NewScriptMenuItem
			// 
			this.NewScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewScriptMenuItem.Text = "New Script";
			this.NewScriptMenuItem.Click += new System.EventHandler(this.NewScriptMenuItem_Click);
			// 
			// OpenScriptMenuItem
			// 
			this.OpenScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenScriptMenuItem.Text = "&Open Script...";
			this.OpenScriptMenuItem.Click += new System.EventHandler(this.OpenScriptMenuItem_Click);
			// 
			// RefreshScriptMenuItem
			// 
			this.RefreshScriptMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.RefreshScriptMenuItem.Text = "&Re&fresh";
			this.RefreshScriptMenuItem.Click += new System.EventHandler(this.RefreshScriptMenuItem_Click);
			// 
			// ToggleScriptMenuItem
			// 
			this.ToggleScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
			this.ToggleScriptMenuItem.Text = "&Toggle";
			this.ToggleScriptMenuItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
			// 
			// PauseScriptMenuItem
			// 
			this.PauseScriptMenuItem.Text = "Pause or Resume";
			this.PauseScriptMenuItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
			// 
			// EditScriptMenuItem
			// 
			this.EditScriptMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.EditScriptMenuItem.Text = "&Edit Script";
			this.EditScriptMenuItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
			// 
			// RemoveScriptMenuItem
			// 
			this.RemoveScriptMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.RemoveScriptMenuItem.Text = "&Remove Script";
			this.RemoveScriptMenuItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
			// 
			// DuplicateScriptMenuItem
			// 
			this.DuplicateScriptMenuItem.Text = "&Duplicate Script";
			this.DuplicateScriptMenuItem.Click += new System.EventHandler(this.DuplicateScriptMenuItem_Click);
			// 
			// ClearConsoleMenuItem
			// 
			this.ClearConsoleMenuItem.Text = "&Clear Output";
			this.ClearConsoleMenuItem.Click += new System.EventHandler(this.ClearConsoleMenuItem_Click);
			// 
			// InsertSeparatorMenuItem
			// 
			this.InsertSeparatorMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.InsertSeparatorMenuItem.Text = "Insert Separator";
			this.InsertSeparatorMenuItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// MoveUpMenuItem
			// 
			this.MoveUpMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
			this.MoveUpMenuItem.Text = "Move &Up";
			this.MoveUpMenuItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// MoveDownMenuItem
			// 
			this.MoveDownMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
			this.MoveDownMenuItem.Text = "Move &Down";
			this.MoveDownMenuItem.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// SelectAllMenuItem
			// 
			this.SelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
			this.SelectAllMenuItem.Text = "Select &All";
			this.SelectAllMenuItem.Click += new System.EventHandler(this.SelectAllMenuItem_Click);
			// 
			// StopAllScriptsMenuItem
			// 
			this.StopAllScriptsMenuItem.Text = "Stop All Scripts";
			this.StopAllScriptsMenuItem.Click += new System.EventHandler(this.StopAllScriptsMenuItem_Click);
			// 
			// RegisteredFunctionsMenuItem
			// 
			this.RegisteredFunctionsMenuItem.ShortcutKeyDisplayString = "F12";
			this.RegisteredFunctionsMenuItem.Text = "&Registered Functions...";
			this.RegisteredFunctionsMenuItem.Click += new System.EventHandler(this.RegisteredFunctionsMenuItem_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DisableScriptsOnLoadMenuItem,
            this.ReturnAllIfNoneSelectedMenuItem,
            this.ReloadWhenScriptFileChangesMenuItem,
            this.toolStripSeparator4,
            this.RegisterToTextEditorsSubMenu});
			this.SettingsSubMenu.Text = "&Settings";
			this.SettingsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// DisableScriptsOnLoadMenuItem
			// 
			this.DisableScriptsOnLoadMenuItem.Text = "Disable Scripts on Load";
			this.DisableScriptsOnLoadMenuItem.Click += new System.EventHandler(this.DisableScriptsOnLoadMenuItem_Click);
			// 
			// ReturnAllIfNoneSelectedMenuItem
			// 
			this.ReturnAllIfNoneSelectedMenuItem.Text = "Toggle All if None Selected";
			this.ReturnAllIfNoneSelectedMenuItem.Click += new System.EventHandler(this.ToggleAllIfNoneSelectedMenuItem_Click);
			// 
			// ReloadWhenScriptFileChangesMenuItem
			// 
			this.ReloadWhenScriptFileChangesMenuItem.Text = "Reload When Script File Changes";
			this.ReloadWhenScriptFileChangesMenuItem.Click += new System.EventHandler(this.ReloadWhenScriptFileChangesMenuItem_Click);
			// 
			// RegisterToTextEditorsSubMenu
			// 
			this.RegisterToTextEditorsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RegisterSublimeText2MenuItem,
            this.RegisterNotePadMenuItem,
            this.GenerateLuaCatsDefinitionMenuItem});
			this.RegisterToTextEditorsSubMenu.Text = "Register To Text Editors";
			this.RegisterToTextEditorsSubMenu.DropDownOpened += new System.EventHandler(this.RegisterToTextEditorsSubMenu_DropDownOpened);
			// 
			// RegisterSublimeText2MenuItem
			// 
			this.RegisterSublimeText2MenuItem.Text = "&Sublime Text 2";
			this.RegisterSublimeText2MenuItem.Click += new System.EventHandler(this.RegisterSublimeText2MenuItem_Click);
			// 
			// RegisterNotePadMenuItem
			// 
			this.RegisterNotePadMenuItem.Text = "Notepad++";
			this.RegisterNotePadMenuItem.Click += new System.EventHandler(this.RegisterNotePadMenuItem_Click);
			// 
			// GenerateLuaCatsDefinitionMenuItem
			// 
			this.GenerateLuaCatsDefinitionMenuItem.Text = "LuaCATS (Lua Language Server)";
			this.GenerateLuaCatsDefinitionMenuItem.Click += new System.EventHandler(this.GenerateLuaCatsDefinitionMenuItem_Click);
			// 
			// HelpSubMenu
			// 
			this.HelpSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FunctionsListMenuItem,
            this.OnlineDocsMenuItem});
			this.HelpSubMenu.Text = "&Help";
			// 
			// FunctionsListMenuItem
			// 
			this.FunctionsListMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.FunctionsListMenuItem.Text = "&Lua Functions List";
			this.FunctionsListMenuItem.Click += new System.EventHandler(this.FunctionsListMenuItem_Click);
			// 
			// OnlineDocsMenuItem
			// 
			this.OnlineDocsMenuItem.Text = "Documentation online...";
			this.OnlineDocsMenuItem.Click += new System.EventHandler(this.OnlineDocsMenuItem_Click);
			// 
			// OutputBox
			// 
			this.OutputBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.OutputBox.ContextMenuStrip = this.ConsoleContextMenu;
			this.OutputBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.OutputBox.HideSelection = false;
			this.OutputBox.Location = new System.Drawing.Point(6, 17);
			this.OutputBox.Name = "OutputBox";
			this.OutputBox.ReadOnly = true;
			this.OutputBox.Size = new System.Drawing.Size(288, 249);
			this.OutputBox.TabIndex = 2;
			this.OutputBox.Text = "";
			this.OutputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OutputBox_KeyDown);
			// 
			// ConsoleContextMenu
			// 
			this.ConsoleContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.CopyContextItem,
			this.SelectAllContextItem,
			this.ClearConsoleContextItem,
			this.toolStripSeparator5,
			this.RegisteredFunctionsContextItem,
			this.ClearRegisteredFunctionsLogContextItem});
			this.ConsoleContextMenu.Name = "contextMenuStrip2";
			this.ConsoleContextMenu.Size = new System.Drawing.Size(204, 142);
			this.ConsoleContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ConsoleContextMenu_Opening);
			// 
			// ClearConsoleContextItem
			// 
			this.ClearConsoleContextItem.Text = "&Clear";
			this.ClearConsoleContextItem.Click += new System.EventHandler(this.ClearConsoleContextItem_Click);
			// 
			// SelectAllContextItem
			// 
			this.SelectAllContextItem.Text = "Select &All";
			this.SelectAllContextItem.Click += new System.EventHandler(this.SelectAllContextItem_Click);
			// 
			// CopyContextItem
			// 
			this.CopyContextItem.Text = "Copy";
			this.CopyContextItem.Click += new System.EventHandler(this.CopyContextItem_Click);
			// 
			// RegisteredFunctionsContextItem
			// 
			this.RegisteredFunctionsContextItem.Text = "&Registered Functions";
			this.RegisteredFunctionsContextItem.Click += new System.EventHandler(this.RegisteredFunctionsMenuItem_Click);
			// 
			// ClearRegisteredFunctionsLogContextItem
			// 
			this.ClearRegisteredFunctionsLogContextItem.Text = "Clear Registered Functions";
			this.ClearRegisteredFunctionsLogContextItem.Click += new System.EventHandler(this.ClearRegisteredFunctionsContextMenuItem_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.InputBox);
			this.groupBox1.Controls.Add(this.OutputBox);
			this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(300, 298);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Output";
			// 
			// InputBox
			// 
			this.InputBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.InputBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.InputBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this.InputBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.InputBox.Location = new System.Drawing.Point(6, 272);
			this.InputBox.Name = "InputBox";
			this.InputBox.Size = new System.Drawing.Size(288, 20);
			this.InputBox.TabIndex = 3;
			this.InputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputBox_KeyDown);
			// 
			// NumberOfScripts
			// 
			this.NumberOfScripts.Location = new System.Drawing.Point(3, 3);
			this.NumberOfScripts.Name = "NumberOfScripts";
			this.NumberOfScripts.Text = "0 script     ";
			// 
			// OutputMessages
			// 
			this.OutputMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OutputMessages.Location = new System.Drawing.Point(13, 329);
			this.OutputMessages.Name = "OutputMessages";
			this.OutputMessages.Text = "                                 ";
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewScriptToolbarItem,
            this.OpenScriptToolbarItem,
            this.ToggleScriptToolbarItem,
            this.RefreshScriptToolbarItem,
            this.PauseToolbarItem,
            this.EditToolbarItem,
            this.RemoveScriptToolbarItem,
            this.DuplicateToolbarButton,
            this.ClearConsoleToolbarButton,
            this.toolStripSeparator2,
            this.MoveUpToolbarItem,
            this.toolStripButtonMoveDown,
            this.InsertSeparatorToolbarItem,
            this.toolStripSeparator10,
            this.EraseToolbarItem});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.TabIndex = 5;
			// 
			// NewScriptToolbarItem
			// 
			this.NewScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.NewScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.NewScriptToolbarItem.Name = "NewScriptToolbarItem";
			this.NewScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.NewScriptToolbarItem.Text = "New Lua Script";
			this.NewScriptToolbarItem.Click += new System.EventHandler(this.NewScriptMenuItem_Click);
			// 
			// OpenScriptToolbarItem
			// 
			this.OpenScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.OpenScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OpenScriptToolbarItem.Name = "OpenScriptToolbarItem";
			this.OpenScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.OpenScriptToolbarItem.Text = "Open Script";
			this.OpenScriptToolbarItem.Click += new System.EventHandler(this.OpenScriptMenuItem_Click);
			// 
			// ToggleScriptToolbarItem
			// 
			this.ToggleScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ToggleScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ToggleScriptToolbarItem.Name = "ToggleScriptToolbarItem";
			this.ToggleScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.ToggleScriptToolbarItem.Text = "Toggle Script";
			this.ToggleScriptToolbarItem.Click += new System.EventHandler(this.ToggleScriptMenuItem_Click);
			// 
			// RefreshScriptToolbarItem
			// 
			this.RefreshScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RefreshScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshScriptToolbarItem.Name = "RefreshScriptToolbarItem";
			this.RefreshScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.RefreshScriptToolbarItem.Text = "Refresh";
			this.RefreshScriptToolbarItem.Click += new System.EventHandler(this.RefreshScriptMenuItem_Click);
			// 
			// PauseToolbarItem
			// 
			this.PauseToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PauseToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PauseToolbarItem.Name = "PauseToolbarItem";
			this.PauseToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.PauseToolbarItem.Text = "Pause or Resume";
			this.PauseToolbarItem.Click += new System.EventHandler(this.PauseScriptMenuItem_Click);
			// 
			// EditToolbarItem
			// 
			this.EditToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.EditToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.EditToolbarItem.Name = "EditToolbarItem";
			this.EditToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.EditToolbarItem.Text = "Edit Script";
			this.EditToolbarItem.Click += new System.EventHandler(this.EditScriptMenuItem_Click);
			// 
			// RemoveScriptToolbarItem
			// 
			this.RemoveScriptToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.RemoveScriptToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveScriptToolbarItem.Name = "RemoveScriptToolbarItem";
			this.RemoveScriptToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.RemoveScriptToolbarItem.Text = "Remove Script";
			this.RemoveScriptToolbarItem.Click += new System.EventHandler(this.RemoveScriptMenuItem_Click);
			// 
			// DuplicateToolbarButton
			// 
			this.DuplicateToolbarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.DuplicateToolbarButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DuplicateToolbarButton.Name = "DuplicateToolbarButton";
			this.DuplicateToolbarButton.Size = new System.Drawing.Size(23, 22);
			this.DuplicateToolbarButton.Text = "Duplicate Script";
			this.DuplicateToolbarButton.Click += new System.EventHandler(this.DuplicateScriptMenuItem_Click);
			// 
			// ClearConsoleToolbarButton
			// 
			this.ClearConsoleToolbarButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.ClearConsoleToolbarButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ClearConsoleToolbarButton.Name = "ClearConsoleToolbarButton";
			this.ClearConsoleToolbarButton.Size = new System.Drawing.Size(23, 22);
			this.ClearConsoleToolbarButton.Text = "Clear Output";
			this.ClearConsoleToolbarButton.Click += new System.EventHandler(this.ClearConsoleMenuItem_Click);
			// 
			// MoveUpToolbarItem
			// 
			this.MoveUpToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.MoveUpToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MoveUpToolbarItem.Name = "MoveUpToolbarItem";
			this.MoveUpToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.MoveUpToolbarItem.Text = "Move Up";
			this.MoveUpToolbarItem.Click += new System.EventHandler(this.MoveUpMenuItem_Click);
			// 
			// toolStripButtonMoveDown
			// 
			this.toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonMoveDown.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonMoveDown.Name = "toolStripButtonMoveDown";
			this.toolStripButtonMoveDown.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonMoveDown.Text = "Move Down";
			this.toolStripButtonMoveDown.Click += new System.EventHandler(this.MoveDownMenuItem_Click);
			// 
			// InsertSeparatorToolbarItem
			// 
			this.InsertSeparatorToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.InsertSeparatorToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.InsertSeparatorToolbarItem.Name = "InsertSeparatorToolbarItem";
			this.InsertSeparatorToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.InsertSeparatorToolbarItem.Text = "Insert Separator";
			this.InsertSeparatorToolbarItem.Click += new System.EventHandler(this.InsertSeparatorMenuItem_Click);
			// 
			// EraseToolbarItem
			// 
			this.EraseToolbarItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.EraseToolbarItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.EraseToolbarItem.Name = "EraseToolbarItem";
			this.EraseToolbarItem.Size = new System.Drawing.Size(23, 22);
			this.EraseToolbarItem.Text = "Erase Stale/Stuck Lua Drawing Layers";
			this.EraseToolbarItem.Click += new System.EventHandler(this.EraseToolbarItem_Click);
			// 
			// LuaListView
			// 
			this.LuaListView.AllowColumnReorder = false;
			this.LuaListView.AllowColumnResize = true;
			this.LuaListView.AllowMassNavigationShortcuts = true;
			this.LuaListView.AllowRightClickSelection = true;
			this.LuaListView.AlwaysScroll = false;
			this.LuaListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.LuaListView.CellHeightPadding = 0;
			this.LuaListView.CellWidthPadding = 0;
			this.LuaListView.ContextMenuStrip = this.ScriptListContextMenu;
			this.LuaListView.FullRowSelect = true;
			this.LuaListView.HorizontalOrientation = false;
			this.LuaListView.LetKeysModifySelection = false;
			this.LuaListView.Location = new System.Drawing.Point(4, 21);
			this.LuaListView.Name = "LuaListView";
			this.LuaListView.RowCount = 0;
			this.LuaListView.ScrollSpeed = 1;
			this.LuaListView.SeekingCutoffInterval = 0;
			this.LuaListView.Size = new System.Drawing.Size(273, 271);
			this.LuaListView.TabIndex = 0;
			this.LuaListView.ColumnClick += new BizHawk.Client.EmuHawk.InputRoll.ColumnClickEventHandler(this.LuaListView_ColumnClick);
			this.LuaListView.DoubleClick += new System.EventHandler(this.LuaListView_DoubleClick);
			this.LuaListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LuaListView_KeyDown);
			this.LuaListView.MultiSelect = true;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 49);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.BranchesMarkersSplit_SplitterMoved);
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.NumberOfScripts);
			this.splitContainer1.Panel1.Controls.Add(this.LuaListView);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
			this.splitContainer1.Size = new System.Drawing.Size(584, 298);
			this.splitContainer1.SplitterDistance = 280;
			this.splitContainer1.TabIndex = 7;
			// 
			// LuaConsole
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 347);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.OutputMessages);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(400, 180);
			this.Name = "LuaConsole";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Load += new System.EventHandler(this.LuaConsole_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.LuaConsole_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEnterWrapper);
			this.ScriptListContextMenu.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ConsoleContextMenu.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private InputRoll LuaListView;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveSessionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveSessionAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScriptSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToggleScriptMenuItem;
		private System.Windows.Forms.GroupBox groupBox1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewSessionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private BizHawk.WinForms.Controls.LocLabelEx NumberOfScripts;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeparatorMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StopAllScriptsMenuItem;
		private System.Windows.Forms.ContextMenuStrip ScriptListContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentScriptsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx StopAllScriptsContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveScriptContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx InsertSeperatorContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ScriptContextSeparator;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditScriptContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ToggleScriptContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveUpMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MoveDownMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllMenuItem;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.ToolStripButton OpenScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton RemoveScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton ToggleScriptToolbarItem;
		private System.Windows.Forms.ToolStripButton InsertSeparatorToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton MoveUpToolbarItem;
		private System.Windows.Forms.ToolStripButton toolStripButtonMoveDown;
		private System.Windows.Forms.ToolStripButton EditToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenSessionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator9;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSessionsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx HelpSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FunctionsListMenuItem;
		private System.Windows.Forms.ContextMenuStrip ConsoleContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearConsoleContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DisableScriptsOnLoadMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PauseScriptMenuItem;
		private System.Windows.Forms.ToolStripButton PauseToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PauseScriptContextItem;
		public System.Windows.Forms.RichTextBox OutputBox;
		private BizHawk.WinForms.Controls.LocLabelEx OutputMessages;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OnlineDocsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewScriptMenuItem;
		private System.Windows.Forms.ToolStripButton NewScriptToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisteredFunctionsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisteredFunctionsContextItem;
		private System.Windows.Forms.ToolStripButton RefreshScriptToolbarItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RefreshScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator10;
		private System.Windows.Forms.ToolStripButton EraseToolbarItem;
		private System.Windows.Forms.ToolStripButton DuplicateToolbarButton;
		private System.Windows.Forms.ToolStripButton ClearConsoleToolbarButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DuplicateScriptMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearConsoleMenuItem;
		private System.Windows.Forms.TextBox InputBox;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReturnAllIfNoneSelectedMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ReloadWhenScriptFileChangesMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisterToTextEditorsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisterSublimeText2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RegisterNotePadMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GenerateLuaCatsDefinitionMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SelectAllContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearRegisteredFunctionsContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator5;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearRegisteredFunctionsLogContextItem;
	}
}