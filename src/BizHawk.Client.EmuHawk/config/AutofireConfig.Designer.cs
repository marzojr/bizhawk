namespace BizHawk.Client.EmuHawk
{
	partial class AutofireConfig
	{
		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnDialogOK = new BizHawk.WinForms.Controls.SzButtonEx();
			this.btnDialogCancel = new BizHawk.WinForms.Controls.SzButtonEx();
			this.nudPatternOn = new BizHawk.WinForms.Controls.SzNUDEx();
			this.nudPatternOff = new BizHawk.WinForms.Controls.SzNUDEx();
			this.lblPatternOn = new BizHawk.WinForms.Controls.LabelEx();
			this.lblPatternOff = new BizHawk.WinForms.Controls.LabelEx();
			this.flpDialogButtons = new BizHawk.WinForms.Controls.LocSzSingleRowFLP();
			this.flpDialog = new BizHawk.WinForms.Controls.LocSzSingleColumnFLP();
			this.flpPattern = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.lblPatternDesc = new BizHawk.WinForms.Controls.LabelEx();
			this.cbConsiderLag = new BizHawk.WinForms.Controls.CheckBoxEx();
			((System.ComponentModel.ISupportInitialize)(this.nudPatternOn)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudPatternOff)).BeginInit();
			this.flpDialogButtons.SuspendLayout();
			this.flpDialog.SuspendLayout();
			this.flpPattern.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnDialogOK
			// 
			this.btnDialogOK.Name = "btnDialogOK";
			this.btnDialogOK.Size = new System.Drawing.Size(75, 23);
			this.btnDialogOK.Text = "&OK";
			this.btnDialogOK.Click += new BizHawk.WinForms.Controls.ButtonClickEventHandler<BizHawk.WinForms.Controls.ButtonExBase>(this.btnDialogOK_Click);
			// 
			// btnDialogCancel
			// 
			this.btnDialogCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnDialogCancel.Name = "btnDialogCancel";
			this.btnDialogCancel.Size = new System.Drawing.Size(75, 23);
			this.btnDialogCancel.Text = "&Cancel";
			this.btnDialogCancel.Click += new BizHawk.WinForms.Controls.ButtonClickEventHandler<BizHawk.WinForms.Controls.ButtonExBase>(this.btnDialogCancel_Click);
			// 
			// nudPatternOn
			// 
			this.nudPatternOn.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.nudPatternOn.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudPatternOn.Name = "nudPatternOn";
			this.nudPatternOn.Size = new System.Drawing.Size(48, 20);
			this.nudPatternOn.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// nudPatternOff
			// 
			this.nudPatternOff.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.nudPatternOff.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudPatternOff.Name = "nudPatternOff";
			this.nudPatternOff.Size = new System.Drawing.Size(48, 20);
			this.nudPatternOff.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// lblPatternOn
			// 
			this.lblPatternOn.Name = "lblPatternOn";
			this.lblPatternOn.Text = "on,";
			// 
			// lblPatternOff
			// 
			this.lblPatternOff.Name = "lblPatternOff";
			this.lblPatternOff.Text = "off";
			// 
			// flpDialogButtons
			// 
			this.flpDialogButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.flpDialogButtons.Controls.Add(this.btnDialogOK);
			this.flpDialogButtons.Controls.Add(this.btnDialogCancel);
			this.flpDialogButtons.Location = new System.Drawing.Point(161, 61);
			this.flpDialogButtons.Name = "flpDialogButtons";
			this.flpDialogButtons.Size = new System.Drawing.Size(162, 29);
			// 
			// flpDialog
			// 
			this.flpDialog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flpDialog.Controls.Add(this.flpPattern);
			this.flpDialog.Controls.Add(this.cbConsiderLag);
			this.flpDialog.Location = new System.Drawing.Point(0, 0);
			this.flpDialog.Name = "flpDialog";
			this.flpDialog.Size = new System.Drawing.Size(323, 55);
			// 
			// flpPattern
			// 
			this.flpPattern.Controls.Add(this.lblPatternDesc);
			this.flpPattern.Controls.Add(this.nudPatternOn);
			this.flpPattern.Controls.Add(this.lblPatternOn);
			this.flpPattern.Controls.Add(this.nudPatternOff);
			this.flpPattern.Controls.Add(this.lblPatternOff);
			this.flpPattern.Name = "flpPattern";
			// 
			// lblPatternDesc
			// 
			this.lblPatternDesc.Name = "lblPatternDesc";
			this.lblPatternDesc.Text = "Pattern:";
			// 
			// cbConsiderLag
			// 
			this.cbConsiderLag.Name = "cbConsiderLag";
			this.cbConsiderLag.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
			this.cbConsiderLag.Text = "Take lag frames into account";
			// 
			// AutofireConfig
			// 
			this.AcceptButton = this.btnDialogOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnDialogCancel;
			this.ClientSize = new System.Drawing.Size(323, 90);
			this.Controls.Add(this.flpDialog);
			this.Controls.Add(this.flpDialogButtons);
			this.MaximizeBox = false;
			this.MinimumSize = new System.Drawing.Size(339, 129);
			this.Name = "AutofireConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Autofire Configuration";
			this.Load += new System.EventHandler(this.AutofireConfig_Load);
			((System.ComponentModel.ISupportInitialize)(this.nudPatternOn)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudPatternOff)).EndInit();
			this.flpDialogButtons.ResumeLayout(false);
			this.flpDialog.ResumeLayout(false);
			this.flpDialog.PerformLayout();
			this.flpPattern.ResumeLayout(false);
			this.flpPattern.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private BizHawk.WinForms.Controls.SzButtonEx btnDialogOK;
		private BizHawk.WinForms.Controls.SzButtonEx btnDialogCancel;
		private BizHawk.WinForms.Controls.SzNUDEx nudPatternOff;
		private BizHawk.WinForms.Controls.LabelEx lblPatternOn;
		private BizHawk.WinForms.Controls.LabelEx lblPatternOff;
		private BizHawk.WinForms.Controls.SingleRowFLP flpPattern;
		private BizHawk.WinForms.Controls.LocSzSingleColumnFLP flpDialog;
		private BizHawk.WinForms.Controls.LocSzSingleRowFLP flpDialogButtons;
		private BizHawk.WinForms.Controls.LabelEx lblPatternDesc;
		public BizHawk.WinForms.Controls.SzNUDEx nudPatternOn;
		private BizHawk.WinForms.Controls.CheckBoxEx cbConsiderLag;
	}
}
