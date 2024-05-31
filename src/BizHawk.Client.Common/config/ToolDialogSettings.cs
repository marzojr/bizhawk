using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;

namespace BizHawk.Client.Common
{
	public class ToolDialogSettings
	{
		// one may wonder why the public property is getting JsonIgnored and the private one JsonIncluded
		[JsonInclude]
		[JsonPropertyOrder(-2)]
		private int? _wndx;
		[JsonInclude]
		[JsonPropertyOrder(-1)]
		private int? _wndy;

		public ToolDialogSettings()
		{
			SaveWindowPosition = true;
			FloatingWindow = true;
		}

		public void RestoreDefaults()
		{
			_wndx = null;
			_wndy = null;
			SaveWindowPosition = true;
			FloatingWindow = true;
			TopMost = false;
			AutoLoad = false;
			Width = null;
			Height = null;
		}

		[JsonIgnore]
		public int? Wndx
		{
			get => _wndx;
			set
			{
				if (value != -32000)
				{
					_wndx = value;
				}
			}
		}

		[JsonIgnore]
		public int? Wndy
		{
			get => _wndy;
			set
			{
				if (value != -32000)
				{
					_wndy = value;
				}
			}
		}

		/// <value>the top-left corner of the <see cref="IToolFormAutoConfig"/>, equivalent to the combined values of <see cref="Wndx"/> and <see cref="Wndy"/></value>
		/// <exception cref="InvalidOperationException">either <see cref="Wndx"/> or <see cref="Wndy"/> is null (it is expected to check for this before using this property)</exception>
		[JsonIgnore]
		public Point TopLeft
		{
			get
			{
				if (_wndx.HasValue && _wndy.HasValue)
				{
					return new Point(_wndx.Value, _wndy.Value);
				}

				throw new InvalidOperationException($"{nameof(TopLeft)} can not be used when one of the coordinates is null");
			}
		}

		public int? Width { get; set; }
		public int? Height { get; set; }

		public bool SaveWindowPosition { get; set; }
		public bool TopMost { get; set; }
		public bool FloatingWindow { get; set; }
		public bool AutoLoad { get; set; }

		[JsonIgnore]
		public bool UseWindowPosition => SaveWindowPosition && Wndx.HasValue
			&& Wndy.HasValue
			&& Wndx != -32000 && Wndy != -32000;

		[JsonIgnore]
		public bool UseWindowSize => SaveWindowPosition && Width.HasValue && Height.HasValue;

		[JsonIgnore]
		public Point WindowPosition => new Point(Wndx ?? 0, Wndy ?? 0);

		[JsonIgnore]
		public Size WindowSize => new Size(Width ?? 0, Height ?? 0);

		public class ColumnList : List<Column>
		{
			public Column this[string name] => this.FirstOrDefault(c => c.Name == name);
		}

		public class Column
		{
			public string Name { get; set; }
			public int Width { get; set; }
			public bool Visible { get; set; }
			public int Index { get; set; }
		}
	}
}
