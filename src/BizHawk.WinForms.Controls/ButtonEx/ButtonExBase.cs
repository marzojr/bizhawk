using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class ButtonExBase : Button
	{
		private readonly IDictionary<ButtonClickEventHandler<ButtonExBase>, EventHandler> _clickDelegates = new Dictionary<ButtonClickEventHandler<ButtonExBase>, EventHandler>();

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool UseVisualStyleBackColor => base.UseVisualStyleBackColor;

		public new event ButtonClickEventHandler<ButtonExBase> Click
		{
			add
			{
				if (!_clickDelegates.TryGetValue(value, out var d))
				{
					d = (sender, args) => value((ButtonExBase) sender, args);
					_clickDelegates[value] = d;
				}
				base.Click += d;
			}
			remove
			{
				base.Click -= _clickDelegates[value];
				_clickDelegates.Remove(value);
			}
		}
	}
}
