using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

// TODO: There are some bad interactions between ScrollToIndex and MakeIndexVisible that are preventing things from working as intended.
//       But, the current behaviour is ok for now for what it is used for.

namespace BizHawk.Client.EmuHawk
{
	// Row width depends on font size and padding
	// Column width is specified in column headers
	// Row width is specified for horizontal orientation
	public partial class InputRoll : Control
	{
		private readonly IControlRenderer _renderer;

		private readonly CellList _selectedItems = new();

		// scrollbar location(s) are calculated later (e.g. on resize)
		private readonly VScrollBar _vBar = new VScrollBar { Visible = false };
		private readonly HScrollBar _hBar = new HScrollBar { Visible = false };

		private readonly Timer _hoverTimer = new Timer();
		private readonly byte[] _lagFrames = new byte[256]; // Large enough value that it shouldn't ever need resizing. // apparently not large enough for 4K

		private Color _backColor;

		public override Color BackColor
		{
			get => _backColor;
			set => base.BackColor = _backColor = value;
		}

		private Color _foreColor;

		public override Color ForeColor
		{
			get => _foreColor;
			set => base.ForeColor = _foreColor = value;
		}

		private RollColumns _columns = new RollColumns();
		private bool _horizontalOrientation;
		private bool _programmaticallyUpdatingScrollBarValues;

		private int _rowCount;
		private SizeF _charSize;

		private int[] _horizontalColumnTops; // Updated on paint, contains one extra item to allow inference of last column height

		private RollColumn/*?*/ _columnDown;

		private RollColumn/*?*/ _columnResizing;

		private int? _currentX;
		private int? _currentY;

		private Cell _lastCell; // The previous cell the mouse was in

		private int _drawHeight;
		private int _drawWidth;

