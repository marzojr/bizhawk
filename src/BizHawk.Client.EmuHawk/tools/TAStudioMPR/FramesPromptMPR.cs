using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class FramesPromptMPR : Form
	{
		public FramesPromptMPR()
		{
			InitializeComponent();
		}

		public FramesPromptMPR(string headMessage, string bodyMessage)
		{
			InitializeComponent();

			this.Text = headMessage;
			this.label1.Text = bodyMessage;
		}

		public int Frames => NumFramesBox.ToRawInt() ?? 0;

		private void FramesPrompt_Load(object sender, EventArgs e)
		{
			NumFramesBox.Select();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
