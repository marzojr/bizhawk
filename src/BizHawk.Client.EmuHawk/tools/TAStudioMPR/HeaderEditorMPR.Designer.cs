using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MovieHeaderEditorMPR
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

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OkBtn = new System.Windows.Forms.Button();
			this.AuthorTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DefaultAuthorButton = new System.Windows.Forms.Button();
			this.MakeDefaultCheckbox = new System.Windows.Forms.CheckBox();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.EmulatorVersionTextBox = new System.Windows.Forms.TextBox();
			this.CoreTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.BoardNameTextBox = new System.Windows.Forms.TextBox();
			this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.GameNameTextBox = new System.Windows.Forms.TextBox();
			this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(249, 238);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 0;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(183, 238);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 1;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// AuthorTextBox
			// 
			this.AuthorTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.AuthorTextBox.Location = new System.Drawing.Point(90, 20);
			this.AuthorTextBox.Name = "AuthorTextBox";
			this.AuthorTextBox.Size = new System.Drawing.Size(162, 20);
			this.AuthorTextBox.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(38, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Author";
			// 
			// DefaultAuthorButton
			// 
			this.DefaultAuthorButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DefaultAuthorButton.Location = new System.Drawing.Point(258, 19);
			this.DefaultAuthorButton.Name = "DefaultAuthorButton";
			this.DefaultAuthorButton.Size = new System.Drawing.Size(51, 23);
			this.DefaultAuthorButton.TabIndex = 4;
			this.DefaultAuthorButton.Text = "Default";
			this.DefaultAuthorButton.UseVisualStyleBackColor = true;
			this.DefaultAuthorButton.Click += new System.EventHandler(this.DefaultAuthorButton_Click);
			// 
			// MakeDefaultCheckbox
			// 
			this.MakeDefaultCheckbox.AutoSize = true;
			this.MakeDefaultCheckbox.Location = new System.Drawing.Point(108, 43);
			this.MakeDefaultCheckbox.Name = "MakeDefaultCheckbox";
			this.MakeDefaultCheckbox.Size = new System.Drawing.Size(88, 17);
			this.MakeDefaultCheckbox.TabIndex = 5;
			this.MakeDefaultCheckbox.Text = "Make default";
			this.MakeDefaultCheckbox.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 80);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(66, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Emu Version";
			// 
			// EmulatorVersionTextBox
			// 
			this.EmulatorVersionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.EmulatorVersionTextBox.Location = new System.Drawing.Point(90, 76);
			this.EmulatorVersionTextBox.Name = "EmulatorVersionTextBox";
			this.EmulatorVersionTextBox.Size = new System.Drawing.Size(162, 20);
			this.EmulatorVersionTextBox.TabIndex = 7;
			// 
			// CoreTextBox
			// 
			this.CoreTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CoreTextBox.Location = new System.Drawing.Point(90, 106);
			this.CoreTextBox.Name = "CoreTextBox";
			this.CoreTextBox.Size = new System.Drawing.Size(162, 20);
			this.CoreTextBox.TabIndex = 10;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 110);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(29, 13);
			this.label4.TabIndex = 11;
			this.label4.Text = "Core";
			// 
			// BoardNameTextBox
			// 
			this.BoardNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BoardNameTextBox.Location = new System.Drawing.Point(90, 136);
			this.BoardNameTextBox.Name = "BoardNameTextBox";
			this.BoardNameTextBox.Size = new System.Drawing.Size(162, 20);
			this.BoardNameTextBox.TabIndex = 12;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(12, 140);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(64, 13);
			this.label5.TabIndex = 13;
			this.label5.Text = "Board name";
			// 
			// GameNameTextBox
			// 
			this.GameNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.GameNameTextBox.Location = new System.Drawing.Point(90, 166);
			this.GameNameTextBox.Name = "GameNameTextBox";
			this.GameNameTextBox.Size = new System.Drawing.Size(162, 20);
			this.GameNameTextBox.TabIndex = 14;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(12, 170);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(64, 13);
			this.label6.TabIndex = 15;
			this.label6.Text = "Game name";
			// 
			// MovieHeaderEditor
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(321, 273);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.GameNameTextBox);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.BoardNameTextBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.CoreTextBox);
			this.Controls.Add(this.EmulatorVersionTextBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.MakeDefaultCheckbox);
			this.Controls.Add(this.DefaultAuthorButton);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.AuthorTextBox);
			this.Controls.Add(this.OkBtn);
			this.Controls.Add(this.CancelBtn);
			this.MinimumSize = new System.Drawing.Size(150, 311);
			this.Name = "MovieHeaderEditor";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Header Info";
			this.Load += new System.EventHandler(this.MovieHeaderEditor_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private Button CancelBtn;
		private Button OkBtn;
		private TextBox AuthorTextBox;
		private Label label1;
		private Button DefaultAuthorButton;
		private CheckBox MakeDefaultCheckbox;
		private Label label2;
		private TextBox EmulatorVersionTextBox;
		private TextBox CoreTextBox;
		private Label label4;
		private TextBox BoardNameTextBox;
		private Label label5;
		private TextBox GameNameTextBox;
		private Label label6;
	}
}