		// Hiding lag frames (Mainly intended for < 60fps play.)
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int LagFramesToHide { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool HideWasLagFrames { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the control will respond to right-click events with a context menu
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool AllowRightClickSelection { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether or not Home and End will navigate to the beginning or end of the list
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool AllowMassNavigationShortcuts { get; set; } = true;

		[Category("Behavior")]
		public bool LetKeysModifySelection { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool SuspendHotkeys { get; set; }

		public InputRoll()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			// Deep in the bowels of winform documentation we discover these are necessary if you want your control to be able to have focus
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.UserMouse, true);

			_renderer = new GdiPlusRenderer(Font);

			UpdateCellSize();

			_vBar.SmallChange = CellHeight;
			_vBar.LargeChange = CellHeight * 20;

			_hBar.SmallChange = CellWidth;
			_hBar.LargeChange = 20;

			Controls.Add(_vBar);
			Controls.Add(_hBar);

			_vBar.ValueChanged += VerticalBar_ValueChanged;
			_hBar.ValueChanged += HorizontalBar_ValueChanged;

			RecalculateScrollBars();
			_columns.ChangedCallback = ColumnChangedCallback;

			_hoverTimer.Interval = 750;
			_hoverTimer.Tick += HoverTimerEventProcessor;
			_hoverTimer.Stop();

			_backColor = Color.White;
			_foreColor = Color.Black;
		}

		private void HoverTimerEventProcessor(object sender, EventArgs e)
		{
			_hoverTimer.Stop();

			CellHovered?.Invoke(this, new CellEventArgs(_lastCell, CurrentCell));
		}

		protected override void Dispose(bool disposing)
		{
			_renderer.Dispose();
			base.Dispose(disposing);
		}

		public void ExpandColumnToFitText(string columnName, string text)
		{
			var column = AllColumns.SingleOrDefault(c => c.Name == columnName);
			if (column != null)
			{
				using var g = CreateGraphics();
				using (_renderer.LockGraphics(g))
				{
					var strLength = (int)_renderer.MeasureString(text, Font).Width + (CellWidthPadding * 2);
					if (column.Width < strLength)
					{
						column.Width = strLength;
						AllColumns.ColumnsChanged();
						Refresh();
					}
				}
			}
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			if (IsHoveringOnColumnEdge)
			{
				if (HorizontalOrientation)
				{
					// TODO
				}
				else
				{
					var col = CurrentCell.Column!;
					var maxLength = col.Text.Length;

					for (int i = 0; i < RowCount; i++)
					{
						string text = "";
						int offSetX = 0, offSetY = 0;
						QueryItemText?.Invoke(i, col, out text, ref offSetX, ref offSetY);
						if (text.Length > maxLength)
						{
							maxLength = text.Length;
						}
					}

					var newWidth = (maxLength * _charSize.Width) + (CellWidthPadding * 2);
					col.Width = (int) newWidth;
					_columns.ColumnsChanged();
					Refresh();
				}
				
			}

			base.OnDoubleClick(e);
		}

		/// <summary>
		/// Gets or sets the amount of left and right padding on the text inside a cell
		/// </summary>
		[DefaultValue(3)]
		[Category("Behavior")]
		public int CellWidthPadding { get; set; } = 3;

		/// <summary>
		/// Gets or sets the amount of top and bottom padding on the text inside a cell
		/// </summary>
		[DefaultValue(1)]
		[Category("Behavior")]
		public int CellHeightPadding { get; set; } = 1;

		/// <summary>
		/// Gets or sets a value indicating whether grid lines are displayed around cells
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool GridLines { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the control is horizontal or vertical
		/// </summary>
		[Category("Behavior")]
		public bool HorizontalOrientation
		{
			get => _horizontalOrientation;
			set
			{
				if (_horizontalOrientation != value)
				{
					_horizontalOrientation = value;
					OrientationChanged();
					_hBar.SmallChange = CellWidth;
					_vBar.SmallChange = CellHeight;
				}
			}
		}

		/// <summary>
		/// Gets or sets the scrolling speed
		/// </summary>
		[Category("Behavior")]
		public int ScrollSpeed { get; set; }

		/// <summary>
		/// Gets or sets the sets the virtual number of rows to be displayed. Does not include the column header row.
		/// </summary>
		[Category("Behavior")]
		public int RowCount
		{
			get => _rowCount;
			set
			{
				bool fullRefresh = false;
				if (_rowCount != value)
				{
					if ((value < _rowCount && IsVisible(value)) || HorizontalOrientation)
					{
						fullRefresh = true;
					}

					_rowCount = value;

					//TODO replace this with a binary search + truncate
					if (_selectedItems.LastOrDefault()?.RowIndex >= _rowCount)
					{
						_selectedItems.RemoveAll(i => i.RowIndex >= _rowCount);
					}

					RecalculateScrollBars();
				}

				// Similarly to ListView in virtual mode, we want to always refresh
				// when setting row count, that gives the calling code assurance that
				// redraw will happen
				if (fullRefresh)
				{
					Refresh();
				}
				else
				{
					FastDraw();
				}
			}
		}

		private void FastDraw()
		{
			if (HorizontalOrientation)
			{
				int x = MaxColumnWidth;
				int y = 0;
				int w = Width - x;
				int h = VisibleColumns.Any()
					? GetHColBottom(VisibleColumns.Count() - 1)
					: 0;
				h = Math.Min(h, _drawHeight);

				Invalidate(new Rectangle(x, y, w, h));
			}
			else
			{
				int x = 0;
				int y = ColumnHeight + 1;

				int w = VisibleColumns.Any()
					? Math.Min(VisibleColumns.Max(c => c.Right) - _hBar.Value, Width)
					: 0;

				int h = Math.Min(RowCount * CellHeight,  Height - y);
				Invalidate(new Rectangle(x, y, w, h));
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether columns can be resized
		/// </summary>
		[Category("Behavior")]
		public bool AllowColumnResize { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether columns can be reordered
		/// </summary>
		[Category("Behavior")]
		public bool AllowColumnReorder { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the entire row will always be selected
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool FullRowSelect { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether multiple items can be selected
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(true)]
		public bool MultiSelect { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the control is in input painting mode
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		public bool InputPaintingMode { get; set; }

		/// <summary>
		/// All visible columns
		/// </summary>
		[Category("Behavior")]
		public IEnumerable<RollColumn> VisibleColumns => _columns.VisibleColumns;

		/// <summary>
		/// Gets or sets how the InputRoll scrolls when calling ScrollToIndex.
		/// </summary>
		[DefaultValue("near")]
		[Category("Behavior")]
		public string ScrollMethod { get; set; } = "near";

		/// <summary>
		/// Gets or sets a value indicating how the scrolling behavior for the hover event
		/// </summary>
		[Category("Behavior")]
		public bool AlwaysScroll { get; set; }

		/// <summary>
		/// Gets or sets the lowest seek interval to activate the progress bar
		/// </summary>
		[Category("Behavior")]
		public int SeekingCutoffInterval { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether pressing page up/down will cause
		/// the current selection to change
		/// </summary>
		[DefaultValue(true)]
		[Category("Behavior")]
		public bool ChangeSelectionWhenPaging { get; set; } = true;

		/// <summary>
		/// Returns all columns including those that are not visible
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RollColumns AllColumns => _columns;

		[DefaultValue(750)]
		[Category("Behavior")]
		public int HoverInterval
		{
			get => _hoverTimer.Interval;
			set => _hoverTimer.Interval = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the control can be toggled into HorizontalOrientation mode
		/// </summary>
		[DefaultValue(false)]
		[Category("Behavior")]
		public bool Rotatable { get; set; }

		/// <summary>
		/// Fire the <see cref="QueryItemText"/> event which requests the text for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemTextHandler QueryItemText;

		/// <summary>
		/// Fire the <see cref="QueryItemBkColor"/> event which requests the background color for the passed cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemBkColorHandler QueryItemBkColor;

		[Category("Virtual")]
		public event QueryRowBkColorHandler QueryRowBkColor;

		/// <summary>
		/// Fire the <see cref="QueryItemIconHandler"/> event which requests an icon for a given cell
		/// </summary>
		[Category("Virtual")]
		public event QueryItemIconHandler QueryItemIcon;

		/// <summary>
		/// Fire the QueryFrameLag event which checks if a given frame is a lag frame
		/// </summary>
		[Category("Virtual")]
		public event QueryFrameLagHandler QueryFrameLag;

		/// <summary>
		/// Fires when the mouse moves from one cell to another (including column header cells)
		/// </summary>
		[Category("Mouse")]
		public event CellChangeEventHandler PointedCellChanged;

		/// <summary>
		/// Fires when a cell is hovered on
		/// </summary>
		[Category("Mouse")]
		public event HoverEventHandler CellHovered;

		/// <summary>
		/// Occurs when a column header is clicked
		/// </summary>
		[Category("Action")]
		public event ColumnClickEventHandler ColumnClick;

		/// <summary>
		/// Occurs when a column header is right-clicked
		/// </summary>
		[Category("Action")]
		public event ColumnClickEventHandler ColumnRightClick;

		/// <summary>
		/// Occurs whenever the 'SelectedItems' property for this control changes
		/// </summary>
		[Category("Behavior")]
		public event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Occurs whenever the mouse wheel is scrolled while the right mouse button is held
		/// </summary>
		[Category("Behavior")]
		public event RightMouseScrollEventHandler RightMouseScrolled;

		[Category("Property Changed")]
		[Description("Occurs when the column header has been reordered")]
		public event ColumnReorderedEventHandler ColumnReordered;

		[Category("Action")]
		[Description("Occurs when the scroll value of the visible rows change (in vertical orientation this is the vertical scroll bar change, and in horizontal it is the horizontal scroll bar)")]
		public event RowScrollEvent RowScroll;

		[Category("Action")]
		[Description("Occurs when the scroll value of the columns (in vertical orientation this is the horizontal scroll bar change, and in horizontal it is the vertical scroll bar)")]
		public event ColumnScrollEvent ColumnScroll;

		[Category("Action")]
		[Description("Occurs when a cell is dragged and then dropped into a new cell, old cell is the cell that was being dragged, new cell is its new destination")]
		public event CellDroppedEvent CellDropped;

		/// <summary>
		/// Retrieve the text for a cell
		/// </summary>
		public delegate void QueryItemTextHandler(int index, RollColumn column, out string text, ref int offsetX, ref int offsetY);

		/// <summary>
		/// Retrieve the background color for a cell
		/// </summary>
		public delegate void QueryItemBkColorHandler(int index, RollColumn column, ref Color color);
		public delegate void QueryRowBkColorHandler(int index, ref Color color);

		/// <summary>
		/// Retrieve the image for a given cell
		/// </summary>
		public delegate void QueryItemIconHandler(int index, RollColumn column, ref Bitmap icon, ref int offsetX, ref int offsetY);

		/// <summary>
		/// Check if a given frame is a lag frame
		/// </summary>
		public delegate bool QueryFrameLagHandler(int index, bool hideWasLag);

		public delegate void CellChangeEventHandler(object sender, CellEventArgs e);

		public delegate void HoverEventHandler(object sender, CellEventArgs e);

		public delegate void RightMouseScrollEventHandler(object sender, MouseEventArgs e);

		public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);

		public delegate void ColumnReorderedEventHandler(object sender, ColumnReorderedEventArgs e);

		public delegate void RowScrollEvent(object sender, EventArgs e);

		public delegate void ColumnScrollEvent(object sender, EventArgs e);

		public delegate void CellDroppedEvent(object sender, CellEventArgs e);

		public class CellEventArgs
		{
			public CellEventArgs(Cell oldCell, Cell newCell)
			{
				OldCell = oldCell;
				NewCell = newCell;
			}

			public Cell OldCell { get; }
			public Cell NewCell { get; }
		}

		public class ColumnClickEventArgs
		{
			public ColumnClickEventArgs(RollColumn/*?*/ column)
			{
				Column = column;
			}

			public RollColumn/*?*/ Column { get; }
		}

		/// <remarks>this is only used in TAStudio, which ignores the args param completely</remarks>
		public class ColumnReorderedEventArgs
		{
			public ColumnReorderedEventArgs(int oldDisplayIndex, int newDisplayIndex, RollColumn/*?*/ column)
			{
				Column = column;
				OldDisplayIndex = oldDisplayIndex;
				NewDisplayIndex = newDisplayIndex;
			}

			public RollColumn/*?*/ Column { get; }

			public int OldDisplayIndex { get; }
			public int NewDisplayIndex { get; }
		}

		private int? _lastSelectedRow;

		public void SelectRow(int index, bool val)
		{
			if (_columns.VisibleColumns.Any())
			{
				if (val)
				{
					SelectCell(new Cell
					{
						RowIndex = index,
						Column = _columns[0]
					});
					_lastSelectedRow = index;
				}
				else
				{
					_selectedItems.RemoveAll(cell => cell.RowIndex == index);
					_lastSelectedRow = _selectedItems.LastOrDefault()?.RowIndex;
				}
			}
		}

		public void SelectAll()
		{
			_selectedItems.Clear();
			var oldFullRowVal = FullRowSelect;
			FullRowSelect = true;
			for (int i = 0; i < RowCount; i++)
			{
				SelectRow(i, true);
			}

			FullRowSelect = oldFullRowVal;
			_lastSelectedRow = RowCount;
			Refresh();
		}

		public void DeselectAll()
		{
			_lastSelectedRow = null;
			_selectedItems.Clear();
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
			Refresh();
		}

		public void ToggleSelectAll()
		{
			if (SelectedRows.CountIsExactly(RowCount)) DeselectAll();
			else SelectAll();
		}

		public void TruncateSelection(int index)
		{
			_selectedItems.RemoveAll(cell => cell.RowIndex > index);
			_lastSelectedRow = _selectedItems.LastOrDefault()?.RowIndex;
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPointingAtColumnHeader => IsHoveringOnColumnCell;

		/// <returns>the <see cref="Cell.RowIndex"/> of the selected row with the earliest index, or <see langword="null"/> if no rows are selected</returns>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int? SelectionStartIndex
			=> AnyRowsSelected ? SelectedRowsWithDuplicates.First() : null;

		/// <returns>the <see cref="Cell.RowIndex"/> of the selected row with the latest index, or <see langword="null"/> if no rows are selected</returns>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int? SelectionEndIndex
			=> AnyRowsSelected ? SelectedRowsWithDuplicates.Last() : null;

		/// <summary>
		/// Gets or sets the current Cell that the mouse was in.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Cell CurrentCell { get; set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsPaintDown { get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool RightButtonHeld { get; private set; }

		public string UserSettingsSerialized()
		{
			var settings = ConfigService.SaveWithType(Settings);
			return settings;
		}

		public void LoadSettingsSerialized(string settingsJson)
		{
			try
			{
				InputRollSettings settings = ConfigService.LoadWithType<InputRollSettings>(settingsJson);

				_columns = settings.Columns;
				_columns.ChangedCallback = ColumnChangedCallback;
				HorizontalOrientation = settings.HorizontalOrientation;
				LagFramesToHide = settings.LagFramesToHide;
				HideWasLagFrames = settings.HideWasLagFrames;
			}
			catch
			{
				// TODO: don't silently fail, inform the user somehow
			}
		}

		private InputRollSettings Settings => new InputRollSettings
		{
			Columns = _columns,
			HorizontalOrientation = HorizontalOrientation,
			LagFramesToHide = LagFramesToHide,
			HideWasLagFrames = HideWasLagFrames
		};

		public class InputRollSettings
		{
			public RollColumns Columns { get; set; }
			public bool HorizontalOrientation { get; set; }
			public int LagFramesToHide { get; set; }
			public bool HideWasLagFrames { get; set; }
		}

		/// <summary>
		/// Gets or sets the first visible row index, if scrolling is needed
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int FirstVisibleRow
		{
			get
			{
				if (HorizontalOrientation)
				{
					return _hBar.Value / CellWidth;
				}

				return _vBar.Value / CellHeight;
			}

			set
			{
				if (HorizontalOrientation)
				{
					if (NeedsHScrollbar)
					{
						_programmaticallyUpdatingScrollBarValues = true;
						if (value * CellWidth <= _hBar.Maximum)
						{
							_hBar.Value = value * CellWidth;
						}
						else
						{
							_hBar.Value = _hBar.Maximum;
						}

						_programmaticallyUpdatingScrollBarValues = false;
					}
				}
				else
				{
					if (NeedsVScrollbar)
					{
						_programmaticallyUpdatingScrollBarValues = true;
						if (value * CellHeight <= _vBar.Maximum)
						{
							_vBar.Value = value * CellHeight;
						}
						else
						{
							_vBar.Value = _vBar.Maximum;
						}

						_programmaticallyUpdatingScrollBarValues = false;
					}
				}
				_programmaticallyChangingRow = false;
				PointMouseToNewCell();
			}
		}

		private int LastFullyVisibleRow
		{
			get
			{
				int halfRow = 0;
				if (HorizontalOrientation)
				{
					halfRow = 1;  // TODO: A more precise calculation, but it really isn't important, you have to be pixel perfect for this to be off by 1 and even then it doesn't look bad because the 1 pixel is the border
				}
				else
				{
					if ((_drawHeight - ColumnHeight - 3) % CellHeight < CellHeight / 2)
					{
						halfRow = 1;
					}
				}

				return FirstVisibleRow + VisibleRows - halfRow + CountLagFramesDisplay(VisibleRows - halfRow);
			}
		}

		private int LastVisibleRow
		{
			get => FirstVisibleRow + VisibleRows + CountLagFramesDisplay(VisibleRows);
			set
			{
				int halfRow = 0;
				if (HorizontalOrientation)
				{
					halfRow = 1; // TODO: A more precise calculation, but it really isn't important, you have to be pixel perfect for this to be off by 1 and even then it doesn't look bad because the 1 pixel is the border
				}
				else
				{
					if ((_drawHeight - ColumnHeight - 3) % CellHeight < CellHeight / 2)
					{
						halfRow = 1;
					}
				}

				if (LagFramesToHide == 0)
				{
					FirstVisibleRow = Math.Max(value - (VisibleRows - halfRow), 0);
				}
				else
				{
					if (Math.Abs(LastFullyVisibleRow - value) > VisibleRows) // Big jump
					{
						FirstVisibleRow = Math.Max(value - (ExpectedDisplayRange() - halfRow), 0);
						SetLagFramesArray();
					}

					// Small jump, more accurate
					int lastVisible = LastFullyVisibleRow;
					do
					{
						if ((lastVisible - value) / (LagFramesToHide + 1) != 0)
						{
							FirstVisibleRow = Math.Max(FirstVisibleRow - ((lastVisible - value) / (LagFramesToHide + 1)), 0);
						}
						else
						{
							FirstVisibleRow -= Math.Sign(lastVisible - value);
						}

						SetLagFramesArray();
						lastVisible = LastFullyVisibleRow;
					}
					while ((lastVisible - value < 0 || _lagFrames[VisibleRows - halfRow] < lastVisible - value) && FirstVisibleRow != 0);
				}
				_programmaticallyChangingRow = false;
				PointMouseToNewCell();
			}
		}

		private bool IsVisible(int index)
		{
			Debug.Assert(FirstVisibleRow < LastFullyVisibleRow, "rows out of order?");
			return FirstVisibleRow <= index && index <= LastFullyVisibleRow;
		}

		public bool IsPartiallyVisible(int index)
		{
			Debug.Assert(FirstVisibleRow < LastVisibleRow, "rows out of order?");
			return FirstVisibleRow <= index && index <= LastVisibleRow;
		}

		/// <summary>
		/// Gets the number of rows currently visible including partially visible rows.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int VisibleRows
		{
			get
			{
				if (HorizontalOrientation)
				{
					return (_drawWidth - MaxColumnWidth) / CellWidth;
				}

				var result = (_drawHeight - ColumnHeight - 3) / CellHeight; // Minus three makes it work
				return result < 0 ? 0 : result;
			}
		}

		private Cell _draggingCell;

		public void DragCurrentCell()
		{
			_draggingCell = CurrentCell;
		}

		public void ReleaseCurrentCell()
		{
			if (_draggingCell != null)
			{
				var draggedCell = _draggingCell;
				_draggingCell = null;

				if (CurrentCell != draggedCell)
				{
					CellDropped?.Invoke(this, new CellEventArgs(draggedCell, CurrentCell));
				}
			}
		}

		/// <summary>
		/// Scrolls to the given index, according to the scroll settings.
		/// </summary>
		public void ScrollToIndex(int index)
		{
			if (ScrollMethod == "near")
			{
				MakeIndexVisible(index);
			}

			if (!IsVisible(index) || AlwaysScroll)
			{
				if (ScrollMethod == "top")
				{
					FirstVisibleRow = index;
				}
				else if (ScrollMethod == "bottom")
				{
					LastVisibleRow = index;
				}
				else if (ScrollMethod == "center")
				{
					if (LagFramesToHide == 0)
					{
						FirstVisibleRow = Math.Max(index - (VisibleRows / 2), 0);
					}
					else
					{
						if (Math.Abs(FirstVisibleRow + CountLagFramesDisplay(VisibleRows / 2) - index) > VisibleRows) // Big jump
						{
							FirstVisibleRow = Math.Max(index - (ExpectedDisplayRange() / 2), 0);
							SetLagFramesArray();
						}

						// Small jump, more accurate
						var range = 0.RangeTo(_lagFrames[VisibleRows]);
						int lastVisible = FirstVisibleRow + CountLagFramesDisplay(VisibleRows / 2);
						do
						{
							if ((lastVisible - index) / (LagFramesToHide + 1) != 0)
							{
								FirstVisibleRow = Math.Max(FirstVisibleRow - ((lastVisible - index) / (LagFramesToHide + 1)), 0);
							}
							else
							{
								FirstVisibleRow -= Math.Sign(lastVisible - index);
							}

							SetLagFramesArray();
							lastVisible = FirstVisibleRow + CountLagFramesDisplay(VisibleRows / 2);
						}
						while (!range.Contains(lastVisible - index) && FirstVisibleRow != 0);
					}
				}
			}
			_programmaticallyChangingRow = false;
			PointMouseToNewCell();
		}

		public bool _programmaticallyChangingRow = false;

		/// <summary>
		/// Scrolls so that the given index is visible, if it isn't already; doesn't use scroll settings.
		/// </summary>
		public void MakeIndexVisible(int index)
		{
			if (!IsVisible(index))
			{
				_programmaticallyChangingRow = true;

				if (FirstVisibleRow > index)
				{
					FirstVisibleRow = index;
				}
				else
				{
					LastVisibleRow = index;
				}
			}
		}

		[Browsable(false)]
		private IEnumerable<int> SelectedRowsWithDuplicates
			=> _selectedItems.Select(static cell => cell.RowIndex ?? -1).Where(static i => i >= 0);

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<int> SelectedRows
			=> SelectedRowsWithDuplicates.Distinct();

		[Browsable(false)]
		public bool AnyRowsSelected
			=> _selectedItems.Any(static cell => cell.RowIndex is not null);

		/// <returns>the <see cref="Cell.RowIndex"/> of the first row in the selection list (throws if no rows are selected)</returns>
		/// <remarks>you probably want <see cref="SelectionStartIndex"/>, TODO check existing callsites</remarks>
		[Browsable(false)]
		public int FirstSelectedRowIndex
			=> SelectedRowsWithDuplicates.First();

		public bool IsRowSelected(int rowIndex)
			=> _selectedItems.IncludesRow(rowIndex);

		public IEnumerable<ToolStripItem> GenerateContextMenuItems()
		{
			if (!Rotatable) return [ ];
			var rotate = new ToolStripMenuItem
			{
				Name = "RotateMenuItem",
				Text = "Rotate",
				ShortcutKeyDisplayString = RotateHotkeyStr
			};
			rotate.Click += (_, _) => HorizontalOrientation = !HorizontalOrientation;
			return [ new ToolStripSeparator(), rotate ];
		}

		public string RotateHotkeyStr => "Ctrl+Shift+F";

		private bool _columnDownMoved;
		private int _previousX; // TODO: move me

		// It's necessary to call this anytime the control is programmatically scrolled
		// Since the mouse may not be pointing to the same cell anymore
		public void PointMouseToNewCell()
		{
			if (_currentX.HasValue && _currentY.HasValue)
			{
				var newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
				if (CurrentCell != newCell)
				{
					if (QueryFrameLag != null && newCell.RowIndex.HasValue)
					{
						newCell.RowIndex += CountLagFramesDisplay(newCell.RowIndex.Value);
					}

					newCell.RowIndex += FirstVisibleRow;
					if (newCell.RowIndex < 0)
					{
						newCell.RowIndex = 0;
					}

					if (_programmaticallyChangingRow)
					{
						_programmaticallyChangingRow = false;
						CellChanged(newCell);
					}
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			_previousX = _currentX ?? 0;
			_currentX = e.X;
			_currentY = e.Y;

			if (_columnResizing != null)
			{
				if (_currentX != _previousX)
				{
					_columnResizing.Width += _currentX.Value - _previousX;
					if (_columnResizing.Width <= 0)
					{
						_columnResizing.Width = 1;
					}

					_columns.ColumnsChanged();
					Refresh();
				}
			}
			else if (_columnDown != null)
			{
				_columnDownMoved = true;
			}

			Cell newCell = CalculatePointedCell(_currentX.Value, _currentY.Value);
			
			// SuuperW: Hide lag frames
			if (QueryFrameLag != null && newCell.RowIndex.HasValue)
			{
				newCell.RowIndex += CountLagFramesDisplay(newCell.RowIndex.Value);
			}

			newCell.RowIndex += FirstVisibleRow;
			if (newCell.RowIndex < 0)
			{
				newCell.RowIndex = 0;
			}

			if (!newCell.Equals(CurrentCell))
			{
				CellChanged(newCell);

				if (IsHoveringOnColumnCell
					|| (WasHoveringOnColumnCell && !IsHoveringOnColumnCell))
				{
					Refresh();
				}
				else if (_columnDown != null)
				{
					Refresh();
				}
			}
			else if (_columnDown != null)  // Kind of silly feeling to have this check twice, but the only alternative I can think of has it refreshing twice when pointed column changes with column down, and speed matters
			{
				Refresh();
			}

			Cursor = IsHoveringOnColumnEdge || _columnResizing != null
				? Cursors.VSplit
				: Cursors.Default;

			base.OnMouseMove(e);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			CurrentCell = new Cell
			{
				Column = null,
				RowIndex = null
			};

			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			bool refresh = false;
			_currentX = null;
			_currentY = null;
			if (IsHoveringOnColumnCell)
			{
				refresh = true;
			}

			CurrentCell = null;
			IsPaintDown = false;
			_columnResizing = null;
			_hoverTimer.Stop();
			if (refresh)
			{
				Refresh();
			}

			base.OnMouseLeave(e);
		}

		// TODO add query callback of whether to select the cell or not
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnColumnEdge)
				{
					_columnResizing = CurrentCell.Column;
				}
				if (IsHoveringOnColumnCell)
				{
					_columnDown = CurrentCell.Column;
				}
				else if (InputPaintingMode)
				{
					IsPaintDown = true;
				}
			}

			if (e.Button == MouseButtons.Right)
			{
				if (!IsHoveringOnColumnCell)
				{
					RightButtonHeld = true;
				}
			}

			if (e.Button == MouseButtons.Left)
			{
				if (IsHoveringOnDataCell)
				{
					if (ModifierKeys == Keys.Alt)
					{
						// do marker drag here
					}
					else if (ModifierKeys is Keys.Shift && CurrentCell.Column! is { Type: ColumnType.Text } col)
					{
						if (_selectedItems.Count is not 0)
						{
							if (FullRowSelect)
							{
								var targetRow = CurrentCell.RowIndex.Value;
								if (!_selectedItems.IncludesRow(targetRow))
								{
									int additionStart, additionEndExcl;
									SortedList<int> rowIndices = new(SelectedRows);
									var firstIndex = rowIndices.Min();
									if (targetRow < firstIndex)
									{
										additionStart = targetRow;
										additionEndExcl = firstIndex;
									}
									else
									{
										var lastIndex = rowIndices.Max();
										if (targetRow > lastIndex)
										{
											additionStart = lastIndex + 1;
											additionEndExcl = targetRow + 1;
										}
										else // Somewhere in between, a scenario that can happen with ctrl-clicking, find the previous and highlight from there --adelikat // shouldn't it be from the previous click target? --yoshi
										{
											var insertionPoint = ~rowIndices.BinarySearch(targetRow); // the search will never succeed since we already know the target row isn't among those selected
											additionStart = rowIndices[insertionPoint - 1]; // insertionPoint is strictly greater than needle, so subtract 1 (this is safe because insertionPoint would only be 0 if needle was less than the first element, which it isn't)
											additionEndExcl = targetRow + 1;
										}
									}
									for (var i = additionStart; i < additionEndExcl; i++) SelectCell(new() { RowIndex = i, Column = col });
								}
							}
							else
							{
								MessageBox.Show("Shift click logic for individual cells has not yet implemented");
							}
						}
						else
						{
							SelectCell(CurrentCell);
						}
					}
					else if (ModifierKeys is Keys.Control && CurrentCell.Column!.Type is ColumnType.Text)
					{
						SelectCell(CurrentCell, toggle: true);
					}
					else if (ModifierKeys != Keys.Shift)
					{
						_selectedItems.Clear();
						SelectCell(CurrentCell);
					}

					Refresh();

					SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			if (AllowRightClickSelection && e.Button == MouseButtons.Right)
			{
				// In the case that we have a context menu already open, we must manually update the CurrentCell as MouseMove isn't triggered while it is open.
				if (CurrentCell == null)
					OnMouseMove(e);

				if (!IsHoveringOnColumnCell)
				{
					// If this cell is not currently selected, clear and select
					if (!_selectedItems.Contains(CurrentCell))
					{
						_selectedItems.Clear();
						SelectCell(CurrentCell);

						Refresh();

						SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
					}
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (_columnResizing == null && IsHoveringOnColumnCell)
			{
				if (_columnDown != null && _columnDownMoved)
				{
					DoColumnReorder();
					_columnDown = null;
					Refresh();
				}
				else if (e.Button == MouseButtons.Left)
				{
					ColumnClickEvent(ColumnAtPixel(HorizontalOrientation ? e.Y : e.X));
				}
				else if (e.Button == MouseButtons.Right)
				{
					ColumnRightClickEvent(ColumnAtPixel(HorizontalOrientation ? e.Y : e.X));
				}
			}

			_columnResizing = null;
			_columnDown = null;
			_columnDownMoved = false;
			RightButtonHeld = false;
			IsPaintDown = false;
			base.OnMouseUp(e);
		}

		private void IncrementScrollBar(ScrollBar bar, bool increment)
		{
			int newVal;
			if (increment)
			{
				newVal = bar.Value + bar.SmallChange * ScrollSpeed;
				if (newVal > bar.Maximum - bar.LargeChange)
				{
					newVal = bar.Maximum - bar.LargeChange;
				}
			}
			else
			{
				newVal = bar.Value - bar.SmallChange * ScrollSpeed;
				if (newVal < 0)
				{
					newVal = 0;
				}
			}

			_programmaticallyUpdatingScrollBarValues = true;
			bar.Value = newVal;
			_programmaticallyUpdatingScrollBarValues = false;
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (RightButtonHeld)
			{
				DoRightMouseScroll(this, e);
			}
			else
			{
				if (HorizontalOrientation)
				{
					do
					{
						IncrementScrollBar(_hBar, e.Delta < 0);
						SetLagFramesFirst();
					}
					while (_lagFrames[0] != 0 && _hBar.Value != 0 && _hBar.Value != _hBar.Maximum);
				}
				else
				{
					do
					{
						IncrementScrollBar(_vBar, e.Delta < 0);
						SetLagFramesFirst();
					}
					while (_lagFrames[0] != 0 && _vBar.Value != 0 && _vBar.Value != _vBar.Maximum);
				}

				if (_currentX != null)
				{
					OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, _currentX.Value, _currentY.Value, 0));
				}

				Refresh();
			}
		}

#pragma warning disable MA0091 // passing through `sender` is intentional
		private void DoRightMouseScroll(object sender, MouseEventArgs e)
		{
			RightMouseScrolled?.Invoke(sender, e);
		}
#pragma warning restore MA0091

		private void ColumnClickEvent(RollColumn/*?*/ column)
		{
			ColumnClick?.Invoke(this, new ColumnClickEventArgs(column));
		}

		private void ColumnRightClickEvent(RollColumn/*?*/ column)
		{
			ColumnRightClick?.Invoke(this, new ColumnClickEventArgs(column));
		}

		// This allows arrow keys to be detected by KeyDown.
		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
			{
				e.IsInputKey = true;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!SuspendHotkeys)
			{
				if (e.IsPressed(Keys.Escape))
				{
					DeselectAll();
					Refresh();
				}
				else if (e.IsCtrlShift(Keys.F))
				{
					if (Rotatable) HorizontalOrientation = !HorizontalOrientation;
				}
				// Scroll
				else if (e.IsPressed(Keys.PageUp))
				{
					if (ChangeSelectionWhenPaging)
					{
						var selectedRow = AnyRowsSelected ? FirstSelectedRowIndex : FirstVisibleRow;
						var increment = LastVisibleRow - FirstVisibleRow;
						var newSelectedRow = selectedRow - increment;
						if (newSelectedRow < 0)
						{
							newSelectedRow = 0;
						}

						FirstVisibleRow = newSelectedRow;
						DeselectAll();
						SelectRow(newSelectedRow, true);
						Refresh();
					}
					else if (FirstVisibleRow > 0)
					{
						LastVisibleRow = FirstVisibleRow;
					}
				}
				else if (e.IsPressed(Keys.PageDown))
				{
					if (ChangeSelectionWhenPaging)
					{
						var selectedRow = AnyRowsSelected ? FirstSelectedRowIndex : FirstVisibleRow;
						var increment = LastVisibleRow - FirstVisibleRow;
						var newSelectedRow = selectedRow + increment;
						if (newSelectedRow > RowCount - 1)
						{
							newSelectedRow = RowCount - 1;
						}

						LastVisibleRow = newSelectedRow;
						DeselectAll();
						SelectRow(newSelectedRow, true);
						Refresh();
					}
					else if (LastVisibleRow < RowCount)
					{
						FirstVisibleRow = LastVisibleRow;
					}
				}
				else if (AllowMassNavigationShortcuts && e.IsPressed(Keys.Home))
				{
					DeselectAll();
					SelectRow(0, true);
					FirstVisibleRow = 0;
					Refresh();
				}
				else if (AllowMassNavigationShortcuts && e.IsPressed(Keys.End))
				{
					DeselectAll();
					SelectRow(RowCount - 1, true);
					LastVisibleRow = RowCount;
					Refresh();
				}
				else if (e.IsPressed(Keys.Up))
				{
					if (AnyRowsSelected)
					{
						var selectedRow = FirstSelectedRowIndex;
						if (selectedRow > 0)
						{
							var targetSelectedRow = selectedRow - 1;
							DeselectAll();
							SelectRow(targetSelectedRow, true);
							ScrollToIndex(targetSelectedRow);
							Refresh();
						}
					}
				}
				else if (e.IsPressed(Keys.Down))
				{
					if (AnyRowsSelected)
					{
						var selectedRow = FirstSelectedRowIndex;
						if (selectedRow < RowCount - 1)
						{
							var targetSelectedRow = selectedRow + 1;
							DeselectAll();
							SelectRow(targetSelectedRow, true);
							ScrollToIndex(targetSelectedRow);
							Refresh();
						}
					}
				}
				else if (e.IsShift(Keys.Up))
				{
					if (MultiSelect && _lastSelectedRow > 0)
					{
						if (_selectedItems.IncludesRow(_lastSelectedRow.Value)
							&& _selectedItems.IncludesRow(_lastSelectedRow.Value - 1)) // Unhighlight if already highlighted
						{
							SelectRow(_lastSelectedRow.Value, false);
						}
						else
						{
							SelectRow(_lastSelectedRow.Value - 1, true);
						}

						Refresh();
					}
				}
				else if (e.IsShift(Keys.Down))
				{
					if (MultiSelect && _lastSelectedRow < RowCount - 1)
					{
						if (_selectedItems.IncludesRow(_lastSelectedRow.Value)
							&& _selectedItems.IncludesRow(_lastSelectedRow.Value + 1)) // Unhighlight if already highlighted
						{
							var origIndex = _lastSelectedRow.Value;
							SelectRow(origIndex, false);

							// SelectRow assumed the max row should be selected, but in this edge case it isn't
							_lastSelectedRow = _selectedItems.FirstOrDefault()?.RowIndex;
						}
						else
						{
							SelectRow(_lastSelectedRow.Value + 1, true);
							
						}

						Refresh();
					}
				}
				// Selection cursor
				else if (e.IsCtrl(Keys.Up))
				{
					if (AnyRowsSelected && LetKeysModifySelection && FirstSelectedRowIndex > 0)
					{
						foreach (var row in SelectedRows.ToList()) // clones SelectedRows
						{
							SelectRow(row - 1, true);
							SelectRow(row, false);
						}
					}
				}
				else if (e.IsCtrl(Keys.Down))
				{
					if (AnyRowsSelected && LetKeysModifySelection)
					{
						foreach (var row in SelectedRows.Reverse()) // clones SelectedRows
						{
							SelectRow(row + 1, true);
							SelectRow(row, false);
						}
					}
				}
				else if (e.IsCtrl(Keys.Left))
				{
					if (AnyRowsSelected && LetKeysModifySelection)
					{
						SelectRow(SelectedRows.Last(), false);
					}
				}
				else if (e.IsCtrl(Keys.Right))
				{
					if (AnyRowsSelected && LetKeysModifySelection && SelectedRows.Last() < _rowCount - 1)
					{
						SelectRow(SelectedRows.Last() + 1, true);
					}
				}
				else if (e.IsCtrlShift(Keys.Left))
				{
					if (AnyRowsSelected && LetKeysModifySelection && FirstSelectedRowIndex > 0)
					{
						SelectRow(FirstSelectedRowIndex - 1, true);
					}
				}
				else if (e.IsCtrlShift(Keys.Right))
				{
					if (AnyRowsSelected && LetKeysModifySelection)
					{
						SelectRow(FirstSelectedRowIndex, false);
					}
				}
				else if (e.IsCtrl(Keys.PageUp))
				{
					//jump to above marker with selection courser
					if (LetKeysModifySelection)
					{
						
					}
				}
				else if (e.IsCtrl(Keys.PageDown))
				{
					//jump to below marker with selection courser
					if (LetKeysModifySelection)
					{

					}
				}
			}

			base.OnKeyDown(e);
		}

		protected override void OnResize(EventArgs e)
		{
			RecalculateScrollBars();
			base.OnResize(e);
			FastDraw();
		}

		private void OrientationChanged()
		{
			// TODO scroll to correct positions
			ColumnChangedCallback();
			Refresh();
		}

		/// <summary>
		/// Call this function to change the CurrentCell to newCell
		/// </summary>
		private void CellChanged(Cell newCell)
		{
			_lastCell = CurrentCell;
			CurrentCell = newCell;

			if (PointedCellChanged is not null
				&& !(_lastCell?.Column == CurrentCell.Column && _lastCell?.RowIndex == CurrentCell.RowIndex)) //TODO isn't this just `Cell.==`? --yoshi
			{
				PointedCellChanged(this, new CellEventArgs(_lastCell, CurrentCell));
			}

			if (CurrentCell?.Column is not null)
			{
				_hoverTimer.Start();
			}
			else
			{
				_hoverTimer.Stop();
			}
		}

		private void VerticalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}

#pragma warning disable MA0091 // unorthodox, but I think this is sound --yoshi
			if (_horizontalOrientation)
			{
				ColumnScroll?.Invoke(_hBar, e);
			}
			else
			{
				RowScroll?.Invoke(_vBar, e);
			}
#pragma warning restore MA0091
		}

		private void HorizontalBar_ValueChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyUpdatingScrollBarValues)
			{
				Refresh();
			}

#pragma warning disable MA0091 // unorthodox, but I think this is sound --yoshi
			if (_horizontalOrientation)
			{
				RowScroll?.Invoke(_hBar, e);
			}
			else
			{
				ColumnScroll?.Invoke(_vBar, e);
			}
#pragma warning restore MA0091
		}

		private void ColumnChangedCallback()
		{
			RecalculateScrollBars();
			if (_columns.VisibleColumns.Any())
			{
				MaxColumnWidth = _columns.VisibleColumns.Max(c => c.Width);
			}
		}

		private void DoColumnReorder()
		{
			if (_columnDown! != CurrentCell.Column!)
			{
				var oldIndex = _columns.IndexOf(_columnDown);
				var newIndex = _columns.IndexOf(CurrentCell.Column);

				ColumnReordered?.Invoke(this, new ColumnReorderedEventArgs(oldIndex, newIndex, _columnDown));

				//TODO surely this only works properly in one direction?
				// also the event is "...Reordered"--past tense--so it should be called AFTER the change --yoshi
				_columns.Remove(_columnDown);
				_columns.Insert(newIndex, _columnDown);
			}
		}

		// ScrollBar.Maximum = DesiredValue + ScrollBar.LargeChange - 1
		// See MSDN Page for more information on the dumb ScrollBar.Maximum Property
		private void RecalculateScrollBars()
		{
			UpdateDrawSize();

			var columns = _columns.VisibleColumns.ToList();
			int iLastColumn = columns.Count - 1;

			if (HorizontalOrientation)
			{
				NeedsVScrollbar = GetHColBottom(iLastColumn) > _drawHeight;
				NeedsHScrollbar = RowCount > 1;
			}
			else
			{
				NeedsVScrollbar = ColumnHeight + (RowCount * CellHeight)  > Height;
				NeedsHScrollbar = TotalColWidth - _drawWidth + 1 > 0;
			}

			UpdateDrawSize();
			if (VisibleRows > 0)
			{
				if (HorizontalOrientation)
				{
					_hBar.Maximum = Math.Max((VisibleRows - 1) * CellWidth, _hBar.Maximum);
					_hBar.LargeChange = (VisibleRows - 1) * CellWidth;
					_vBar.LargeChange = Math.Max(0, _drawHeight / 2);
				}
				else
				{
					_vBar.Maximum = Math.Max((VisibleRows - 1) * CellHeight, _vBar.Maximum); // ScrollBar.Maximum is dumb
					_vBar.LargeChange = (VisibleRows - 1) * CellHeight;
					// DrawWidth can be negative if the TAStudio window is small enough
					// Clamp LargeChange to 0 here to prevent exceptions
					_hBar.LargeChange = Math.Max(0, _drawWidth / 2);
				}
			}

			// Update VBar
			if (NeedsVScrollbar)
			{
				if (HorizontalOrientation)
				{
					_vBar.Maximum = GetHColBottom(iLastColumn) - _drawHeight + _vBar.LargeChange;
					if (_vBar.Maximum < 0)
					{
						_vBar.Maximum = 0;
					}
				}
				else
				{
					_vBar.Maximum = RowsToPixels(RowCount + 1) - (CellHeight * 3) + _vBar.LargeChange - 1;
					if (_vBar.Maximum < 0)
					{
						_vBar.Maximum = 0;
					}
				}

				_vBar.Location = new Point(Width - _vBar.Width, 0);
				_vBar.Height = Height;
				_vBar.Visible = true;
			}
			else
			{
				_vBar.Visible = false;
				_vBar.Value = 0;
			}

			// Update HBar
			if (NeedsHScrollbar)
			{
				if (HorizontalOrientation)
				{
					_hBar.Maximum = RowsToPixels(RowCount + 1) - (CellHeight * 3) + _hBar.LargeChange - 1;
				}
				else
				{
					_hBar.Maximum = TotalColWidth - _drawWidth + _hBar.LargeChange;
				}

				_hBar.Location = new Point(0, Height - _hBar.Height);
				_hBar.Width = Width - (NeedsVScrollbar ? (_vBar.Width + 1) : 0);
				_hBar.Visible = true;
			}
			else
			{
				_hBar.Visible = false;
				_hBar.Value = 0;
			}
		}

		private void UpdateDrawSize()
		{
			_drawWidth = NeedsVScrollbar
				? Width - _vBar.Width
				: Width;

			_drawHeight = NeedsHScrollbar
				? Height - _hBar.Height
				: Height;
		}

		/// <summary>
		/// If FullRowSelect is enabled, selects all cells in the row that contains the given cell. Otherwise only given cell is added.
		/// </summary>
		/// <param name="cell">The cell to select.</param>
		/// <param name="toggle">Specifies whether or not to toggle the current state, rather than force the value to true</param>
		private void SelectCell(Cell cell, bool toggle = false)
		{
			if (cell.RowIndex is int row && row < RowCount)
			{
				if (!MultiSelect)
				{
					_selectedItems.Clear();
					_lastSelectedRow = null;
				}

				if (FullRowSelect)
				{
					if (toggle && _selectedItems.IncludesRow(row))
					{
						_selectedItems.RemoveAll(x => x.RowIndex == row);
						_lastSelectedRow = _selectedItems.LastOrDefault()?.RowIndex;
					}
					else
					{
						foreach (var column in _columns)
						{
							_selectedItems.Add(new() { RowIndex = row, Column = column });
							_lastSelectedRow = row;
						}
					}
				}
				else
				{
					_lastSelectedRow = null; // TODO: tracking this by cell is a lot more work
					if (toggle && _selectedItems.IncludesRow(row))
					{
						var item = _selectedItems
							.FirstOrDefault(x => x.Equals(cell));

						if (item != null)
						{
							_selectedItems.Remove(item);
						}
					}
					else
					{
						_selectedItems.Add(CurrentCell);
					}
				}
			}
		}

		private bool IsHoveringOnColumnCell => CurrentCell?.Column != null && !CurrentCell.RowIndex.HasValue;

		private bool IsHoveringOnColumnEdge => AllowColumnResize && IsHoveringOnColumnCell && IsPointingOnCellEdge(_currentX);

		private bool IsHoveringOnDataCell => CurrentCell?.Column != null && CurrentCell.RowIndex.HasValue;

		private bool WasHoveringOnColumnCell => _lastCell?.Column != null && !_lastCell.RowIndex.HasValue;

		private bool IsPointingOnCellEdge(int? x) => x.HasValue
			&& !HorizontalOrientation //TODO support column resize in horizontal orientation
			&& _columns.VisibleColumns.Any(column => (column.Left - _hBar.Value + (column.Width - column.Width / 6)).RangeTo(column.Right - _hBar.Value).Contains(x.Value));

		/// <summary>
		/// Finds the specific cell that contains the (x, y) coordinate.
		/// </summary>
		/// <remarks>The row number that it returns will be between 0 and VisibleRows, NOT the absolute row number.</remarks>
		/// <param name="x">X coordinate point.</param>
		/// <param name="y">Y coordinate point.</param>
		/// <returns>The cell with row number and RollColumn reference, both of which can be null. </returns>
		private Cell CalculatePointedCell(int x, int y)
		{
			var newCell = new Cell();

			// If pointing to a column header
			if (_columns.VisibleColumns.Any())
			{
				if (HorizontalOrientation)
				{
					newCell.RowIndex = PixelsToRows(x);
					newCell.Column = ColumnAtPixel(y);
				}
				else
				{
					newCell.RowIndex = PixelsToRows(y);
					newCell.Column = ColumnAtPixel(x);
				}
			}

			if (!(IsPaintDown || RightButtonHeld) && newCell.RowIndex <= -1) // -2 if we're entering from the top
			{
				newCell.RowIndex = null;
			}

			return newCell;
		}

		// A boolean that indicates if the InputRoll is too large vertically and requires a vertical scrollbar.
		private bool NeedsVScrollbar { get; set; }

		// A boolean that indicates if the InputRoll is too large horizontally and requires a horizontal scrollbar.
		private bool NeedsHScrollbar { get; set; }

		// Gets the total width of all the columns by using the last column's Right property.
		private int TotalColWidth => _columns.VisibleColumns.Any()
			? _columns.VisibleColumns.Last().Right
			: 0;

		/// <summary>
		/// Returns the RollColumn object at the specified visible pixel coordinate.
		/// </summary>
		/// <param name="pixel">The pixel coordinate.</param>
		/// <returns>RollColumn object that contains the pixel coordinate or null if none exists.</returns>
		private RollColumn/*?*/ ColumnAtPixel(int pixel)
		{
			if (_horizontalOrientation)
			{
				return _columns.VisibleColumns.Select(static (n, i) => (Column: n, Index: i))
					.FirstOrNull(item => pixel >= GetHColTop(item.Index) - _vBar.Value && pixel <= GetHColBottom(item.Index) - _vBar.Value)
					?.Column;
			}
			return _columns.VisibleColumns.FirstOrDefault(column => pixel >= column.Left - _hBar.Value && pixel <= column.Right - _hBar.Value);
		}

		/// <summary>
		/// Converts a row number to a horizontal or vertical coordinate.
		/// </summary>
		/// <returns>A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</returns>
		private int RowsToPixels(int index)
		{
			if (_horizontalOrientation)
			{
				return (index * CellWidth) + MaxColumnWidth;
			}

			return (index * CellHeight) + ColumnHeight;
		}

		/// <summary>
		/// Converts a horizontal or vertical coordinate to a row number.
		/// </summary>
		/// <param name="pixels">A vertical coordinate if Vertical Oriented, otherwise a horizontal coordinate.</param>
		/// <returns>A row number between 0 and VisibleRows if it is a data row, otherwise a negative number if above all Datarows.</returns>
		private int PixelsToRows(int pixels)
		{
			// Using Math.Floor and float because integer division rounds towards 0 but we want to round down.
			if (_horizontalOrientation)
			{
				return (int)Math.Floor((float)(pixels - MaxColumnWidth) / CellWidth);
			}

			return (int)Math.Floor((float)(pixels - ColumnHeight) / CellHeight);
		}

		private int GetHColTop(int index) =>
			_horizontalColumnTops != null && 0.RangeToExclusive(_horizontalColumnTops.Length).Contains(index)
				? _horizontalColumnTops[index]
				: index * CellHeight;

		private int GetHColHeight(int index) =>
			_horizontalColumnTops != null && 0.RangeToExclusive(_horizontalColumnTops.Length - 1).Contains(index)
				? _horizontalColumnTops[index + 1] - _horizontalColumnTops[index]
				: CellHeight;

		private int GetHColBottom(int index) =>
			GetHColTop(index + 1);

		// The width of the largest column cell in Horizontal Orientation
		private int MaxColumnWidth { get; set; }

		// The height of a column cell in Vertical Orientation.
		private int ColumnHeight => CellHeight + 2;

		// The width of a cell in Horizontal Orientation.
		private int CellWidth => Math.Max((int)_charSize.Height + CellHeightPadding * 2, (int)_charSize.Width + CellWidthPadding * 2);

		// The height of a cell in Vertical Orientation.
		private int CellHeight => (int)_charSize.Height + CellHeightPadding * 2;

		/// <summary>
		/// Call when _charSize, MaxCharactersInHorizontal, or CellPadding is changed.
		/// </summary>
		private void UpdateCellSize()
		{
			using (var g = CreateGraphics())
			using (_renderer.LockGraphics(g))
			{
				// Measure width change to ignore extra padding at start/end
				var size1 = _renderer.MeasureString("A", Font);
				var size2 = _renderer.MeasureString("AA", Font);
				_charSize = new SizeF(size2.Width - size1.Width, size1.Height);
			}
			
			if (_columns.VisibleColumns.Any())
			{
				MaxColumnWidth = _columns.VisibleColumns.Max(c => c.Width);
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			UpdateCellSize();
		}

		// SuuperW: Count lag frames between FirstDisplayed and given display position
		private int CountLagFramesDisplay(int relativeIndex)
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				int count = 0;
				for (int i = 0; i <= relativeIndex; i++)
				{
					count += _lagFrames[i];
				}

				return count;
			}

			return 0;
		}

		// Count lag frames between FirstDisplayed and given relative frame index
		private int CountLagFramesAbsolute(int relativeIndex)
		{
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				int count = 0;
				for (int i = 0; i + count <= relativeIndex; i++)
				{
					count += _lagFrames[i];
				}

				return count;
			}

			return 0;
		}

		private void SetLagFramesArray()
		{
			int firstVisibleRow = FirstVisibleRow;
			int visibleRows = VisibleRows;
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				bool showNext = false;

				// First one needs to check BACKWARDS for lag frame count.
				SetLagFramesFirst();
				int f = _lagFrames[0];
				if (QueryFrameLag(firstVisibleRow + f, HideWasLagFrames))
				{
					showNext = true;
				}

				for (int i = 1; i <= visibleRows; i++)
				{
					_lagFrames[i] = 0;
					if (!showNext)
					{
						for (; _lagFrames[i] < LagFramesToHide; _lagFrames[i]++)
						{
							if (!QueryFrameLag(firstVisibleRow + i + f, HideWasLagFrames))
							{
								break;
							}

							f++;
						}
					}
					else
					{
						if (!QueryFrameLag(firstVisibleRow + i + f, HideWasLagFrames))
						{
							showNext = false;
						}
					}

					if (_lagFrames[i] == LagFramesToHide && QueryFrameLag(firstVisibleRow + i + f, HideWasLagFrames))
					{
						showNext = true;
					}
				}
			}
			else
			{
				for (int i = 0; i <= visibleRows; i++)
				{
					_lagFrames[i] = 0;
				}
			}
		}
		private void SetLagFramesFirst()
		{
			int firstVisibleRow = FirstVisibleRow;
			if (QueryFrameLag != null && LagFramesToHide != 0)
			{
				// Count how many lag frames are above displayed area.
				int count = 0;
				do
				{
					count++;
				}
				while (QueryFrameLag(firstVisibleRow - count, HideWasLagFrames) && count <= LagFramesToHide);
				count--;

				// Count forward
				int fCount = -1;
				do
				{
					fCount++;
				}
				while (QueryFrameLag(firstVisibleRow + fCount, HideWasLagFrames) && count + fCount < LagFramesToHide);
				_lagFrames[0] = (byte)fCount;
			}
			else
			{
				_lagFrames[0] = 0;
			}
		}

		// Number of displayed + hidden frames, if fps is as expected
		private int ExpectedDisplayRange()
		{
			return (VisibleRows + 1) * LagFramesToHide;
		}
	}
}
