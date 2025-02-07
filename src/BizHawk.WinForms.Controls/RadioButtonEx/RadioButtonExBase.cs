using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	public abstract class RadioButtonExBase : RadioButton, ICheckBoxOrRadioEx, ITrackedRadioButton
	{
		private readonly IDictionary<CBOrRBCheckedChangedEventHandler<ICheckBoxOrRadioEx>, EventHandler> _cChangedDelegates = new Dictionary<CBOrRBCheckedChangedEventHandler<ICheckBoxOrRadioEx>, EventHandler>();

		/// <remarks>use to prevent recursion</remarks>
		protected bool CheckedChangedCausedByTracker { get; private set; }

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int TabIndex => base.TabIndex;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool TabStop => base.TabStop;

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

		protected RadioButtonExBase() {}

		protected RadioButtonExBase(IRadioButtonReadOnlyTracker tracker)
		{
			tracker.Add(this);
			CheckedChanged += (changedSender, _) =>
			{
				if (changedSender.Checked) tracker.UpdateDeselected(Name);
			};
		}

		public void UncheckFromTracker()
		{
			CheckedChangedCausedByTracker = true;
			Checked = false;
			CheckedChangedCausedByTracker = false;
		}
	}
}
