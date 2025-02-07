using System;

namespace BizHawk.WinForms.Controls
{
	public delegate void ButtonClickEventHandler<in TButton>(TButton sender, EventArgs args)
		where TButton : ButtonExBase;
}
