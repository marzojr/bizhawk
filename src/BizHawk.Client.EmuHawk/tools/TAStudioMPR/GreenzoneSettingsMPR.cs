using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GreenzoneSettingsMPR : Form, IDialogParent
	{
		private readonly Action<ZwinderStateManagerSettings, bool> _saveSettings;
		private ZwinderStateManagerSettings _settings;
		private readonly bool _isDefault;

		public IDialogController DialogController { get; }

		public GreenzoneSettingsMPR(IDialogController dialogController, ZwinderStateManagerSettings settings, Action<ZwinderStateManagerSettings, bool> saveSettings, bool isDefault)
		{
			DialogController = dialogController;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;

			_saveSettings = saveSettings;
			_settings = settings;
			_isDefault = isDefault;
			if (!isDefault)
				Text = "Savestate History Settings";
			SettingsPropertyGrid.SelectedObject = _settings;
		}

		private void GreenzoneSettings_Load(object sender, EventArgs e)
		{
			SettingsPropertyGrid.AdjustDescriptionHeightToFit();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var keep = !_isDefault && DialogController.ShowMessageBox2("Attempt to keep old states?", "Keep old states?");
			_saveSettings(_settings, keep);
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void DefaultsButton_Click(object sender, EventArgs e)
		{
			_settings = new ZwinderStateManagerSettings();
			SettingsPropertyGrid.SelectedObject = _settings;
		}
	}
}
