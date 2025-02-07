using System;

namespace BizHawk.WinForms.Controls
{
	public delegate void CBOrRBCheckedChangedEventHandler<in TCheckBox>(TCheckBox sender, EventArgs args)
		where TCheckBox : ICheckBoxOrRadioEx;
}
