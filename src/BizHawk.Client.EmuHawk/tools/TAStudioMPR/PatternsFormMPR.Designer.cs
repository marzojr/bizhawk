namespace BizHawk.Client.EmuHawk
{
	partial class PatternsFormMPR
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
			this.ButtonBox = new System.Windows.Forms.ComboBox();
			this.PatternList = new System.Windows.Forms.ListBox();
			this.InsertButton = new System.Windows.Forms.Button();
			this.DeleteButton = new System.Windows.Forms.Button();
			this.LagBox = new System.Windows.Forms.CheckBox();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.ValueNum = new System.Windows.Forms.NumericUpDown();
			this.CountNum = new System.Windows.Forms.NumericUpDown();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.OnOffBox = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.ValueNum)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CountNum)).BeginInit();
			this.SuspendLayout();
			// 
			// ButtonBox
			// 
			this.ButtonBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ButtonBox.FormattingEnabled = true;
			this.ButtonBox.Location = new System.Drawing.Point(12, 12);
			this.ButtonBox.Name = "ButtonBox";
			this.ButtonBox.Size = new System.Drawing.Size(169, 21);
			this.ButtonBox.TabIndex = 0;
			this.ButtonBox.SelectedIndexChanged += new System.EventHandler(this.ButtonBox_SelectedIndexChanged);
			// 
			// PatternList
			// 
			this.PatternList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.PatternList.FormattingEnabled = true;
			this.PatternList.Items.AddRange(new object[] {
            "0: On\t(x1)",
            "1: Off\t(x1)",
            "Loop to 0"});
			this.PatternList.Location = new System.Drawing.Point(12, 39);
			this.PatternList.Name = "PatternList";
			this.PatternList.Size = new System.Drawing.Size(169, 134);
			this.PatternList.TabIndex = 1;
			this.PatternList.SelectedIndexChanged += new System.EventHandler(this.PatternList_SelectedIndexChanged);
			// 
			// InsertButton
			// 
			this.InsertButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.InsertButton.Location = new System.Drawing.Point(12, 207);
			this.InsertButton.Name = "InsertButton";
			this.InsertButton.Size = new System.Drawing.Size(57, 23);
			this.InsertButton.TabIndex = 2;
			this.InsertButton.Text = "Insert";
			this.InsertButton.UseVisualStyleBackColor = true;
			this.InsertButton.Click += new System.EventHandler(this.InsertButton_Click);
			// 
			// DeleteButton
			// 
			this.DeleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DeleteButton.Location = new System.Drawing.Point(124, 207);
			this.DeleteButton.Name = "DeleteButton";
			this.DeleteButton.Size = new System.Drawing.Size(57, 23);
			this.DeleteButton.TabIndex = 2;
			this.DeleteButton.Text = "Delete";
			this.DeleteButton.UseVisualStyleBackColor = true;
			this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
			// 
			// LagBox
			// 
			this.LagBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LagBox.AutoSize = true;
			this.LagBox.Checked = true;
			this.LagBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.LagBox.Location = new System.Drawing.Point(12, 236);
			this.LagBox.Name = "LagBox";
			this.LagBox.Size = new System.Drawing.Size(132, 17);
			this.LagBox.TabIndex = 3;
			this.LagBox.Text = "Account for lag frames";
			this.LagBox.UseVisualStyleBackColor = true;
			this.LagBox.CheckedChanged += new System.EventHandler(this.LagBox_CheckedChanged);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(9, 181);
			this.label1.Name = "label1";
			this.label1.Text = "Value:";
			// 
			// ValueNum
			// 
			this.ValueNum.Location = new System.Drawing.Point(48, 179);
			this.ValueNum.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.ValueNum.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
			this.ValueNum.Name = "ValueNum";
			this.ValueNum.Size = new System.Drawing.Size(51, 20);
			this.ValueNum.TabIndex = 5;
			this.ValueNum.Visible = false;
			this.ValueNum.ValueChanged += new System.EventHandler(this.ValueNum_ValueChanged);
			// 
			// CountNum
			// 
			this.CountNum.Location = new System.Drawing.Point(143, 179);
			this.CountNum.Name = "CountNum";
			this.CountNum.Size = new System.Drawing.Size(38, 20);
			this.CountNum.TabIndex = 5;
			this.CountNum.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.CountNum.ValueChanged += new System.EventHandler(this.CountNum_ValueChanged);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(103, 181);
			this.label2.Name = "label2";
			this.label2.Text = "Count:";
			// 
			// OnOffBox
			// 
			this.OnOffBox.AutoSize = true;
			this.OnOffBox.Location = new System.Drawing.Point(48, 180);
			this.OnOffBox.Name = "OnOffBox";
			this.OnOffBox.Size = new System.Drawing.Size(15, 14);
			this.OnOffBox.TabIndex = 6;
			this.OnOffBox.UseVisualStyleBackColor = true;
			this.OnOffBox.CheckedChanged += new System.EventHandler(this.OnOffBox_CheckedChanged);
			// 
			// PatternsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(193, 258);
			this.Controls.Add(this.OnOffBox);
			this.Controls.Add(this.CountNum);
			this.Controls.Add(this.ValueNum);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.LagBox);
			this.Controls.Add(this.DeleteButton);
			this.Controls.Add(this.InsertButton);
			this.Controls.Add(this.PatternList);
			this.Controls.Add(this.ButtonBox);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(209, 9999);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(209, 39);
			this.Name = "PatternsForm";
			this.Text = "Patterns Options";
			this.Load += new System.EventHandler(this.PatternsForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.ValueNum)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CountNum)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox ButtonBox;
		private System.Windows.Forms.ListBox PatternList;
		private System.Windows.Forms.Button InsertButton;
		private System.Windows.Forms.Button DeleteButton;
		private System.Windows.Forms.CheckBox LagBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.NumericUpDown ValueNum;
		private System.Windows.Forms.NumericUpDown CountNum;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.CheckBox OnOffBox;
	}
}