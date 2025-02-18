namespace BizHawk.Client.EmuHawk
{
	partial class UndoHistoryFormMPR
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
			this.ClearButton = new System.Windows.Forms.Button();
			this.UndoButton = new System.Windows.Forms.Button();
			this.RedoButton = new System.Windows.Forms.Button();
			this.RightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.undoHereToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.redoHereToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.sepToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.clearHistoryToHereToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.AutoScrollCheck = new System.Windows.Forms.CheckBox();
			this.MaxStepsNum = new System.Windows.Forms.NumericUpDown();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.HistoryView = new BizHawk.Client.EmuHawk.InputRoll();
			this.RightClickMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MaxStepsNum)).BeginInit();
			this.SuspendLayout();
			// 
			// ClearButton
			// 
			this.ClearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ClearButton.Location = new System.Drawing.Point(326, 228);
			this.ClearButton.Name = "ClearButton";
			this.ClearButton.Size = new System.Drawing.Size(55, 22);
			this.ClearButton.TabIndex = 4;
			this.ClearButton.Text = "Clear";
			this.ClearButton.UseVisualStyleBackColor = true;
			this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
			// 
			// UndoButton
			// 
			this.UndoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UndoButton.Location = new System.Drawing.Point(10, 228);
			this.UndoButton.Name = "UndoButton";
			this.UndoButton.Size = new System.Drawing.Size(55, 22);
			this.UndoButton.TabIndex = 3;
			this.UndoButton.Text = "Undo";
			this.UndoButton.UseVisualStyleBackColor = true;
			this.UndoButton.Click += new System.EventHandler(this.UndoButton_Click);
			// 
			// RedoButton
			// 
			this.RedoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.RedoButton.Location = new System.Drawing.Point(71, 228);
			this.RedoButton.Name = "RedoButton";
			this.RedoButton.Size = new System.Drawing.Size(55, 22);
			this.RedoButton.TabIndex = 3;
			this.RedoButton.Text = "Redo";
			this.RedoButton.UseVisualStyleBackColor = true;
			this.RedoButton.Click += new System.EventHandler(this.RedoButton_Click);
			// 
			// RightClickMenu
			// 
			this.RightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoHereToolStripMenuItem,
            this.redoHereToolStripMenuItem,
            this.sepToolStripMenuItem,
            this.clearHistoryToHereToolStripMenuItem});
			this.RightClickMenu.Name = "RightClickMenu";
			this.RightClickMenu.Size = new System.Drawing.Size(209, 76);
			// 
			// undoHereToolStripMenuItem
			// 
			this.undoHereToolStripMenuItem.Text = "Undo To Selection";
			this.undoHereToolStripMenuItem.Click += new System.EventHandler(this.UndoHereMenuItem_Click);
			// 
			// redoHereToolStripMenuItem
			// 
			this.redoHereToolStripMenuItem.Text = "Redo To Selection";
			this.redoHereToolStripMenuItem.Click += new System.EventHandler(this.RedoHereMenuItem_Click);
			// 
			// clearHistoryToHereToolStripMenuItem
			// 
			this.clearHistoryToHereToolStripMenuItem.Text = "Clear History To Selection";
			this.clearHistoryToHereToolStripMenuItem.Click += new System.EventHandler(this.ClearHistoryToHereMenuItem_Click);
			// 
			// AutoScrollCheck
			// 
			this.AutoScrollCheck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AutoScrollCheck.AutoSize = true;
			this.AutoScrollCheck.Checked = true;
			this.AutoScrollCheck.CheckState = System.Windows.Forms.CheckState.Checked;
			this.AutoScrollCheck.Location = new System.Drawing.Point(132, 233);
			this.AutoScrollCheck.Name = "AutoScrollCheck";
			this.AutoScrollCheck.Size = new System.Drawing.Size(77, 17);
			this.AutoScrollCheck.TabIndex = 5;
			this.AutoScrollCheck.Text = "Auto Scroll";
			this.AutoScrollCheck.UseVisualStyleBackColor = true;
			// 
			// MaxStepsNum
			// 
			this.MaxStepsNum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.MaxStepsNum.Location = new System.Drawing.Point(268, 230);
			this.MaxStepsNum.Maximum = new decimal(new int[] {
            -1486618625,
            232830643,
            0,
            0});
			this.MaxStepsNum.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.MaxStepsNum.Name = "MaxStepsNum";
			this.MaxStepsNum.Size = new System.Drawing.Size(52, 20);
			this.MaxStepsNum.TabIndex = 6;
			this.MaxStepsNum.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			this.MaxStepsNum.ValueChanged += new System.EventHandler(this.MaxStepsNum_ValueChanged);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(236, 234);
			this.label1.Name = "label1";
			this.label1.Text = "Max:";
			// 
			// HistoryView
			// 
			this.HistoryView.AllowColumnReorder = false;
			this.HistoryView.AllowColumnResize = false;
			this.HistoryView.AlwaysScroll = false;
			this.HistoryView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HistoryView.CellHeightPadding = 0;
			this.HistoryView.CellWidthPadding = 0;
			this.HistoryView.FullRowSelect = true;
			this.HistoryView.HorizontalOrientation = false;
			this.HistoryView.LetKeysModifySelection = false;
			this.HistoryView.Location = new System.Drawing.Point(10, 10);
			this.HistoryView.MultiSelect = false;
			this.HistoryView.Name = "HistoryView";
			this.HistoryView.RowCount = 0;
			this.HistoryView.ScrollSpeed = 0;
			this.HistoryView.Size = new System.Drawing.Size(369, 213);
			this.HistoryView.TabIndex = 2;
			this.HistoryView.DoubleClick += new System.EventHandler(this.HistoryView_DoubleClick);
			this.HistoryView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HistoryView_MouseDown);
			this.HistoryView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.HistoryView_MouseUp);
			// 
			// UndoHistoryForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(391, 258);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.MaxStepsNum);
			this.Controls.Add(this.AutoScrollCheck);
			this.Controls.Add(this.ClearButton);
			this.Controls.Add(this.RedoButton);
			this.Controls.Add(this.UndoButton);
			this.Controls.Add(this.HistoryView);
			this.Name = "UndoHistoryForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Undo History";
			this.RightClickMenu.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.MaxStepsNum)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button ClearButton;
		private System.Windows.Forms.Button UndoButton;
		private InputRoll HistoryView;
		private System.Windows.Forms.Button RedoButton;
		private System.Windows.Forms.ContextMenuStrip RightClickMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx undoHereToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx redoHereToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx sepToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx clearHistoryToHereToolStripMenuItem;
		private System.Windows.Forms.CheckBox AutoScrollCheck;
		private System.Windows.Forms.NumericUpDown MaxStepsNum;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
	}
}