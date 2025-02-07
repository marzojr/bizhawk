using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class CheckBoxExBase : CheckBox, ICheckBoxOrRadioEx
	{
		private readonly IDictionary<CBOrRBCheckedChangedEventHandler<ICheckBoxOrRadioEx>, EventHandler> _cChangedDelegates = new Dictionary<CBOrRBCheckedChangedEventHandler<ICheckBoxOrRadioEx>, EventHandler>();

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool UseVisualStyleBackColor => base.UseVisualStyleBackColor;

		public new event CBOrRBCheckedChangedEventHandler<ICheckBoxOrRadioEx> CheckedChanged
		{
			add
			{
				if (!_cChangedDelegates.TryGetValue(value, out var d))
				{
					d = (sender, args) => value((ICheckBoxOrRadioEx) sender, args);
					_cChangedDelegates[value] = d;
				}
				base.CheckedChanged += d;
			}
			remove
			{
				base.CheckedChanged -= _cChangedDelegates[value];
				_cChangedDelegates.Remove(value);
			}
		}
	}
}
