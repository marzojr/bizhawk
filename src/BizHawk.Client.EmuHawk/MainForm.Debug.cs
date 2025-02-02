#nullable enable

#if DEBUG

using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ForDebugging;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private void AddDebugMenu()
		{
//			void OpenModal<T>()
//				where T : Form, new()
//			{
//				using T form = new();
//				this.ShowDialogAsChild(form);
//			}
			void OpenTool<T>() where T : class, IToolForm => Tools.Load<T>();
			ToolStripMenuItemEx firmwareAutopatchDebugItem = new() { Text = FirmwareAutopatchDebugToolForm.TOOL_NAME };
			firmwareAutopatchDebugItem.Click += (_, _) => OpenTool<FirmwareAutopatchDebugToolForm>();
			ToolStripMenuItemEx rcHashDebugItem = new() { Text = "Debug RC Hash" };
			rcHashDebugItem.Click += (_, _) => RCheevos.DebugHash();
			ToolStripMenuItemEx debugMenu = new()
			{
				DropDownItems =
				{
					new ToolStripMenuItemEx
					{
						DropDownItems =
						{
							firmwareAutopatchDebugItem,
						},
						Text = "Firmware",
					},
					new ToolStripMenuItemEx
					{
						DropDownItems =
						{
							rcHashDebugItem,
						},
						Text = "RCheevos",
					},
					new ToolStripSeparatorEx(),
					new DebugVSystemMenuItem(VSystemID.Raw.GB, VSystemID.Raw.GBC)
					{
						DropDownItems =
						{
							new DebugVSystemChildItem(
								"Debug SameBoy States",
								() => ((Sameboy) Emulator).DebugSameBoyState())
							{
								RequiresCore = CoreNames.Sameboy,
							},
						},
					},
					new DebugVSystemMenuItem(VSystemID.Raw.N64)
					{
						DropDownItems =
						{
							new DebugVSystemChildItem(N64RomByteswapToolForm.TOOL_NAME, OpenTool<N64RomByteswapToolForm>) { RequiresLoadedRom = false },
						},
					},
				},
				Image = Resources.Bug,
				Text = "&Debug Utilities",
			};
			debugMenu.DropDownOpened += (ddoSender, _) =>
			{
				var sysID = Emulator.SystemId;
				var coreName = Emulator.Attributes().CoreName;
				foreach (var item in ((ToolStripMenuItemEx) ddoSender).DropDownItems.OfType<DebugVSystemMenuItem>())
				{
					var groupEnabled = item.SysIDs.Contains(sysID);
					foreach (var child in item.DropDownItems.Cast<DebugVSystemChildItem>().Where(static child => child.RequiresLoadedRom)) // RequiresLoadedRom == false => leave Enabled as default true
					{
						child.Enabled = groupEnabled && (child.RequiresCore is null || child.RequiresCore == coreName);
					}
				}
			};
			HelpSubMenu.DropDownItems.Insert(0, debugMenu);
		}
	}
}
#endif
