using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public sealed class TAStudioColorSettingsFormMPR : Form
	{
		public TAStudioColorSettingsFormMPR(TAStudioPaletteMPR initPalette, Action<TAStudioPalette> save)
		{
			Dictionary<string, Color> colours = new();
			void Init(TAStudioPaletteMPR fromPalette)
			{
//				colours["currentFrame_FrameCol"] = fromPalette.CurrentFrame_FrameCol;
				colours["currentFrame_InputLog"] = fromPalette.CurrentFrame_InputLog;
				colours["greenZone_FrameCol"] = fromPalette.GreenZone_FrameCol;
				colours["greenZone_InputLog"] = fromPalette.GreenZone_InputLog;
				colours["greenZone_InputLog_Stated"] = fromPalette.GreenZone_InputLog_Stated;
				colours["greenZone_InputLog_Invalidated"] = fromPalette.GreenZone_InputLog_Invalidated;
				colours["lagZone_FrameCol"] = fromPalette.LagZone_FrameCol;
				colours["lagZone_InputLog"] = fromPalette.LagZone_InputLog;
				colours["lagZone_InputLog_Stated"] = fromPalette.LagZone_InputLog_Stated;
				colours["lagZone_InputLog_Invalidated"] = fromPalette.LagZone_InputLog_Invalidated;
				colours["marker_FrameCol"] = fromPalette.Marker_FrameCol;
				colours["analogEdit_Col"] = fromPalette.AnalogEdit_Col;
			}
			Init(initPalette);

			ColorDialog picker = new() { FullOpen = true };
			Size panelSize = new(20, 20);
			SingleRowFLP Row(string key, string labelText) // can't use ref here because those aren't captured in closures :(
			{
				Panel panel = new() { BackColor = colours[key], BorderStyle = BorderStyle.FixedSingle, Size = panelSize, Tag = key };
				panel.Click += (_, _) =>
				{
					picker.Color = colours[key];
					if (picker.ShowDialog().IsOk()) panel.BackColor = colours[key] = picker.Color;
				};
				return new() { Controls = { panel, new LabelEx { Text = labelText } } };
			}
			SingleColumnFLP flpPanels = new()
			{
				Controls =
				{
//					Row("currentFrame_FrameCol", "CurrentFrame: FrameCol"),
					Row("currentFrame_InputLog", "Emulated Frame Cursor"),
					Row("greenZone_FrameCol", "Frame# Column"),
					Row("greenZone_InputLog", "Input Log"),
					Row("greenZone_InputLog_Stated", "Savestate"),
					Row("greenZone_InputLog_Invalidated", "Invalidated"),
					Row("lagZone_FrameCol", "Frame# Column (Lag)"),
					Row("lagZone_InputLog", "Input Log (Lag)"),
					Row("lagZone_InputLog_Stated", "Savestate (Lag)"),
					Row("lagZone_InputLog_Invalidated", "Invalidated (Lag)"),
					Row("marker_FrameCol", "Marker"),
					Row("analogEdit_Col", "Analog Edit Mode"),
				},
			};

			Size btnSize = new(75, 23);
			SzButtonEx btnOK = new() { Size = btnSize, Text = "OK" };
			btnOK.Click += (_, _) =>
			{
				save(new(
//					currentFrame_FrameCol: colours["currentFrame_FrameCol"],
					currentFrame_InputLog: colours["currentFrame_InputLog"],
					greenZone_FrameCol: colours["greenZone_FrameCol"],
					greenZone_InputLog: colours["greenZone_InputLog"],
					greenZone_InputLog_Stated: colours["greenZone_InputLog_Stated"],
					greenZone_InputLog_Invalidated: colours["greenZone_InputLog_Invalidated"],
					lagZone_FrameCol: colours["lagZone_FrameCol"],
					lagZone_InputLog: colours["lagZone_InputLog"],
					lagZone_InputLog_Stated: colours["lagZone_InputLog_Stated"],
					lagZone_InputLog_Invalidated: colours["lagZone_InputLog_Invalidated"],
					marker_FrameCol: colours["marker_FrameCol"],
					analogEdit_Col: colours["analogEdit_Col"]));
				Close();
			};
			SzButtonEx btnCancel = new() { Size = btnSize, Text = "Cancel" };
			btnCancel.Click += (_, _) => Close();
			SzButtonEx btnDefaults = new() { Size = btnSize, Text = "Defaults" };
			btnDefaults.Click += (_, _) =>
			{
				Init(TAStudioPaletteMPR.Default);
				foreach (var panel in flpPanels.Controls.Cast<SingleRowFLP>().Select(flp => (Panel)flp.Controls[0]))
				{
					panel.BackColor = colours[(string) panel.Tag];
				}
			};
			SingleRowFLP flpButtons = new() { Controls = { btnOK, btnCancel, btnDefaults } };
			((FlowLayoutPanel) flpButtons).FlowDirection = FlowDirection.RightToLeft; // why did I disable this

			SuspendLayout();
			ClientSize = new(240, 320);
			Text = "Edit TAStudio Colors";
			Controls.Add(new SingleColumnFLP { Controls = { flpButtons, flpPanels } });
			ResumeLayout();
		}
	}
}
