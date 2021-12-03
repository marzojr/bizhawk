#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

using static BizHawk.Common.CommctrlImports;

namespace BizHawk.Client.EmuHawk
{
	public static class ControlExtensions
	{
		/// <exception cref="ArgumentException"><typeparamref name="T"/> does not inherit <see cref="Enum"/></exception>
		public static void PopulateFromEnum<T>(this ComboBox box, T enumVal)
			where T : Enum
		{
			box.Items.Clear();
			box.Items.AddRange(typeof(T).GetEnumDescriptions().Cast<object>().ToArray());
			box.SelectedItem = enumVal.GetDescription();
		}

#if !NET6_0_OR_GREATER
		/// <summary>extension method to make <see cref="Control.Invoke(Delegate)"/> easier to use</summary>
		public static void Invoke(this Control control, Action action)
			=> control.Invoke(action);

		/// <summary>extension method to make <see cref="Control.BeginInvoke(Delegate)"/> easier to use</summary>
		public static void BeginInvoke(this Control control, Action action)
			=> control.BeginInvoke(action);
#endif

		public static ToolStripMenuItem ToColumnsMenu(this InputRoll inputRoll, Action changeCallback)
		{
			var menu = new ToolStripMenuItem
			{
				Name = "GeneratedColumnsSubMenu",
				Text = "Columns"
			};

			var columns = inputRoll.AllColumns;

			foreach (var column in columns)
			{
				var menuItem = new ToolStripMenuItem
				{
					Name = column.Name,
					Text = $"{column.Text} ({column.Name})",
					Checked = column.Visible,
					CheckOnClick = true,
					Tag = column.Name
				};

				menuItem.CheckedChanged += (o, ev) =>
				{
					var sender = (ToolStripMenuItem)o;
					columns.Find(c => c.Name == (string)sender.Tag).Visible = sender.Checked;
					columns.ColumnsChanged();
					changeCallback();
					inputRoll.Refresh();
				};

				menu.DropDownItems.Add(menuItem);
			}

			return menu;
		}

		public static Point ChildPointToScreen(this Control control, Control child)
		{
			return control.PointToScreen(new Point(child.Location.X, child.Location.Y));
		}

		public static void FollowMousePointer(this Form form)
		{
			var point = Cursor.Position;
			point.Offset(form.Width / -2, form.Height / -2);
			form.StartPosition = FormStartPosition.Manual;
			form.Location = point;
		}

		public static DialogResult ShowDialogOnScreen(this Form form)
		{
			var topLeft = new Point(
				Math.Max(0, form.Location.X),
				Math.Max(0, form.Location.Y));
			var screen = Screen.AllScreens.First(s => s.WorkingArea.Contains(topLeft));
			var w = screen.WorkingArea.Right - form.Bounds.Right;
			var h = screen.WorkingArea.Bottom - form.Bounds.Bottom;
			if (h < 0) topLeft.Y += h;
			if (w < 0) topLeft.X += w;
			form.SetDesktopLocation(topLeft.X, topLeft.Y);
			return form.ShowDialog();
		}

		public static Color Add(this Color color, int val)
		{
			var col = color.ToArgb();
			col += val;
			return Color.FromArgb(col);
		}

		/// <remarks>
		/// Due to the way this is written, using it in a foreach (as is done in SNESGraphicsDebugger)
		/// passes <c>Control</c> as the type parameter, meaning only properties on <see cref="Control"/> (and <see cref="Component"/>, etc.)
		/// will be processed. Why is there even a type param at all? I certainly don't know. --yoshi
		/// </remarks>
		public static T Clone<T>(this T controlToClone)
			where T : Control
		{
			PropertyInfo[] controlProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			Type t = controlToClone.GetType();
			var instance = (T) Activator.CreateInstance(t);

			t.GetProperty("AutoSize")?.SetMethod?.Invoke(instance, new object[] {false});

			for (int i = 0; i < 3; i++) // why 3 passes of this? --yoshi
			{
				foreach (var propInfo in controlProperties)
				{
					if (!propInfo.CanWrite)
					{
						continue;
					}

					if (propInfo.Name != "AutoSize" && propInfo.Name != "WindowTarget")
					{
						propInfo.SetValue(instance, propInfo.GetValue(controlToClone, null), null);
					}
				}
			}

			if (controlToClone is RetainedViewportPanel rvpToClone && instance is RetainedViewportPanel rvpCloned)
			{
				rvpCloned.SetBitmap((Bitmap) rvpToClone.GetBitmap().Clone());
			}

			return instance;
		}

		/// <summary>
		/// Converts the outdated IEnumerable Controls property to an <see cref="IEnumerable{T}"/> like .NET should have done a long time ago
		/// </summary>
		public static IEnumerable<Control> Controls(this Control control)
			=> control.Controls.Cast<Control>();

		public static IEnumerable<TabPage> TabPages(this TabControl tabControl)
		{
			return tabControl.TabPages.Cast<TabPage>();
		}

		public static void ReplaceDropDownItems(this ToolStripDropDownItem menu, params ToolStripItem[] items)
		{
			menu.DropDownItems.Clear();
			menu.DropDownItems.AddRange(items);
		}
	}

