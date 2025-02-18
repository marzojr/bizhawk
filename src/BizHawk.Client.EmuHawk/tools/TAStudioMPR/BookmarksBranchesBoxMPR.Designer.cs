namespace BizHawk.Client.EmuHawk
{
	partial class BookmarksBranchesBoxMPR
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.BookmarksBranchesGroupBox = new System.Windows.Forms.GroupBox();
			this.UndoBranchButton = new System.Windows.Forms.Button();
			this.JumpToBranchButton = new System.Windows.Forms.Button();
			this.UpdateBranchButton = new System.Windows.Forms.Button();
			this.AddWithTextBranchButton = new System.Windows.Forms.Button();
			this.AddBranchButton = new System.Windows.Forms.Button();
			this.LoadBranchButton = new System.Windows.Forms.Button();
			this.BranchView = new BizHawk.Client.EmuHawk.InputRoll();
			this.BranchesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.AddBranchContextMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AddBranchWithTextContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.LoadBranchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.UpdateBranchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditBranchTextContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.JumpToBranchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.UndoBranchToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RemoveBranchContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.BookmarksBranchesGroupBox.SuspendLayout();
			this.BranchesContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// BookmarksBranchesGroupBox
			// 
			this.BookmarksBranchesGroupBox.Controls.Add(this.UndoBranchButton);
			this.BookmarksBranchesGroupBox.Controls.Add(this.JumpToBranchButton);
			this.BookmarksBranchesGroupBox.Controls.Add(this.UpdateBranchButton);
			this.BookmarksBranchesGroupBox.Controls.Add(this.AddWithTextBranchButton);
			this.BookmarksBranchesGroupBox.Controls.Add(this.AddBranchButton);
			this.BookmarksBranchesGroupBox.Controls.Add(this.LoadBranchButton);
			this.BookmarksBranchesGroupBox.Controls.Add(this.BranchView);
			this.BookmarksBranchesGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BookmarksBranchesGroupBox.Location = new System.Drawing.Point(0, 0);
			this.BookmarksBranchesGroupBox.Name = "BookmarksBranchesGroupBox";
			this.BookmarksBranchesGroupBox.Size = new System.Drawing.Size(198, 278);
			this.BookmarksBranchesGroupBox.TabIndex = 0;
			this.BookmarksBranchesGroupBox.TabStop = false;
			this.BookmarksBranchesGroupBox.Text = "Branches";
			// 
			// UndoBranchButton
			// 
			this.UndoBranchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UndoBranchButton.Enabled = false;
			this.UndoBranchButton.Location = new System.Drawing.Point(156, 247);
			this.UndoBranchButton.Name = "UndoBranchButton";
			this.UndoBranchButton.Size = new System.Drawing.Size(24, 24);
			this.UndoBranchButton.TabIndex = 6;
			this.UndoBranchButton.UseVisualStyleBackColor = true;
			this.UndoBranchButton.Click += new System.EventHandler(this.UndoBranchToolStripMenuItem_Click);
			// 
			// JumpToBranchButton
			// 
			this.JumpToBranchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.JumpToBranchButton.Enabled = false;
			this.JumpToBranchButton.Location = new System.Drawing.Point(126, 247);
			this.JumpToBranchButton.Name = "JumpToBranchButton";
			this.JumpToBranchButton.Size = new System.Drawing.Size(24, 24);
			this.JumpToBranchButton.TabIndex = 5;
			this.toolTip1.SetToolTip(this.JumpToBranchButton, "Jump To Branch Frame");
			this.JumpToBranchButton.UseVisualStyleBackColor = true;
			this.JumpToBranchButton.Click += new System.EventHandler(this.JumpToBranchToolStripMenuItem_Click);
			// 
			// UpdateBranchButton
			// 
			this.UpdateBranchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UpdateBranchButton.Enabled = false;
			this.UpdateBranchButton.Location = new System.Drawing.Point(96, 247);
			this.UpdateBranchButton.Name = "UpdateBranchButton";
			this.UpdateBranchButton.Size = new System.Drawing.Size(24, 24);
			this.UpdateBranchButton.TabIndex = 4;
			this.toolTip1.SetToolTip(this.UpdateBranchButton, "Update Branch");
			this.UpdateBranchButton.UseVisualStyleBackColor = true;
			this.UpdateBranchButton.Click += new System.EventHandler(this.UpdateBranchToolStripMenuItem_Click);
			// 
			// AddWithTextBranchButton
			// 
			this.AddWithTextBranchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddWithTextBranchButton.Location = new System.Drawing.Point(36, 247);
			this.AddWithTextBranchButton.Name = "AddWithTextBranchButton";
			this.AddWithTextBranchButton.Size = new System.Drawing.Size(24, 24);
			this.AddWithTextBranchButton.TabIndex = 2;
			this.toolTip1.SetToolTip(this.AddWithTextBranchButton, "Add Branch with Text");
			this.AddWithTextBranchButton.UseVisualStyleBackColor = true;
			this.AddWithTextBranchButton.Click += new System.EventHandler(this.AddBranchWithTexToolStripMenuItem_Click);
			// 
			// AddBranchButton
			// 
			this.AddBranchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddBranchButton.Location = new System.Drawing.Point(6, 247);
			this.AddBranchButton.Name = "AddBranchButton";
			this.AddBranchButton.Size = new System.Drawing.Size(24, 24);
			this.AddBranchButton.TabIndex = 1;
			this.toolTip1.SetToolTip(this.AddBranchButton, "Add Branch");
			this.AddBranchButton.UseVisualStyleBackColor = true;
			this.AddBranchButton.Click += new System.EventHandler(this.AddBranchToolStripMenuItem_Click);
			// 
			// LoadBranchButton
			// 
			this.LoadBranchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoadBranchButton.Enabled = false;
			this.LoadBranchButton.Location = new System.Drawing.Point(66, 247);
			this.LoadBranchButton.Name = "LoadBranchButton";
			this.LoadBranchButton.Size = new System.Drawing.Size(24, 24);
			this.LoadBranchButton.TabIndex = 3;
			this.toolTip1.SetToolTip(this.LoadBranchButton, "Load Branch");
			this.LoadBranchButton.UseVisualStyleBackColor = true;
			this.LoadBranchButton.Click += new System.EventHandler(this.LoadBranchToolStripMenuItem_Click);
			// 
			// BranchView
			// 
			this.BranchView.AllowColumnReorder = false;
			this.BranchView.AllowColumnResize = false;
			this.BranchView.AlwaysScroll = false;
			this.BranchView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BranchView.CellHeightPadding = 0;
			this.BranchView.ChangeSelectionWhenPaging = false;
			this.BranchView.ContextMenuStrip = this.BranchesContextMenu;
			this.BranchView.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BranchView.FullRowSelect = true;
			this.BranchView.HorizontalOrientation = false;
			this.BranchView.LetKeysModifySelection = false;
			this.BranchView.Location = new System.Drawing.Point(6, 19);
			this.BranchView.Name = "BranchView";
			this.BranchView.RowCount = 0;
			this.BranchView.ScrollSpeed = 1;
			this.BranchView.Size = new System.Drawing.Size(186, 224);
			this.BranchView.TabIndex = 0;
			this.BranchView.PointedCellChanged += new BizHawk.Client.EmuHawk.InputRoll.CellChangeEventHandler(this.BranchView_PointedCellChanged);
			this.BranchView.SelectedIndexChanged += new System.EventHandler(this.BranchView_SelectedIndexChanged);
			this.BranchView.CellDropped += new BizHawk.Client.EmuHawk.InputRoll.CellDroppedEvent(this.BranchView_CellDropped);
			this.BranchView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseDoubleClick);
			this.BranchView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseDown);
			this.BranchView.MouseLeave += new System.EventHandler(this.BranchView_MouseLeave);
			this.BranchView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseMove);
			this.BranchView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseUp);
			// 
			// BranchesContextMenu
			// 
			this.BranchesContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AddBranchContextMenu,
            this.AddBranchWithTextContextMenuItem,
            this.LoadBranchContextMenuItem,
            this.UpdateBranchContextMenuItem,
            this.EditBranchTextContextMenuItem,
            this.JumpToBranchContextMenuItem,
            this.UndoBranchToolStripMenuItem,
            this.toolStripSeparator2,
            this.RemoveBranchContextMenuItem});
			this.BranchesContextMenu.Name = "BranchesContextMenu";
			this.BranchesContextMenu.Size = new System.Drawing.Size(147, 186);
			this.BranchesContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.BranchesContextMenu_Opening);
			// 
			// AddBranchContextMenu
			// 
			this.AddBranchContextMenu.Text = "Add";
			this.AddBranchContextMenu.Click += new System.EventHandler(this.AddBranchToolStripMenuItem_Click);
			// 
			// AddBranchWithTextContextMenuItem
			// 
			this.AddBranchWithTextContextMenuItem.Text = "Add with Text";
			this.AddBranchWithTextContextMenuItem.Click += new System.EventHandler(this.AddBranchWithTexToolStripMenuItem_Click);
			// 
			// LoadBranchContextMenuItem
			// 
			this.LoadBranchContextMenuItem.Text = "Load";
			this.LoadBranchContextMenuItem.Click += new System.EventHandler(this.LoadBranchToolStripMenuItem_Click);
			// 
			// UpdateBranchContextMenuItem
			// 
			this.UpdateBranchContextMenuItem.Text = "&Update";
			this.UpdateBranchContextMenuItem.Click += new System.EventHandler(this.UpdateBranchToolStripMenuItem_Click);
			// 
			// EditBranchTextContextMenuItem
			// 
			this.EditBranchTextContextMenuItem.Text = "Edit Text";
			this.EditBranchTextContextMenuItem.Click += new System.EventHandler(this.EditBranchTextToolStripMenuItem_Click);
			// 
			// JumpToBranchContextMenuItem
			// 
			this.JumpToBranchContextMenuItem.Text = "Jump To";
			this.JumpToBranchContextMenuItem.Click += new System.EventHandler(this.JumpToBranchToolStripMenuItem_Click);
			// 
			// UndoBranchToolStripMenuItem
			// 
			this.UndoBranchToolStripMenuItem.Enabled = false;
			this.UndoBranchToolStripMenuItem.Text = "Undo";
			this.UndoBranchToolStripMenuItem.Click += new System.EventHandler(this.UndoBranchToolStripMenuItem_Click);
			// 
			// RemoveBranchContextMenuItem
			// 
			this.RemoveBranchContextMenuItem.Text = "Remove";
			this.RemoveBranchContextMenuItem.Click += new System.EventHandler(this.RemoveBranchToolStripMenuItem_Click);
			// 
			// BookmarksBranchesBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.BookmarksBranchesGroupBox);
			this.Name = "BookmarksBranchesBox";
			this.Size = new System.Drawing.Size(198, 278);
			this.BookmarksBranchesGroupBox.ResumeLayout(false);
			this.BranchesContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox BookmarksBranchesGroupBox;
		private InputRoll BranchView;
		private System.Windows.Forms.ContextMenuStrip BranchesContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddBranchContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveBranchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx LoadBranchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UpdateBranchContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditBranchTextContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddBranchWithTextContextMenuItem;
		private System.Windows.Forms.Button UpdateBranchButton;
		private System.Windows.Forms.Button AddWithTextBranchButton;
		private System.Windows.Forms.Button AddBranchButton;
		private System.Windows.Forms.Button LoadBranchButton;
		private System.Windows.Forms.ToolTip toolTip1;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private System.Windows.Forms.Button JumpToBranchButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx JumpToBranchContextMenuItem;
		private System.Windows.Forms.Button UndoBranchButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx UndoBranchToolStripMenuItem;
	}
}
