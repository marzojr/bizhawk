namespace BizHawk.WinForms.Controls
{
	public interface ICheckBoxOrRadioEx
	{
		bool Checked { get; set; }

		event CBOrRBCheckedChangedEventHandler<ICheckBoxOrRadioEx> CheckedChanged;
	}
}
