using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexColorsForm : Form
	{
		private readonly HexEditor _hexEditor;

		public HexColorsForm(HexEditor hexEditor)
		{
			_hexEditor = hexEditor;
			InitializeComponent();
		}

		private void HexColors_Form_Load(object sender, EventArgs e)
		{
			HexBackgrnd.BackColor = _hexEditor.Colors.Background;
			HexForegrnd.BackColor = _hexEditor.Colors.Foreground;
			HexMenubar.BackColor = _hexEditor.Colors.MenuBar;
			HexFreeze.BackColor = _hexEditor.Colors.Freeze;
			HexFreezeHL.BackColor = _hexEditor.Colors.HighlightFreeze;
			HexHighlight.BackColor = _hexEditor.Colors.Highlight;
			Hex00.BackColor = _hexEditor.Colors.Foreground00;
		}

		private void HexBackground_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.Background = colorDialog1.Color;
				_hexEditor.Header.BackColor = colorDialog1.Color;
				_hexEditor.MemoryViewerBox.BackColor = _hexEditor.Colors.Background;
				HexBackgrnd.BackColor = colorDialog1.Color;
			}
		}

		private void Hex00_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.Foreground00 = colorDialog1.Color;
				Hex00.BackColor = colorDialog1.Color;
			}
		}

		private void HexForeground_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.Foreground = colorDialog1.Color;
				_hexEditor.Header.ForeColor = colorDialog1.Color;
				_hexEditor.MemoryViewerBox.ForeColor = _hexEditor.Colors.Foreground;
				HexForegrnd.BackColor = colorDialog1.Color;
			}
		}

		private void HexMenuBar_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.MenuBar = colorDialog1.Color;
				_hexEditor.HexMenuStrip.BackColor = _hexEditor.Colors.MenuBar;
				HexMenubar.BackColor = colorDialog1.Color;
			}
		}

		private void HexHighlight_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.Highlight = colorDialog1.Color;
				HexHighlight.BackColor = colorDialog1.Color;
			}
		}

		private void HexFreeze_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.Freeze = colorDialog1.Color;
				HexFreeze.BackColor = colorDialog1.Color;
			}
		}

		private void HexFreezeHL_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_hexEditor.Colors.HighlightFreeze = colorDialog1.Color;
				HexFreezeHL.BackColor = colorDialog1.Color;
			}
		}
	}
}
