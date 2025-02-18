namespace BizHawk.Client.EmuHawk
{
	partial class FramesPromptMPR
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
			this.OkBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.NumFramesBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(67, 47);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 1;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(134, 47);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 2;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(12, 20);
			this.label1.Name = "label1";
			this.label1.Text = "Insert number of frames:";
			// 
			// NumFramesBox
			// 
			this.NumFramesBox.ByteSize = BizHawk.Client.Common.WatchSize.Word;
			this.NumFramesBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.NumFramesBox.Location = new System.Drawing.Point(138, 16);
			this.NumFramesBox.MaxLength = 5;
			this.NumFramesBox.Name = "NumFramesBox";
			this.NumFramesBox.Nullable = false;
			this.NumFramesBox.Size = new System.Drawing.Size(51, 20);
			this.NumFramesBox.TabIndex = 3;
			this.NumFramesBox.Text = "1";
			this.NumFramesBox.Type = BizHawk.Client.Common.WatchDisplayType.Unsigned;
			// 
			// FramesPrompt
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(206, 82);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.NumFramesBox);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FramesPrompt";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Insert Frames";
			this.Load += new System.EventHandler(this.FramesPrompt_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private WatchValueBox NumFramesBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
	}
}