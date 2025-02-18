using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PatternsFormMPR : Form
	{
		private readonly TAStudioMPR _tastudio;

		private readonly List<int> _counts = new List<int>();
		private readonly List<string> _values = new List<string>();
		private int _loopAt;
		private bool _updating;

		private string SelectedButton => ButtonBox.Text;

		private bool IsBool => _tastudio.MovieSession.MovieController.Definition.BoolButtons.Contains(SelectedButton);

		public PatternsFormMPR(TAStudioMPR owner)
		{
			InitializeComponent();
			_tastudio = owner;

			foreach (var button in _tastudio.MovieSession.MovieController.Definition.BoolButtons)
			{
				ButtonBox.Items.Add(button);
			}

			foreach (var button in _tastudio.MovieSession.MovieController.Definition.Axes.Keys)
			{
				ButtonBox.Items.Add(button);
			}
		}

		private void PatternsForm_Load(object sender, EventArgs e)
		{
			ButtonBox.SelectedIndex = 0;
		}

		private void ButtonBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			GetPattern();
			UpdateDisplay();

			if (IsBool)
			{
				OnOffBox.Visible = true;
				ValueNum.Visible = false;
			}
			else
			{
				ValueNum.Visible = true;
				OnOffBox.Visible = false;
			}

			CountNum.Value = _counts[0];
		}

		private void PatternList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_updating)
			{
				UpdateDisplay();
			}
		}

		private void InsertButton_Click(object sender, EventArgs e)
		{
			_counts.Insert(PatternList.SelectedIndex, 1);
			string defaultStr = "false";
			if (!IsBool)
			{
				defaultStr = _tastudio.MovieSession.MovieController.Definition.Axes[SelectedButton].Neutral.ToString();
			}

			_values.Insert(PatternList.SelectedIndex, defaultStr);

			UpdatePattern();
			UpdateDisplay();
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			if (PatternList.SelectedIndex >= _counts.Count)
			{
				return;
			}
			_counts.RemoveAt(PatternList.SelectedIndex);
			_values.RemoveAt(PatternList.SelectedIndex);
			UpdatePattern();
			UpdateDisplay();
		}

		private void LagBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdatePattern();
		}

		private void ValueNum_ValueChanged(object sender, EventArgs e)
		{
			if (_updating || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex >= _counts.Count)
			{
				return;
			}

			_values[PatternList.SelectedIndex] = ((int) ValueNum.Value).ToString(NumberFormatInfo.InvariantInfo);
			UpdatePattern();
			UpdateDisplay();
		}

		private void OnOffBox_CheckedChanged(object sender, EventArgs e)
		{
			if (_updating || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex >= _counts.Count)
			{
				return;
			}

			_values[PatternList.SelectedIndex] = OnOffBox.Checked.ToString();
			UpdatePattern();
			UpdateDisplay();
		}

		private void CountNum_ValueChanged(object sender, EventArgs e)
		{
			if (_updating || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex > _counts.Count)
			{
				return;
			}

			if (PatternList.SelectedIndex == _counts.Count)
			{
				_loopAt = (int)CountNum.Value;
			}
			else
			{
				// repeating zero times is not allowed
				if ((int)CountNum.Value == 0)
				{
					CountNum.Value = 1;
				}

				_counts[PatternList.SelectedIndex] = (int)CountNum.Value;
			}

			UpdatePattern();
			UpdateDisplay();
		}

		private void UpdateDisplay()
		{
			_updating = true;
			PatternList.SuspendLayout();

			int oldIndex = PatternList.SelectedIndex;
			if (oldIndex == -1)
			{
				oldIndex = 0;
			}

			PatternList.Items.Clear();
			int index = 0;
			for (int i = 0; i < _counts.Count; i++)
			{
				string str = $"{index}: ";
				if (IsBool)
				{
					str += _values[i][0] == 'T' ? "On" : "Off";
				}
				else
				{
					str += _values[i];
				}

				PatternList.Items.Add($"{str}\t(x{_counts[i]})");
				index += _counts[i];
			}

			PatternList.Items.Add($"Loop to: {_loopAt}");

			if (oldIndex >= PatternList.Items.Count)
			{
				oldIndex = PatternList.Items.Count - 1;
			}

			PatternList.SelectedIndex = oldIndex;

			if (PatternList.SelectedIndex != -1 && PatternList.SelectedIndex < _values.Count)
			{
				index = _tastudio.MovieSession.MovieController.Definition.BoolButtons.IndexOf(SelectedButton);

				if (index != -1)
				{
					LagBox.Checked = _tastudio.BoolPatterns[index].SkipsLag;
					OnOffBox.Checked = _values[PatternList.SelectedIndex][0] == 'T';
					CountNum.Value = _counts[PatternList.SelectedIndex];
				}
				else
				{
					index = _tastudio.MovieSession.MovieController.Definition.Axes.IndexOf(SelectedButton);

					LagBox.Checked = _tastudio.AxisPatterns[index].SkipsLag;
					ValueNum.Value = int.Parse(_values[PatternList.SelectedIndex]);
					CountNum.Value = _counts[PatternList.SelectedIndex];
				}
			}
			else if (PatternList.SelectedIndex == _values.Count)
			{
				CountNum.Value = _loopAt;
			}

			PatternList.ResumeLayout();
			_updating = false;
		}

		private void UpdatePattern()
		{
			int index = _tastudio.MovieSession.MovieController.Definition.BoolButtons.IndexOf(SelectedButton);

			if (index != -1)
			{
				var p = new List<bool>();
				for (int i = 0; i < _counts.Count; i++)
				{
					for (int c = 0; c < _counts[i]; c++)
					{
						p.Add(Convert.ToBoolean(_values[i]));
					}
				}

				_tastudio.BoolPatterns[index] = new AutoPatternBool(p.ToArray(), LagBox.Checked, 0, _loopAt);
			}
			else
			{
				index = _tastudio.MovieSession.MovieController.Definition.Axes.IndexOf(SelectedButton);

				var p = new List<int>();
				for (int i = 0; i < _counts.Count; i++)
				{
					for (int c = 0; c < _counts[i]; c++)
					{
						p.Add(int.Parse(_values[i]));
					}
				}

				_tastudio.AxisPatterns[index] = new AutoPatternAxis(p.ToArray(), LagBox.Checked, 0, _loopAt);
			}

			_tastudio.UpdateAutoFire(SelectedButton, null);
		}

		private void GetPattern()
		{
			int index = _tastudio.MovieSession.MovieController.Definition.BoolButtons.IndexOf(SelectedButton);

			if (index != -1)
			{
				bool[] p = _tastudio.BoolPatterns[index].Pattern;
				bool lastValue = p[0];
				_counts.Clear();
				_values.Clear();
				_counts.Add(1);
				_values.Add(lastValue.ToString());
				for (int i = 1; i < p.Length; i++)
				{
					if (p[i] == lastValue)
					{
						_counts[_counts.Count - 1]++;
					}
					else
					{
						_counts.Add(1);
						_values.Add(p[i].ToString());
						lastValue = p[i];
					}
				}

				_loopAt = _tastudio.BoolPatterns[index].Loop;
			}
			else
			{
				index = _tastudio.MovieSession.MovieController.Definition.Axes.IndexOf(SelectedButton);

				var p = _tastudio.AxisPatterns[index].Pattern;
				var lastValue = p[0];
				_counts.Clear();
				_values.Clear();
				_counts.Add(1);
				_values.Add(lastValue.ToString());
				for (int i = 1; i < p.Length; i++)
				{
					if (p[i] == lastValue)
					{
						_counts[_counts.Count - 1]++;
					}
					else
					{
						_counts.Add(1);
						_values.Add(p[i].ToString());
						lastValue = p[i];
					}
				}

				_loopAt = _tastudio.AxisPatterns[index].Loop;
			}
		}
	}
}
