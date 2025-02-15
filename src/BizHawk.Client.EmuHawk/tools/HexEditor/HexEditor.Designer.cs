using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class HexEditor
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
			this.HexMenuStrip = new MenuStripEx();
			this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveAsBinaryMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveAsTextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.importAsBinaryToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.LoadTableFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CloseTableFileMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RecentTablesSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.noneToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CopyMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ExportMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PasteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator6 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.FindMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FindNextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FindPrevMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.OptionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MemoryDomainsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.DataSizeSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DataSizeByteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DataSizeWordMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DataSizeDWordMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.BigEndianMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.GoToAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AddToRamWatchMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FreezeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.UnfreezeAllMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PokeAddressMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SettingsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.CustomColorsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SetColorsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator8 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.ResetColorsToDefaultMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator7 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.resetToDefaultToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ViewerContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.CopyContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ExportContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PasteContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.FreezeContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AddToRamWatchContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.UnfreezeAllContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.PokeContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ContextSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.IncrementContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.DecrementContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ContextSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.GoToContextItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripMenuItem1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.viewN64MatrixToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.MemoryViewerBox = new System.Windows.Forms.GroupBox();
			this.HexScrollBar = new System.Windows.Forms.VScrollBar();
			this.AddressLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.AddressesLabel = new BizHawk.WinForms.Controls.HexLabelEx();
			this.Header = new BizHawk.WinForms.Controls.LocLabelEx();
			this.HexMenuStrip.SuspendLayout();
			this.ViewerContextMenuStrip.SuspendLayout();
			this.MemoryViewerBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// HexMenuStrip
			// 
			this.HexMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.EditMenuItem,
            this.OptionsSubMenu,
            this.SettingsSubMenu});
			this.HexMenuStrip.TabIndex = 1;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveMenuItem,
            this.SaveAsBinaryMenuItem,
            this.SaveAsTextMenuItem,
            this.importAsBinaryToolStripMenuItem,
            this.toolStripSeparator4,
            this.LoadTableFileMenuItem,
            this.CloseTableFileMenuItem,
            this.RecentTablesSubMenu});
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Text = "Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsBinaryMenuItem
			// 
			this.SaveAsBinaryMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
			this.SaveAsBinaryMenuItem.Text = "Save as binary...";
			this.SaveAsBinaryMenuItem.Click += new System.EventHandler(this.SaveAsBinaryMenuItem_Click);
			// 
			// SaveAsTextMenuItem
			// 
			this.SaveAsTextMenuItem.Text = "Save as text...";
			this.SaveAsTextMenuItem.Click += new System.EventHandler(this.SaveAsTextMenuItem_Click);
			// 
			// importAsBinaryToolStripMenuItem
			// 
			this.importAsBinaryToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.importAsBinaryToolStripMenuItem.Text = "Import as binary...";
			this.importAsBinaryToolStripMenuItem.Click += new System.EventHandler(this.importAsBinaryToolStripMenuItem_Click);
			// 
			// LoadTableFileMenuItem
			// 
			this.LoadTableFileMenuItem.Text = "&Load .tbl file";
			this.LoadTableFileMenuItem.Click += new System.EventHandler(this.LoadTableFileMenuItem_Click);
			// 
			// CloseTableFileMenuItem
			// 
			this.CloseTableFileMenuItem.Text = "Close .tbl file";
			this.CloseTableFileMenuItem.Click += new System.EventHandler(this.CloseTableFileMenuItem_Click);
			// 
			// RecentTablesSubMenu
			// 
			this.RecentTablesSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem});
			this.RecentTablesSubMenu.Text = "Recent";
			this.RecentTablesSubMenu.DropDownOpened += new System.EventHandler(this.RecentTablesSubMenu_DropDownOpened);
			// 
			// noneToolStripMenuItem
			// 
			this.noneToolStripMenuItem.Text = "None";
			// 
			// EditMenuItem
			// 
			this.EditMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyMenuItem,
            this.ExportMenuItem,
            this.PasteMenuItem,
            this.toolStripSeparator6,
            this.FindMenuItem,
            this.FindNextMenuItem,
            this.FindPrevMenuItem});
			this.EditMenuItem.Text = "&Edit";
			this.EditMenuItem.DropDownOpened += new System.EventHandler(this.EditMenuItem_DropDownOpened);
			// 
			// CopyMenuItem
			// 
			this.CopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.CopyMenuItem.Text = "&Copy";
			this.CopyMenuItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// ExportMenuItem
			// 
			this.ExportMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.ExportMenuItem.Text = "&Export";
			this.ExportMenuItem.Click += new System.EventHandler(this.ExportMenuItem_Click);
			// 
			// PasteMenuItem
			// 
			this.PasteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.PasteMenuItem.Text = "&Paste";
			this.PasteMenuItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// FindMenuItem
			// 
			this.FindMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this.FindMenuItem.Text = "&Find...";
			this.FindMenuItem.Click += new System.EventHandler(this.FindMenuItem_Click);
			// 
			// FindNextMenuItem
			// 
			this.FindNextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.FindNextMenuItem.Text = "Find Next";
			this.FindNextMenuItem.Click += new System.EventHandler(this.FindNextMenuItem_Click);
			// 
			// FindPrevMenuItem
			// 
			this.FindPrevMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.FindPrevMenuItem.Text = "Find Prev";
			this.FindPrevMenuItem.Click += new System.EventHandler(this.FindPrevMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MemoryDomainsMenuItem,
            this.DataSizeSubMenu,
            this.BigEndianMenuItem,
            this.toolStripSeparator2,
            this.GoToAddressMenuItem,
            this.AddToRamWatchMenuItem,
            this.FreezeAddressMenuItem,
            this.UnfreezeAllMenuItem,
            this.PokeAddressMenuItem});
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// MemoryDomainsMenuItem
			// 
			this.MemoryDomainsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
			this.MemoryDomainsMenuItem.Text = "&Memory Domains";
			this.MemoryDomainsMenuItem.DropDownOpened += new System.EventHandler(this.MemoryDomainsMenuItem_DropDownOpened);
			// 
			// DataSizeSubMenu
			// 
			this.DataSizeSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DataSizeByteMenuItem,
            this.DataSizeWordMenuItem,
            this.DataSizeDWordMenuItem});
			this.DataSizeSubMenu.Text = "Data Size";
			// 
			// DataSizeByteMenuItem
			// 
			this.DataSizeByteMenuItem.Text = "1 Byte";
			this.DataSizeByteMenuItem.Click += new System.EventHandler(this.DataSizeByteMenuItem_Click);
			// 
			// DataSizeWordMenuItem
			// 
			this.DataSizeWordMenuItem.Text = "2 Byte";
			this.DataSizeWordMenuItem.Click += new System.EventHandler(this.DataSizeWordMenuItem_Click);
			// 
			// DataSizeDWordMenuItem
			// 
			this.DataSizeDWordMenuItem.Text = "4 Byte";
			this.DataSizeDWordMenuItem.Click += new System.EventHandler(this.DataSizeDWordMenuItem_Click);
			// 
			// BigEndianMenuItem
			// 
			this.BigEndianMenuItem.Text = "Big Endian";
			this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
			// 
			// GoToAddressMenuItem
			// 
			this.GoToAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
			this.GoToAddressMenuItem.Text = "&Go to Address...";
			this.GoToAddressMenuItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
			// 
			// AddToRamWatchMenuItem
			// 
			this.AddToRamWatchMenuItem.ShortcutKeyDisplayString = "Ctrl+W";
			this.AddToRamWatchMenuItem.Text = "Add to RAM Watch";
			this.AddToRamWatchMenuItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// FreezeAddressMenuItem
			// 
			this.FreezeAddressMenuItem.ShortcutKeyDisplayString = "Space";
			this.FreezeAddressMenuItem.Text = "&Freeze Address";
			this.FreezeAddressMenuItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// UnfreezeAllMenuItem
			// 
			this.UnfreezeAllMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Delete)));
			this.UnfreezeAllMenuItem.Text = "Unfreeze All";
			this.UnfreezeAllMenuItem.Click += new System.EventHandler(this.UnfreezeAllMenuItem_Click);
			// 
			// PokeAddressMenuItem
			// 
			this.PokeAddressMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.PokeAddressMenuItem.Text = "&Poke Address";
			this.PokeAddressMenuItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// SettingsSubMenu
			// 
			this.SettingsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CustomColorsSubMenu});
			this.SettingsSubMenu.Text = "&Settings";
			// 
			// CustomColorsSubMenu
			// 
			this.CustomColorsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SetColorsMenuItem,
            this.toolStripSeparator8,
            this.ResetColorsToDefaultMenuItem});
			this.CustomColorsSubMenu.Text = "Custom Colors";
			// 
			// SetColorsMenuItem
			// 
			this.SetColorsMenuItem.Text = "Set Colors";
			this.SetColorsMenuItem.Click += new System.EventHandler(this.SetColorsMenuItem_Click);
			// 
			// ResetColorsToDefaultMenuItem
			// 
			this.ResetColorsToDefaultMenuItem.Text = "Reset to Default";
			this.ResetColorsToDefaultMenuItem.Click += new System.EventHandler(this.ResetColorsToDefaultMenuItem_Click);
			// 
			// resetToDefaultToolStripMenuItem
			// 
			this.resetToDefaultToolStripMenuItem.Text = "Reset to Default";
			// 
			// ViewerContextMenuStrip
			// 
			this.ViewerContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyContextItem,
            this.ExportContextItem,
            this.PasteContextItem,
            this.FreezeContextItem,
            this.AddToRamWatchContextItem,
            this.UnfreezeAllContextItem,
            this.PokeContextItem,
            this.ContextSeparator1,
            this.IncrementContextItem,
            this.DecrementContextItem,
            this.ContextSeparator2,
            this.GoToContextItem,
            this.toolStripMenuItem1,
            this.viewN64MatrixToolStripMenuItem});
			this.ViewerContextMenuStrip.Name = "ViewerContextMenuStrip";
			this.ViewerContextMenuStrip.Size = new System.Drawing.Size(222, 264);
			this.ViewerContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.ViewerContextMenuStrip_Opening);
			// 
			// CopyContextItem
			// 
			this.CopyContextItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.CopyContextItem.Text = "&Copy";
			this.CopyContextItem.Click += new System.EventHandler(this.CopyMenuItem_Click);
			// 
			// ExportContextItem
			// 
			this.ExportContextItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.ExportContextItem.Text = "&Export";
			// 
			// PasteContextItem
			// 
			this.PasteContextItem.ShortcutKeyDisplayString = "Ctrl+V";
			this.PasteContextItem.Text = "&Paste";
			this.PasteContextItem.Click += new System.EventHandler(this.PasteMenuItem_Click);
			// 
			// FreezeContextItem
			// 
			this.FreezeContextItem.ShortcutKeyDisplayString = "Space";
			this.FreezeContextItem.Text = "&Freeze";
			this.FreezeContextItem.Click += new System.EventHandler(this.FreezeAddressMenuItem_Click);
			// 
			// AddToRamWatchContextItem
			// 
			this.AddToRamWatchContextItem.ShortcutKeyDisplayString = "Ctrl+W";
			this.AddToRamWatchContextItem.Text = "&Add to RAM Watch";
			this.AddToRamWatchContextItem.Click += new System.EventHandler(this.AddToRamWatchMenuItem_Click);
			// 
			// UnfreezeAllContextItem
			// 
			this.UnfreezeAllContextItem.ShortcutKeyDisplayString = "Shift+Del";
			this.UnfreezeAllContextItem.Text = "&Unfreeze All";
			this.UnfreezeAllContextItem.Click += new System.EventHandler(this.UnfreezeAllMenuItem_Click);
			// 
			// PokeContextItem
			// 
			this.PokeContextItem.ShortcutKeyDisplayString = "Ctrl+P";
			this.PokeContextItem.Text = "&Poke Address";
			this.PokeContextItem.Click += new System.EventHandler(this.PokeAddressMenuItem_Click);
			// 
			// IncrementContextItem
			// 
			this.IncrementContextItem.ShortcutKeyDisplayString = "+";
			this.IncrementContextItem.Text = "&Increment";
			this.IncrementContextItem.Click += new System.EventHandler(this.IncrementContextItem_Click);
			// 
			// DecrementContextItem
			// 
			this.DecrementContextItem.ShortcutKeyDisplayString = "-";
			this.DecrementContextItem.Text = "&Decrement";
			this.DecrementContextItem.Click += new System.EventHandler(this.DecrementContextItem_Click);
			// 
			// GoToContextItem
			// 
			this.GoToContextItem.ShortcutKeyDisplayString = "Ctrl+G";
			this.GoToContextItem.Text = "&Go to Address...";
			this.GoToContextItem.Click += new System.EventHandler(this.GoToAddressMenuItem_Click);
			// 
			// viewN64MatrixToolStripMenuItem
			// 
			this.viewN64MatrixToolStripMenuItem.Text = "View N64 Matrix";
			this.viewN64MatrixToolStripMenuItem.Click += new System.EventHandler(this.viewN64MatrixToolStripMenuItem_Click);
			// 
			// MemoryViewerBox
			// 
			this.MemoryViewerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MemoryViewerBox.ContextMenuStrip = this.ViewerContextMenuStrip;
			this.MemoryViewerBox.Controls.Add(this.HexScrollBar);
			this.MemoryViewerBox.Controls.Add(this.AddressLabel);
			this.MemoryViewerBox.Controls.Add(this.AddressesLabel);
			this.MemoryViewerBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MemoryViewerBox.Location = new System.Drawing.Point(12, 27);
			this.MemoryViewerBox.MaximumSize = new System.Drawing.Size(600, 1024);
			this.MemoryViewerBox.MinimumSize = new System.Drawing.Size(260, 180);
			this.MemoryViewerBox.Name = "MemoryViewerBox";
			this.MemoryViewerBox.Size = new System.Drawing.Size(560, 262);
			this.MemoryViewerBox.TabIndex = 2;
			this.MemoryViewerBox.TabStop = false;
			this.MemoryViewerBox.Paint += new System.Windows.Forms.PaintEventHandler(this.MemoryViewerBox_Paint);
			// 
			// HexScrollBar
			// 
			this.HexScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HexScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
			this.HexScrollBar.LargeChange = 16;
			this.HexScrollBar.Location = new System.Drawing.Point(544, 16);
			this.HexScrollBar.Name = "HexScrollBar";
			this.HexScrollBar.Size = new System.Drawing.Size(16, 246);
			this.HexScrollBar.TabIndex = 1;
			this.HexScrollBar.ValueChanged += new System.EventHandler(this.HexScrollBar_ValueChanged);
			// 
			// AddressLabel
			// 
			this.AddressLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AddressLabel.Location = new System.Drawing.Point(3, 30);
			this.AddressLabel.Name = "AddressLabel";
			this.AddressLabel.Text = "      ";
			// 
			// AddressesLabel
			// 
			this.AddressesLabel.ContextMenuStrip = this.ViewerContextMenuStrip;
			this.AddressesLabel.Location = new System.Drawing.Point(79, 30);
			this.AddressesLabel.Name = "AddressesLabel";
			this.AddressesLabel.Text = "RAM";
			this.AddressesLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseDown);
			this.AddressesLabel.MouseLeave += new System.EventHandler(this.AddressesLabel_MouseLeave);
			this.AddressesLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseMove);
			this.AddressesLabel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.AddressesLabel_MouseUp);
			// 
			// Header
			// 
			this.Header.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Header.Location = new System.Drawing.Point(28, 44);
			this.Header.Name = "Header";
			this.Header.Text = "label1";
			// 
			// HexEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 301);
			this.Controls.Add(this.Header);
			this.Controls.Add(this.MemoryViewerBox);
			this.Controls.Add(this.HexMenuStrip);
			this.MainMenuStrip = this.HexMenuStrip;
			this.MinimumSize = new System.Drawing.Size(360, 180);
			this.Name = "HexEditor";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Load += new System.EventHandler(this.HexEditor_Load);
			this.ResizeEnd += new System.EventHandler(this.HexEditor_ResizeEnd);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HexEditor_KeyDown);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.HexEditor_KeyPress);
			this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.HexEditor_MouseWheel);
			this.Resize += new System.EventHandler(this.HexEditor_Resize);
			this.HexMenuStrip.ResumeLayout(false);
			this.HexMenuStrip.PerformLayout();
			this.ViewerContextMenuStrip.ResumeLayout(false);
			this.MemoryViewerBox.ResumeLayout(false);
			this.MemoryViewerBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public MenuStripEx HexMenuStrip;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsTextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MemoryDomainsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeByteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeWordMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeDWordMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GoToAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SettingsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BigEndianMenuItem;
		private System.Windows.Forms.ContextMenuStrip ViewerContextMenuStrip;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddToRamWatchContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddToRamWatchMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FreezeAddressMenuItem;
		public System.Windows.Forms.GroupBox MemoryViewerBox;
		private BizHawk.WinForms.Controls.HexLabelEx AddressesLabel;
		private System.Windows.Forms.VScrollBar HexScrollBar;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UnfreezeAllMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UnfreezeAllContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextSeparator1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx IncrementContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DecrementContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx GoToContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx ContextSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FindMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator6;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsBinaryMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx resetToDefaultToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CustomColorsSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SetColorsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator8;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ResetColorsToDefaultMenuItem;
		public BizHawk.WinForms.Controls.LocLabelEx Header;
		private BizHawk.WinForms.Controls.LocLabelEx AddressLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CopyContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PasteContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FindNextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FindPrevMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeAddressMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx PokeContextItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx LoadTableFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentTablesSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx noneToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx CloseTableFileMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripMenuItem1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx viewN64MatrixToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExportContextItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExportMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx importAsBinaryToolStripMenuItem;
	}
}