	public static class ListViewExtensions
	{
		/// <summary>
		/// Dumps the contents of the ListView into a tab separated list of lines
		/// </summary>
		public static string CopyItemsAsText(this ListView listViewControl)
		{
			var indexes = listViewControl.SelectedIndices;
			if (indexes.Count <= 0)
			{
				return "";
			}

			var sb = new StringBuilder();

			// walk over each selected item and subitem within it to generate a string from it
			foreach (int index in indexes)
			{
				foreach (ListViewItem.ListViewSubItem item in listViewControl.Items[index].SubItems)
				{
					if (!string.IsNullOrWhiteSpace(item.Text))
					{
						sb.Append(item.Text).Append('\t');
					}
				}

				// remove the last tab
				sb.Remove(sb.Length - 1, 1);

				sb.Append("\r\n");
			}

			// remove last newline
			sb.Length -= 2;


			return sb.ToString();
		}

		/// <exception cref="Win32Exception">unmanaged call failed</exception>
		public static void SetSortIcon(this ListView listViewControl, int columnIndex, SortOrder order)
		{
			if (OSTailoredCode.IsUnixHost)
			{
				return;
			}

			var columnHeader = WmImports.SendMessageW(listViewControl.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
			for (int columnNumber = 0, l = listViewControl.Columns.Count; columnNumber < l; columnNumber++)
			{
				var columnPtr = new IntPtr(columnNumber);
				var item = new HDITEMW { mask = HDITEMW.Mask.Format };
				if (SendMessageW(columnHeader, HDM_GETITEMW, columnPtr, ref item) == IntPtr.Zero)
				{
					throw new Win32Exception();
				}

				if (columnNumber != columnIndex || order == SortOrder.None)
				{
					item.fmt &= ~HDITEMW.Format.SortDown & ~HDITEMW.Format.SortUp;
				}
				// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
				else switch (order)
				{
					case SortOrder.Ascending:
						item.fmt &= ~HDITEMW.Format.SortDown;
						item.fmt |= HDITEMW.Format.SortUp;
						break;
					case SortOrder.Descending:
						item.fmt &= ~HDITEMW.Format.SortUp;
						item.fmt |= HDITEMW.Format.SortDown;
						break;
				}

				if (SendMessageW(columnHeader, HDM_SETITEMW, columnPtr, ref item) == IntPtr.Zero)
				{
					throw new Win32Exception();
				}
			}
		}

		public static bool IsOk(this DialogResult dialogResult)
		{
			return dialogResult == DialogResult.OK;
		}

		/// <summary>
		/// Sets the desired effect if data is present, else None
		/// </summary>
		public static void Set(this DragEventArgs e, DragDropEffects effect)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
				? effect
				: DragDropEffects.None;
		}

		public static Bitmap ToBitMap(this Control control)
		{
			var b = new Bitmap(control.Width, control.Height);
			var rect = new Rectangle(new Point(0, 0), control.Size);
			control.DrawToBitmap(b, rect);
			return b;
		}

		public static void ToClipBoard(this Bitmap bitmap)
		{
			using var img = bitmap;
			Clipboard.SetImage(img);
		}

		public static void SaveAsFile(this Bitmap bitmap, IGameInfo game, string suffix, string systemId, PathEntryCollection paths, IDialogParent parent)
		{
			var result = parent.ShowFileSaveDialog(
				discardCWDChange: true,
				filter: FilesystemFilterSet.Screenshots,
				initDir: paths.ScreenshotAbsolutePathFor(systemId),
				initFileName: $"{game.FilesystemSafeName()}-{suffix}");
			if (result is null) return;
			FileInfo file = new(result);
			string extension = file.Extension.ToUpperInvariant();
			ImageFormat i = extension switch
			{
				".BMP" => ImageFormat.Bmp,
				_ => ImageFormat.Png,
			};
			bitmap.Save(file.FullName, i);
		}

		public static void SetDistanceOrDefault(this SplitContainer splitter, int distance, int defaultDistance)
		{
			if (distance > 0)
			{
				try
				{
					splitter.SplitterDistance = distance;
				}
				catch (Exception)
				{
					splitter.SplitterDistance = defaultDistance;
				}
			}
		}

		public static bool IsPressed(this KeyEventArgs e, Keys key)
			=> !e.Alt && !e.Control && !e.Shift && e.KeyCode == key;

		public static bool IsShift(this KeyEventArgs e, Keys key)
			=> !e.Alt && !e.Control && e.Shift && e.KeyCode == key;

		public static bool IsCtrl(this KeyEventArgs e, Keys key)
			=> !e.Alt && e.Control && !e.Shift && e.KeyCode == key;

		public static bool IsAlt(this KeyEventArgs e, Keys key)
			=> e.Alt && !e.Control && !e.Shift && e.KeyCode == key;

		public static bool IsCtrlShift(this KeyEventArgs e, Keys key)
			=> !e.Alt && e.Control && e.Shift && e.KeyCode == key;

		/// <summary>
		/// Changes the description height area to match the rows needed for the largest description in the list
		/// </summary>
		public static void AdjustDescriptionHeightToFit(this PropertyGrid grid)
		{
			try
			{
				int maxLength = 0;
				string desc = "";

				foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(grid.SelectedObject))
				{
					var s = property.Description;
					if (s != null && s.Length > maxLength)
					{
						maxLength = s.Length;
						desc = s;
					}
				}

				foreach (Control control in grid.Controls)
				{
					if (control.GetType().Name == "DocComment")
					{
						var field = control.GetType().GetField("userSized", BindingFlags.Instance | BindingFlags.NonPublic);
						field?.SetValue(control, true);
						using var label = new Label();
						var maxSize = new Size(grid.Width - 9, 999999);
						control.Height = label.Height + TextRenderer.MeasureText(desc, control.Font, maxSize, TextFormatFlags.WordBreak).Height;
						return;
					}
				}
			}
			catch
			{
				// Eat it
			}
		}
	}
}
