namespace BizHawk.Client.EmuHawk
{
	partial class GreenzoneSettingsMPR
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
			this.DefaultsButton = new System.Windows.Forms.Button();
			this.SettingsPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(263, 333);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 3;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(329, 333);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 2;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// DefaultsButton
			// 
			this.DefaultsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DefaultsButton.Location = new System.Drawing.Point(12, 333);
			this.DefaultsButton.Name = "DefaultsButton";
			this.DefaultsButton.Size = new System.Drawing.Size(101, 23);
			this.DefaultsButton.TabIndex = 4;
			this.DefaultsButton.Text = "Restore &Defaults";
			this.DefaultsButton.UseVisualStyleBackColor = true;
			this.DefaultsButton.Click += new System.EventHandler(this.DefaultsButton_Click);
			// 
			// SettingsPropertyGrid
			// 
			this.SettingsPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SettingsPropertyGrid.Location = new System.Drawing.Point(12, 8);
			this.SettingsPropertyGrid.Name = "SettingsPropertyGrid";
			this.SettingsPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this.SettingsPropertyGrid.Size = new System.Drawing.Size(376, 319);
			this.SettingsPropertyGrid.TabIndex = 5;
			this.SettingsPropertyGrid.ToolbarVisible = false;
			// 
			// GreenzoneSettings
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(400, 368);
			this.Controls.Add(this.SettingsPropertyGrid);
			this.Controls.Add(this.DefaultsButton);
			this.Controls.Add(this.OkBtn);
			this.Controls.Add(this.CancelBtn);
			this.Name = "GreenzoneSettings";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Default Savestate History Settings";
			this.Load += new System.EventHandler(this.GreenzoneSettings_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button DefaultsButton;
		private System.Windows.Forms.PropertyGrid SettingsPropertyGrid;
	}
}