using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MovieHeaderEditorMPR : Form
	{
		private readonly IBasicMovieInfo _movie;
		private readonly Config _config;

		public MovieHeaderEditorMPR(IBasicMovieInfo movie, Config config)
		{
			_movie = movie;
			_config = config;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
		}

		private void MovieHeaderEditor_Load(object sender, EventArgs e)
		{
			AuthorTextBox.Text = _movie.Author;
			EmulatorVersionTextBox.Text = _movie.EmulatorVersion;
			CoreTextBox.Text = _movie.Core;
			BoardNameTextBox.Text = _movie.BoardName;
			GameNameTextBox.Text = _movie.GameName;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_movie.Author = AuthorTextBox.Text;
			if (MakeDefaultCheckbox.Checked)
			{
				_config.DefaultAuthor = AuthorTextBox.Text;
			}

			_movie.EmulatorVersion = EmulatorVersionTextBox.Text;
			_movie.Core = CoreTextBox.Text;
			_movie.BoardName = BoardNameTextBox.Text;
			_movie.GameName = GameNameTextBox.Text;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void DefaultAuthorButton_Click(object sender, EventArgs e)
		{
			AuthorTextBox.Text = _config.DefaultAuthor;
		}
	}
}
