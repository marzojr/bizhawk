namespace BizHawk.Client.EmuHawk
{
	partial class MarkerControlMPR
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
			this.MarkerContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.JumpToMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ScrollToMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.EditMarkerFrameToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AddMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.RemoveMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.JumpToMarkerButton = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.EditMarkerButton = new System.Windows.Forms.Button();
			this.EditMarkerFrameButton = new System.Windows.Forms.Button();
			this.AddMarkerButton = new System.Windows.Forms.Button();
			this.RemoveMarkerButton = new System.Windows.Forms.Button();
			this.ScrollToMarkerButton = new System.Windows.Forms.Button();
			this.AddMarkerWithTextButton = new System.Windows.Forms.Button();
			this.MarkerView = new BizHawk.Client.EmuHawk.InputRoll();
			this.MarkersGroupBox = new System.Windows.Forms.GroupBox();
			this.MarkerContextMenu.SuspendLayout();
			this.MarkersGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// MarkerContextMenu
			// 
			this.MarkerContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.JumpToMarkerToolStripMenuItem,
            this.ScrollToMarkerToolStripMenuItem,
            this.EditMarkerToolStripMenuItem,
            this.EditMarkerFrameToolStripMenuItem,
            this.AddMarkerToolStripMenuItem,
            this.toolStripSeparator1,
            this.RemoveMarkerToolStripMenuItem});
			this.MarkerContextMenu.Name = "MarkerContextMenu";
			this.MarkerContextMenu.Size = new System.Drawing.Size(196, 142);
			this.MarkerContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.MarkerContextMenu_Opening);
			// 
			// JumpToMarkerToolStripMenuItem
			// 
			this.JumpToMarkerToolStripMenuItem.Text = "Jump To";
			this.JumpToMarkerToolStripMenuItem.Click += new System.EventHandler(this.JumpToMarkerToolStripMenuItem_Click);
			// 
			// ScrollToMarkerToolStripMenuItem
			// 
			this.ScrollToMarkerToolStripMenuItem.Text = "Scroll To";
			this.ScrollToMarkerToolStripMenuItem.Click += new System.EventHandler(this.ScrollToMarkerToolStripMenuItem_Click);
			// 
			// EditMarkerToolStripMenuItem
			// 
			this.EditMarkerToolStripMenuItem.Text = "Edit Text";
			this.EditMarkerToolStripMenuItem.Click += new System.EventHandler(this.EditMarkerToolStripMenuItem_Click);
			// 
			// EditMarkerFrameToolStripMenuItem
			// 
			this.EditMarkerFrameToolStripMenuItem.Text = "Edit Frame (Alt + Drag)";
			this.EditMarkerFrameToolStripMenuItem.Click += new System.EventHandler(this.EditMarkerFrameToolStripMenuItem_Click);
			// 
			// AddMarkerToolStripMenuItem
			// 
			this.AddMarkerToolStripMenuItem.Text = "Add";
			this.AddMarkerToolStripMenuItem.Click += new System.EventHandler(this.AddMarkerToolStripMenuItem_Click);
			// 
			// RemoveMarkerToolStripMenuItem
			// 
			this.RemoveMarkerToolStripMenuItem.Text = "Remove";
			this.RemoveMarkerToolStripMenuItem.Click += new System.EventHandler(this.RemoveMarkerToolStripMenuItem_Click);
			// 
			// JumpToMarkerButton
			// 
			this.JumpToMarkerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.JumpToMarkerButton.Enabled = false;
			this.JumpToMarkerButton.Location = new System.Drawing.Point(61, 247);
			this.JumpToMarkerButton.Name = "JumpToMarkerButton";
			this.JumpToMarkerButton.Size = new System.Drawing.Size(24, 24);
			this.JumpToMarkerButton.TabIndex = 3;
			this.toolTip1.SetToolTip(this.JumpToMarkerButton, "Jump To Marker Frame");
			this.JumpToMarkerButton.UseVisualStyleBackColor = true;
			this.JumpToMarkerButton.Click += new System.EventHandler(this.JumpToMarkerToolStripMenuItem_Click);
			// 
			// EditMarkerButton
			// 
			this.EditMarkerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.EditMarkerButton.Enabled = false;
			this.EditMarkerButton.Location = new System.Drawing.Point(117, 247);
			this.EditMarkerButton.Name = "EditMarkerButton";
			this.EditMarkerButton.Size = new System.Drawing.Size(24, 24);
			this.EditMarkerButton.TabIndex = 5;
			this.toolTip1.SetToolTip(this.EditMarkerButton, "Edit Marker Text");
			this.EditMarkerButton.UseVisualStyleBackColor = true;
			this.EditMarkerButton.Click += new System.EventHandler(this.EditMarkerToolStripMenuItem_Click);
			// 
			// EditMarkerFrameButton
			// 
			this.EditMarkerFrameButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.EditMarkerFrameButton.Enabled = false;
			this.EditMarkerFrameButton.Location = new System.Drawing.Point(145, 247);
			this.EditMarkerFrameButton.Name = "EditMarkerFrameButton";
			this.EditMarkerFrameButton.Size = new System.Drawing.Size(24, 24);
			this.EditMarkerFrameButton.TabIndex = 6;
			this.toolTip1.SetToolTip(this.EditMarkerFrameButton, "Edit Marker Frame (Alt + Drag)");
			this.EditMarkerFrameButton.UseVisualStyleBackColor = true;
			this.EditMarkerFrameButton.Click += new System.EventHandler(this.EditMarkerFrameToolStripMenuItem_Click);
			// 
			// AddMarkerButton
			// 
			this.AddMarkerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddMarkerButton.Location = new System.Drawing.Point(5, 247);
			this.AddMarkerButton.Name = "AddMarkerButton";
			this.AddMarkerButton.Size = new System.Drawing.Size(24, 24);
			this.AddMarkerButton.TabIndex = 1;
			this.toolTip1.SetToolTip(this.AddMarkerButton, "Add Marker to Emulated Frame");
			this.AddMarkerButton.UseVisualStyleBackColor = true;
			this.AddMarkerButton.Click += new System.EventHandler(this.AddMarkerToolStripMenuItem_Click);
			// 
			// RemoveMarkerButton
			// 
			this.RemoveMarkerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.RemoveMarkerButton.Enabled = false;
			this.RemoveMarkerButton.Location = new System.Drawing.Point(173, 247);
			this.RemoveMarkerButton.Name = "RemoveMarkerButton";
			this.RemoveMarkerButton.Size = new System.Drawing.Size(24, 24);
			this.RemoveMarkerButton.TabIndex = 7;
			this.toolTip1.SetToolTip(this.RemoveMarkerButton, "Remove Marker");
			this.RemoveMarkerButton.UseVisualStyleBackColor = true;
			this.RemoveMarkerButton.Click += new System.EventHandler(this.RemoveMarkerToolStripMenuItem_Click);
			// 
			// ScrollToMarkerButton
			// 
			this.ScrollToMarkerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ScrollToMarkerButton.Enabled = false;
			this.ScrollToMarkerButton.Location = new System.Drawing.Point(89, 247);
			this.ScrollToMarkerButton.Name = "ScrollToMarkerButton";
			this.ScrollToMarkerButton.Size = new System.Drawing.Size(24, 24);
			this.ScrollToMarkerButton.TabIndex = 4;
			this.toolTip1.SetToolTip(this.ScrollToMarkerButton, "Scroll View To Marker Frame");
			this.ScrollToMarkerButton.UseVisualStyleBackColor = true;
			this.ScrollToMarkerButton.Click += new System.EventHandler(this.ScrollToMarkerToolStripMenuItem_Click);
			// 
			// AddMarkerWithTextButton
			// 
			this.AddMarkerWithTextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddMarkerWithTextButton.Location = new System.Drawing.Point(33, 247);
			this.AddMarkerWithTextButton.Name = "AddMarkerWithTextButton";
			this.AddMarkerWithTextButton.Size = new System.Drawing.Size(24, 24);
			this.AddMarkerWithTextButton.TabIndex = 2;
			this.toolTip1.SetToolTip(this.AddMarkerWithTextButton, "Add Marker with Text to Emulated Frame");
			this.AddMarkerWithTextButton.UseVisualStyleBackColor = true;
			this.AddMarkerWithTextButton.Click += new System.EventHandler(this.AddMarkerWithTextToolStripMenuItem_Click);
			// 
			// MarkerView
			// 
			this.MarkerView.AllowColumnReorder = false;
			this.MarkerView.AllowColumnResize = false;
			this.MarkerView.AlwaysScroll = false;
			this.MarkerView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MarkerView.CellHeightPadding = 0;
			this.MarkerView.ContextMenuStrip = this.MarkerContextMenu;
			this.MarkerView.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MarkerView.FullRowSelect = true;
			this.MarkerView.HorizontalOrientation = false;
			this.MarkerView.LetKeysModifySelection = false;
			this.MarkerView.Location = new System.Drawing.Point(6, 19);
			this.MarkerView.Name = "MarkerView";
			this.MarkerView.RowCount = 0;
			this.MarkerView.ScrollSpeed = 1;
			this.MarkerView.Size = new System.Drawing.Size(186, 224);
			this.MarkerView.TabIndex = 0;
			this.MarkerView.TabStop = false;
			this.MarkerView.SelectedIndexChanged += new System.EventHandler(this.MarkerView_SelectedIndexChanged);
			this.MarkerView.DoubleClick += new System.EventHandler(this.MarkerView_MouseDoubleClick);
			// 
			// MarkersGroupBox
			// 
			this.MarkersGroupBox.Controls.Add(this.MarkerView);
			this.MarkersGroupBox.Controls.Add(this.AddMarkerButton);
			this.MarkersGroupBox.Controls.Add(this.AddMarkerWithTextButton);
			this.MarkersGroupBox.Controls.Add(this.RemoveMarkerButton);
			this.MarkersGroupBox.Controls.Add(this.ScrollToMarkerButton);
			this.MarkersGroupBox.Controls.Add(this.EditMarkerButton);
			this.MarkersGroupBox.Controls.Add(this.EditMarkerFrameButton);
			this.MarkersGroupBox.Controls.Add(this.JumpToMarkerButton);
			this.MarkersGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MarkersGroupBox.Location = new System.Drawing.Point(0, 0);
			this.MarkersGroupBox.Name = "MarkersGroupBox";
			this.MarkersGroupBox.Size = new System.Drawing.Size(198, 278);
			this.MarkersGroupBox.TabIndex = 0;
			this.MarkersGroupBox.TabStop = false;
			this.MarkersGroupBox.Text = "Markers";
			// 
			// MarkerControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.MarkersGroupBox);
			this.Name = "MarkerControl";
			this.Size = new System.Drawing.Size(198, 278);
			this.MarkerContextMenu.ResumeLayout(false);
			this.MarkersGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private InputRoll MarkerView;
		private System.Windows.Forms.Button AddMarkerButton;
		private System.Windows.Forms.Button RemoveMarkerButton;
		private System.Windows.Forms.Button JumpToMarkerButton;
		private System.Windows.Forms.Button EditMarkerButton;
		private System.Windows.Forms.Button EditMarkerFrameButton;
		private System.Windows.Forms.Button ScrollToMarkerButton;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ContextMenuStrip MarkerContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScrollToMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditMarkerFrameToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx JumpToMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private System.Windows.Forms.Button AddMarkerWithTextButton;
		private System.Windows.Forms.GroupBox MarkersGroupBox;
	}
}
