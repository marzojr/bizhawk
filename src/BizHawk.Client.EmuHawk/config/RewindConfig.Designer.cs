namespace BizHawk.Client.EmuHawk
{
	partial class RewindConfig
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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.RewindEnabledBox = new System.Windows.Forms.CheckBox();
			this.UseCompression = new System.Windows.Forms.CheckBox();
			this.label4 = new BizHawk.WinForms.Controls.LabelEx();
			this.BufferSizeUpDown = new System.Windows.Forms.NumericUpDown();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.StateSizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.FullnessLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.TargetRewindIntervalRadioButton = new System.Windows.Forms.RadioButton();
			this.TargetFrameLengthRadioButton = new System.Windows.Forms.RadioButton();
			this.locSingleRowFLP1 = new BizHawk.WinForms.Controls.LocSingleRowFLP();
			this.labelEx3 = new BizHawk.WinForms.Controls.LabelEx();
			this.labelEx2 = new BizHawk.WinForms.Controls.LabelEx();
			this.labelEx1 = new BizHawk.WinForms.Controls.LabelEx();
			this.cbDeltaCompression = new System.Windows.Forms.CheckBox();
			this.TargetFrameLengthNumeric = new System.Windows.Forms.NumericUpDown();
			this.TargetRewindIntervalNumeric = new System.Windows.Forms.NumericUpDown();
			this.EstTimeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.ApproxFramesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.RewindFramesUsedLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.rbStatesText = new System.Windows.Forms.RadioButton();
			this.rbStatesBinary = new System.Windows.Forms.RadioButton();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.btnResetCompression = new System.Windows.Forms.Button();
			this.trackBarCompression = new System.Windows.Forms.TrackBar();
			this.nudCompression = new System.Windows.Forms.NumericUpDown();
			this.groupBox7 = new System.Windows.Forms.GroupBox();
			this.label20 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.KbLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.BigScreenshotNumeric = new System.Windows.Forms.NumericUpDown();
			this.LowResLargeScreenshotsCheckbox = new System.Windows.Forms.CheckBox();
			this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.ScreenshotInStatesCheckbox = new System.Windows.Forms.CheckBox();
			this.label15 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label16 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.BackupSavestatesCheckbox = new System.Windows.Forms.CheckBox();
			this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SpeedMultiplierNumeric = new System.Windows.Forms.NumericUpDown();
			this.FastSpeedMultiplierNumeric = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.BufferSizeUpDown)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.locSingleRowFLP1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TargetFrameLengthNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TargetRewindIntervalNumeric)).BeginInit();
			this.groupBox6.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompression)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).BeginInit();
			this.groupBox7.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BigScreenshotNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SpeedMultiplierNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FastSpeedMultiplierNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(575, 470);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(656, 470);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// RewindEnabledBox
			// 
			this.RewindEnabledBox.AutoSize = true;
			this.RewindEnabledBox.Location = new System.Drawing.Point(15, 90);
			this.RewindEnabledBox.Name = "RewindEnabledBox";
			this.RewindEnabledBox.Size = new System.Drawing.Size(65, 17);
			this.RewindEnabledBox.TabIndex = 1;
			this.RewindEnabledBox.Text = "Enabled";
			this.RewindEnabledBox.UseVisualStyleBackColor = true;
			// 
			// UseCompression
			// 
			this.UseCompression.AutoSize = true;
			this.UseCompression.Location = new System.Drawing.Point(15, 240);
			this.UseCompression.Name = "UseCompression";
			this.UseCompression.Size = new System.Drawing.Size(324, 17);
			this.UseCompression.TabIndex = 5;
			this.UseCompression.Text = "Use zlib compression (economizes buffer usage at cost of CPU)";
			this.UseCompression.UseVisualStyleBackColor = true;
			this.UseCompression.CheckedChanged += new System.EventHandler(this.UseCompression_CheckedChanged);
			// 
			// label4
			// 
			this.label4.Margin = new System.Windows.Forms.Padding(0);
			this.label4.Name = "label4";
			this.label4.Text = "MB";
			// 
			// BufferSizeUpDown
			// 
			this.BufferSizeUpDown.Location = new System.Drawing.Point(25, 3);
			this.BufferSizeUpDown.Maximum = new decimal(new int[] {
			15,
			0,
			0,
			0});
			this.BufferSizeUpDown.Minimum = new decimal(new int[] {
			6,
			0,
			0,
			0});
			this.BufferSizeUpDown.Name = "BufferSizeUpDown";
			this.BufferSizeUpDown.Size = new System.Drawing.Size(52, 20);
			this.BufferSizeUpDown.TabIndex = 8;
			this.BufferSizeUpDown.Value = new decimal(new int[] {
			9,
			0,
			0,
			0});
			this.BufferSizeUpDown.ValueChanged += new System.EventHandler(this.BufferSizeUpDown_ValueChanged);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(12, 112);
			this.label3.Name = "label3";
			this.label3.Text = "Max buffer size:";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(12, 17);
			this.label1.Name = "label1";
			this.label1.Text = "Avg. State Size:";
			// 
			// StateSizeLabel
			// 
			this.StateSizeLabel.Location = new System.Drawing.Point(92, 17);
			this.StateSizeLabel.Name = "StateSizeLabel";
			this.StateSizeLabel.Text = "0 KB";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(67, 48);
			this.label6.Name = "label6";
			this.label6.Text = "Full:";
			// 
			// FullnessLabel
			// 
			this.FullnessLabel.Location = new System.Drawing.Point(94, 48);
			this.FullnessLabel.Name = "FullnessLabel";
			this.FullnessLabel.Text = "0%";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.label5);
			this.groupBox4.Controls.Add(this.label2);
			this.groupBox4.Controls.Add(this.FastSpeedMultiplierNumeric);
			this.groupBox4.Controls.Add(this.SpeedMultiplierNumeric);
			this.groupBox4.Controls.Add(this.TargetRewindIntervalRadioButton);
			this.groupBox4.Controls.Add(this.TargetFrameLengthRadioButton);
			this.groupBox4.Controls.Add(this.locSingleRowFLP1);
			this.groupBox4.Controls.Add(this.cbDeltaCompression);
			this.groupBox4.Controls.Add(this.TargetFrameLengthNumeric);
			this.groupBox4.Controls.Add(this.TargetRewindIntervalNumeric);
			this.groupBox4.Controls.Add(this.UseCompression);
			this.groupBox4.Controls.Add(this.RewindEnabledBox);
			this.groupBox4.Controls.Add(this.label3);
			this.groupBox4.Controls.Add(this.EstTimeLabel);
			this.groupBox4.Controls.Add(this.label11);
			this.groupBox4.Controls.Add(this.ApproxFramesLabel);
			this.groupBox4.Controls.Add(this.label8);
			this.groupBox4.Controls.Add(this.RewindFramesUsedLabel);
			this.groupBox4.Controls.Add(this.label7);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this.FullnessLabel);
			this.groupBox4.Controls.Add(this.label6);
			this.groupBox4.Controls.Add(this.StateSizeLabel);
			this.groupBox4.Location = new System.Drawing.Point(12, 12);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(371, 287);
			this.groupBox4.TabIndex = 2;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "RewindSettings";
			// 
			// TargetRewindIntervalRadioButton
			// 
			this.TargetRewindIntervalRadioButton.AutoSize = true;
			this.TargetRewindIntervalRadioButton.Location = new System.Drawing.Point(15, 162);
			this.TargetRewindIntervalRadioButton.Name = "TargetRewindIntervalRadioButton";
			this.TargetRewindIntervalRadioButton.Size = new System.Drawing.Size(210, 17);
			this.TargetRewindIntervalRadioButton.TabIndex = 49;
			this.TargetRewindIntervalRadioButton.TabStop = true;
			this.TargetRewindIntervalRadioButton.Text = "Rewinds every fixed number of frames: ";
			this.TargetRewindIntervalRadioButton.UseVisualStyleBackColor = true;
			// 
			// TargetFrameLengthRadioButton
			// 
			this.TargetFrameLengthRadioButton.AutoSize = true;
			this.TargetFrameLengthRadioButton.Location = new System.Drawing.Point(15, 138);
			this.TargetFrameLengthRadioButton.Name = "TargetFrameLengthRadioButton";
			this.TargetFrameLengthRadioButton.Size = new System.Drawing.Size(125, 17);
			this.TargetFrameLengthRadioButton.TabIndex = 48;
			this.TargetFrameLengthRadioButton.TabStop = true;
			this.TargetFrameLengthRadioButton.Text = "Desired frame length:";
			this.TargetFrameLengthRadioButton.UseVisualStyleBackColor = true;
			// 
			// locSingleRowFLP1
			// 
			this.locSingleRowFLP1.Controls.Add(this.labelEx3);
			this.locSingleRowFLP1.Controls.Add(this.BufferSizeUpDown);
			this.locSingleRowFLP1.Controls.Add(this.labelEx2);
			this.locSingleRowFLP1.Controls.Add(this.labelEx1);
			this.locSingleRowFLP1.Controls.Add(this.label4);
			this.locSingleRowFLP1.Location = new System.Drawing.Point(100, 107);
			this.locSingleRowFLP1.Name = "locSingleRowFLP1";
			// 
			// labelEx3
			// 
			this.labelEx3.Margin = new System.Windows.Forms.Padding(0);
			this.labelEx3.Name = "labelEx3";
			this.labelEx3.Text = "2 ^";
			// 
			// labelEx2
			// 
			this.labelEx2.Margin = new System.Windows.Forms.Padding(0);
			this.labelEx2.Name = "labelEx2";
			this.labelEx2.Text = "MB  =";
			// 
			// labelEx1
			// 
			this.labelEx1.Margin = new System.Windows.Forms.Padding(0);
			this.labelEx1.Name = "labelEx1";
			this.labelEx1.Text = "512";
			// 
			// cbDeltaCompression
			// 
			this.cbDeltaCompression.AutoSize = true;
			this.cbDeltaCompression.Location = new System.Drawing.Point(15, 263);
			this.cbDeltaCompression.Name = "cbDeltaCompression";
			this.cbDeltaCompression.Size = new System.Drawing.Size(332, 17);
			this.cbDeltaCompression.TabIndex = 35;
			this.cbDeltaCompression.Text = "Use delta compression (economizes buffer usage at cost of CPU)";
			this.cbDeltaCompression.UseVisualStyleBackColor = true;
			// 
			// TargetFrameLengthNumeric
			// 
			this.TargetFrameLengthNumeric.Location = new System.Drawing.Point(146, 138);
			this.TargetFrameLengthNumeric.Maximum = new decimal(new int[] {
			500000,
			0,
			0,
			0});
			this.TargetFrameLengthNumeric.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.TargetFrameLengthNumeric.Name = "TargetFrameLengthNumeric";
			this.TargetFrameLengthNumeric.Size = new System.Drawing.Size(52, 20);
			this.TargetFrameLengthNumeric.TabIndex = 21;
			this.TargetFrameLengthNumeric.Value = new decimal(new int[] {
			600,
			0,
			0,
			0});
			// 
			// TargetRewindIntervalNumeric
			// 
			this.TargetRewindIntervalNumeric.Location = new System.Drawing.Point(231, 162);
			this.TargetRewindIntervalNumeric.Maximum = new decimal(new int[] {
			500000,
			0,
			0,
			0});
			this.TargetRewindIntervalNumeric.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.TargetRewindIntervalNumeric.Name = "TargetRewindIntervalNumeric";
			this.TargetRewindIntervalNumeric.Size = new System.Drawing.Size(52, 20);
			this.TargetRewindIntervalNumeric.TabIndex = 21;
			this.TargetRewindIntervalNumeric.Value = new decimal(new int[] {
			5,
			0,
			0,
			0});
			// 
			// EstTimeLabel
			// 
			this.EstTimeLabel.Location = new System.Drawing.Point(273, 32);
			this.EstTimeLabel.Name = "EstTimeLabel";
			this.EstTimeLabel.Text = "0 min";
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(221, 32);
			this.label11.Name = "label11";
			this.label11.Text = "Est. Time:";
			// 
			// ApproxFramesLabel
			// 
			this.ApproxFramesLabel.Location = new System.Drawing.Point(273, 17);
			this.ApproxFramesLabel.Name = "ApproxFramesLabel";
			this.ApproxFramesLabel.Text = "0 frames";
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(209, 17);
			this.label8.Name = "label8";
			this.label8.Text = "Est. storage:";
			// 
			// RewindFramesUsedLabel
			// 
			this.RewindFramesUsedLabel.Location = new System.Drawing.Point(94, 32);
			this.RewindFramesUsedLabel.Name = "RewindFramesUsedLabel";
			this.RewindFramesUsedLabel.Text = "0";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(15, 32);
			this.label7.Name = "label7";
			this.label7.Text = "Frames Stored:";
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.rbStatesText);
			this.groupBox6.Controls.Add(this.rbStatesBinary);
			this.groupBox6.Location = new System.Drawing.Point(22, 78);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(215, 48);
			this.groupBox6.TabIndex = 4;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Type";
			// 
			// rbStatesText
			// 
			this.rbStatesText.AutoSize = true;
			this.rbStatesText.Location = new System.Drawing.Point(88, 18);
			this.rbStatesText.Name = "rbStatesText";
			this.rbStatesText.Size = new System.Drawing.Size(46, 17);
			this.rbStatesText.TabIndex = 1;
			this.rbStatesText.TabStop = true;
			this.rbStatesText.Text = "Text";
			this.rbStatesText.UseVisualStyleBackColor = true;
			// 
			// rbStatesBinary
			// 
			this.rbStatesBinary.AutoSize = true;
			this.rbStatesBinary.Location = new System.Drawing.Point(6, 18);
			this.rbStatesBinary.Name = "rbStatesBinary";
			this.rbStatesBinary.Size = new System.Drawing.Size(54, 17);
			this.rbStatesBinary.TabIndex = 1;
			this.rbStatesBinary.TabStop = true;
			this.rbStatesBinary.Text = "Binary";
			this.rbStatesBinary.UseVisualStyleBackColor = true;
			// 
			// btnResetCompression
			// 
			this.btnResetCompression.AutoSize = true;
			this.btnResetCompression.Location = new System.Drawing.Point(243, 34);
			this.btnResetCompression.Name = "btnResetCompression";
			this.btnResetCompression.Size = new System.Drawing.Size(27, 27);
			this.btnResetCompression.TabIndex = 3;
			this.toolTip1.SetToolTip(this.btnResetCompression, "Reset to default");
			this.btnResetCompression.UseVisualStyleBackColor = true;
			this.btnResetCompression.Click += new System.EventHandler(this.BtnResetCompression_Click);
			// 
			// trackBarCompression
			// 
			this.trackBarCompression.LargeChange = 1;
			this.trackBarCompression.Location = new System.Drawing.Point(22, 37);
			this.trackBarCompression.Maximum = 9;
			this.trackBarCompression.Name = "trackBarCompression";
			this.trackBarCompression.Size = new System.Drawing.Size(157, 45);
			this.trackBarCompression.TabIndex = 1;
			this.toolTip1.SetToolTip(this.trackBarCompression, "0 = None; 9 = Maximum");
			this.trackBarCompression.Value = 1;
			this.trackBarCompression.ValueChanged += new System.EventHandler(this.TrackBarCompression_ValueChanged);
			// 
			// nudCompression
			// 
			this.nudCompression.Location = new System.Drawing.Point(185, 37);
			this.nudCompression.Maximum = new decimal(new int[] {
			9,
			0,
			0,
			0});
			this.nudCompression.Name = "nudCompression";
			this.nudCompression.Size = new System.Drawing.Size(52, 20);
			this.nudCompression.TabIndex = 2;
			this.nudCompression.Value = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.nudCompression.ValueChanged += new System.EventHandler(this.NudCompression_ValueChanged);
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.label20);
			this.groupBox7.Controls.Add(this.KbLabel);
			this.groupBox7.Controls.Add(this.BigScreenshotNumeric);
			this.groupBox7.Controls.Add(this.LowResLargeScreenshotsCheckbox);
			this.groupBox7.Controls.Add(this.label13);
			this.groupBox7.Controls.Add(this.label14);
			this.groupBox7.Controls.Add(this.ScreenshotInStatesCheckbox);
			this.groupBox7.Controls.Add(this.label15);
			this.groupBox7.Controls.Add(this.label16);
			this.groupBox7.Controls.Add(this.BackupSavestatesCheckbox);
			this.groupBox7.Controls.Add(this.label12);
			this.groupBox7.Controls.Add(this.groupBox6);
			this.groupBox7.Controls.Add(this.btnResetCompression);
			this.groupBox7.Controls.Add(this.nudCompression);
			this.groupBox7.Controls.Add(this.trackBarCompression);
			this.groupBox7.Location = new System.Drawing.Point(389, 12);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(342, 408);
			this.groupBox7.TabIndex = 6;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Savestate Options";
			// 
			// label20
			// 
			this.label20.Location = new System.Drawing.Point(21, 291);
			this.label20.Name = "label20";
			this.label20.Text = "Use a low resolution screenshot for better save/load performance";
			// 
			// KbLabel
			// 
			this.KbLabel.Location = new System.Drawing.Point(276, 271);
			this.KbLabel.Name = "KbLabel";
			this.KbLabel.Text = "KB";
			// 
			// BigScreenshotNumeric
			// 
			this.BigScreenshotNumeric.Location = new System.Drawing.Point(212, 267);
			this.BigScreenshotNumeric.Maximum = new decimal(new int[] {
			8192,
			0,
			0,
			0});
			this.BigScreenshotNumeric.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.BigScreenshotNumeric.Name = "BigScreenshotNumeric";
			this.BigScreenshotNumeric.Size = new System.Drawing.Size(58, 20);
			this.BigScreenshotNumeric.TabIndex = 32;
			this.BigScreenshotNumeric.Value = new decimal(new int[] {
			128,
			0,
			0,
			0});
			// 
			// LowResLargeScreenshotsCheckbox
			// 
			this.LowResLargeScreenshotsCheckbox.AutoSize = true;
			this.LowResLargeScreenshotsCheckbox.Location = new System.Drawing.Point(21, 269);
			this.LowResLargeScreenshotsCheckbox.Name = "LowResLargeScreenshotsCheckbox";
			this.LowResLargeScreenshotsCheckbox.Size = new System.Drawing.Size(195, 17);
			this.LowResLargeScreenshotsCheckbox.TabIndex = 31;
			this.LowResLargeScreenshotsCheckbox.Text = "Low Res Screenshots on buffers >=";
			this.LowResLargeScreenshotsCheckbox.UseVisualStyleBackColor = true;
			// 
			// label13
			// 
			this.label13.Location = new System.Drawing.Point(21, 235);
			this.label13.Name = "label13";
			this.label13.Text = "black screen on the frame it is loaded.";
			// 
			// label14
			// 
			this.label14.Location = new System.Drawing.Point(19, 221);
			this.label14.Name = "label14";
			this.label14.Text = "Saves a screenshot and loads it on loadstate so there isn\'t a";
			// 
			// ScreenshotInStatesCheckbox
			// 
			this.ScreenshotInStatesCheckbox.AutoSize = true;
			this.ScreenshotInStatesCheckbox.Location = new System.Drawing.Point(22, 201);
			this.ScreenshotInStatesCheckbox.Name = "ScreenshotInStatesCheckbox";
			this.ScreenshotInStatesCheckbox.Size = new System.Drawing.Size(180, 17);
			this.ScreenshotInStatesCheckbox.TabIndex = 28;
			this.ScreenshotInStatesCheckbox.Text = "Save a screenshot in savestates";
			this.ScreenshotInStatesCheckbox.UseVisualStyleBackColor = true;
			this.ScreenshotInStatesCheckbox.CheckedChanged += new System.EventHandler(this.ScreenshotInStatesCheckbox_CheckedChanged);
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(39, 171);
			this.label15.Name = "label15";
			this.label15.Text = "before overwriting it.";
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(39, 158);
			this.label16.Name = "label16";
			this.label16.Text = "When set, the client will make a backup copy of a savestate";
			// 
			// BackupSavestatesCheckbox
			// 
			this.BackupSavestatesCheckbox.AutoSize = true;
			this.BackupSavestatesCheckbox.Location = new System.Drawing.Point(21, 138);
			this.BackupSavestatesCheckbox.Name = "BackupSavestatesCheckbox";
			this.BackupSavestatesCheckbox.Size = new System.Drawing.Size(119, 17);
			this.BackupSavestatesCheckbox.TabIndex = 25;
			this.BackupSavestatesCheckbox.Text = "Backup Savestates";
			this.BackupSavestatesCheckbox.UseVisualStyleBackColor = true;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(19, 21);
			this.label12.Name = "label12";
			this.label12.Text = "Compression Level";
			// 
			// SpeedMultiplierNumeric
			// 
			this.SpeedMultiplierNumeric.Location = new System.Drawing.Point(194, 188);
			this.SpeedMultiplierNumeric.Maximum = new decimal(new int[] {
			500000,
			0,
			0,
			0});
			this.SpeedMultiplierNumeric.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.SpeedMultiplierNumeric.Name = "SpeedMultiplierNumeric";
			this.SpeedMultiplierNumeric.Size = new System.Drawing.Size(52, 20);
			this.SpeedMultiplierNumeric.TabIndex = 7;
			this.SpeedMultiplierNumeric.Value = new decimal(new int[] {
			1,
			0,
			0,
			0});
			// 
			// FastSpeedMultiplierNumeric
			// 
			this.FastSpeedMultiplierNumeric.Location = new System.Drawing.Point(194, 214);
			this.FastSpeedMultiplierNumeric.Maximum = new decimal(new int[] {
			500000,
			0,
			0,
			0});
			this.FastSpeedMultiplierNumeric.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.FastSpeedMultiplierNumeric.Name = "FastSpeedMultiplierNumeric";
			this.FastSpeedMultiplierNumeric.Size = new System.Drawing.Size(52, 20);
			this.FastSpeedMultiplierNumeric.TabIndex = 62;
			this.FastSpeedMultiplierNumeric.Value = new decimal(new int[] {
			1,
			0,
			0,
			0});
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 190);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(176, 13);
			this.label2.TabIndex = 63;
			this.label2.Text = "Rewind speed (without fast foward):";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(12, 217);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(164, 13);
			this.label5.TabIndex = 64;
			this.label5.Text = "Rewind speed (with fast forward):";
			// 
			// RewindConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(741, 505);
			this.Controls.Add(this.groupBox7);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RewindConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Rewind & Savestate Cofiguration";
			this.Load += new System.EventHandler(this.RewindConfig_Load);
			((System.ComponentModel.ISupportInitialize)(this.BufferSizeUpDown)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.locSingleRowFLP1.ResumeLayout(false);
			this.locSingleRowFLP1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TargetFrameLengthNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TargetRewindIntervalNumeric)).EndInit();
			this.groupBox6.ResumeLayout(false);
			this.groupBox6.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBarCompression)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudCompression)).EndInit();
			this.groupBox7.ResumeLayout(false);
			this.groupBox7.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.BigScreenshotNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SpeedMultiplierNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FastSpeedMultiplierNumeric)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.CheckBox RewindEnabledBox;
		private System.Windows.Forms.CheckBox UseCompression;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx StateSizeLabel;
		private BizHawk.WinForms.Controls.LabelEx label4;
		private System.Windows.Forms.NumericUpDown BufferSizeUpDown;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private BizHawk.WinForms.Controls.LocLabelEx FullnessLabel;
		private System.Windows.Forms.GroupBox groupBox4;
		private BizHawk.WinForms.Controls.LocLabelEx RewindFramesUsedLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private BizHawk.WinForms.Controls.LocLabelEx ApproxFramesLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private BizHawk.WinForms.Controls.LocLabelEx EstTimeLabel;
				private BizHawk.WinForms.Controls.LocLabelEx label11;
				private System.Windows.Forms.GroupBox groupBox6;
				private System.Windows.Forms.RadioButton rbStatesText;
				private System.Windows.Forms.RadioButton rbStatesBinary;
				private System.Windows.Forms.ToolTip toolTip1;
				private System.Windows.Forms.TrackBar trackBarCompression;
				private System.Windows.Forms.NumericUpDown nudCompression;
				private System.Windows.Forms.Button btnResetCompression;
				private System.Windows.Forms.GroupBox groupBox7;
				private BizHawk.WinForms.Controls.LocLabelEx label12;
				private BizHawk.WinForms.Controls.LocLabelEx KbLabel;
				private System.Windows.Forms.NumericUpDown BigScreenshotNumeric;
				private System.Windows.Forms.CheckBox LowResLargeScreenshotsCheckbox;
				private BizHawk.WinForms.Controls.LocLabelEx label13;
				private BizHawk.WinForms.Controls.LocLabelEx label14;
				private System.Windows.Forms.CheckBox ScreenshotInStatesCheckbox;
				private BizHawk.WinForms.Controls.LocLabelEx label15;
				private BizHawk.WinForms.Controls.LocLabelEx label16;
				private System.Windows.Forms.CheckBox BackupSavestatesCheckbox;
				private BizHawk.WinForms.Controls.LocLabelEx label20;
		private System.Windows.Forms.NumericUpDown TargetFrameLengthNumeric;
		private System.Windows.Forms.NumericUpDown TargetRewindIntervalNumeric;
		private System.Windows.Forms.CheckBox cbDeltaCompression;
		private WinForms.Controls.LocSingleRowFLP locSingleRowFLP1;
		private WinForms.Controls.LabelEx labelEx3;
		private WinForms.Controls.LabelEx labelEx2;
		private WinForms.Controls.LabelEx labelEx1;
		private System.Windows.Forms.RadioButton TargetFrameLengthRadioButton;
		private System.Windows.Forms.RadioButton TargetRewindIntervalRadioButton;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown FastSpeedMultiplierNumeric;
		private System.Windows.Forms.NumericUpDown SpeedMultiplierNumeric;
	}
}