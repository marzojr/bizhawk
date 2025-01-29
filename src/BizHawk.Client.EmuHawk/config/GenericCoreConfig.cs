using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericCoreConfig : Form
	{
		private readonly struct TempTypeDescProviderScope : IDisposable
		{
			private readonly TypeDescriptionProvider _desc;

			private readonly Type _type;

			public TempTypeDescProviderScope(TypeDescriptionProvider desc, Type type)
				=> TypeDescriptor.AddProvider(_desc = desc, _type = type);

			public void Dispose()
				=> TypeDescriptor.RemoveProvider(_desc, _type);
		}

		public static DialogResult DoDialogFor(
			IDialogParent owner,
			ISettingsAdapter settable,
			string title,
			bool isMovieActive,
			bool ignoreSettings = false,
			bool ignoreSyncSettings = false)
		{
			using GenericCoreConfig dlg = new(
				settable,
				isMovieActive: isMovieActive,
				ignoreSettings: ignoreSettings,
				ignoreSyncSettings: ignoreSyncSettings)
			{
				Text = title,
			};
			return owner.ShowDialogAsChild(dlg);
		}

		private static DialogResult DoMAMEDialog(
			IDialogParent owner,
			ISettingsAdapter settable,
			List<MAME.DriverSetting> settings,
			bool isMovieActive)
		{
			using TempTypeDescProviderScope scope = new(new MAMETypeDescriptorProvider(settings), typeof(MAME.MAMESyncSettings));
			return DoDialogFor(owner, settable, "MAME Settings", isMovieActive: isMovieActive, ignoreSettings: true);
		}

		public static DialogResult DoNymaDialogFor(
			IDialogParent owner,
			ISettingsAdapter settable,
			string title,
			NymaCore.NymaSettingsInfo settingsInfo,
			bool isMovieActive)
		{
			NymaTypeDescriptorProvider desc = new(settingsInfo);
			using TempTypeDescProviderScope scope = new(desc, typeof(NymaCore.NymaSettings)), scope1 = new(desc, typeof(NymaCore.NymaSyncSettings));
			return DoDialogFor(
				owner,
				settable,
				title,
				isMovieActive: isMovieActive,
				ignoreSettings: !settingsInfo.HasSettings,
				ignoreSyncSettings: !settingsInfo.HasSyncSettings);
		}

		public static void DoDialog(IEmulator emulator, IDialogParent owner, bool isMovieActive)
		{
			var settable = ((MainForm) owner).GetSettingsAdapterForLoadedCoreUntyped(); //HACK
			var title = $"{emulator.Attributes().CoreName} Settings";
			_ = emulator switch
			{
				MAME mame => DoMAMEDialog(owner, settable, mame.CurrentDriverSettings, isMovieActive: isMovieActive),
				NymaCore core => DoNymaDialogFor(owner, settable, title, core.SettingsInfo, isMovieActive: isMovieActive),
				_ => DoDialogFor(owner, settable, title, isMovieActive: isMovieActive)
			};
		}

		private readonly ISettingsAdapter _settable;

		private object _s;
		private object _ss;
		private bool _syncSettingsChanged;
		private bool _settingsChanged;

		private GenericCoreConfig(
			ISettingsAdapter settable,
			bool isMovieActive,
			bool ignoreSettings = false,
			bool ignoreSyncSettings = false)
		{
			InitializeComponent();

			_settable = settable;

			if (_settable.HasSettings && !ignoreSettings) _s = _settable.GetSettings();

			if (_settable.HasSyncSettings && !ignoreSyncSettings) _ss = _settable.GetSyncSettings();

			if (_s != null)
			{
				propertyGrid1.SelectedObject = _s;
				propertyGrid1.AdjustDescriptionHeightToFit();
				propertyGrid1.EnableCategoriesIfUsed();
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage1);
			}

			if (_ss != null)
			{
				propertyGrid2.SelectedObject = _ss;
				propertyGrid2.AdjustDescriptionHeightToFit();
				propertyGrid2.EnableCategoriesIfUsed();
			}
			else
			{
				tabControl1.TabPages.Remove(tabPage2);
			}

			if (isMovieActive)
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (_s != null && _settingsChanged)
			{
				_settable.PutCoreSettings(_s);
			}

			if (_ss != null && _syncSettingsChanged)
			{
				_settable.PutCoreSyncSettings(_ss);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void PropertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_syncSettingsChanged = true;
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			// the new config objects guarantee that the default constructor gives a default-settings object
			if (_s != null)
			{
				_s = Activator.CreateInstance(_s.GetType());
				propertyGrid1.SelectedObject = _s;
				_settingsChanged = true;
			}

			if (_ss != null)
			{
				_ss = Activator.CreateInstance(_ss.GetType());
				propertyGrid2.SelectedObject = _ss;
				_syncSettingsChanged = true;
			}
		}

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_settingsChanged = true;
		}
	}
}
