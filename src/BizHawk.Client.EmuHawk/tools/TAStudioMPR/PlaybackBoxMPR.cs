using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlaybackBoxMPR : UserControl
	{
		private bool _loading = true;

		public TAStudioMPR TastudioMPR { get; set; }

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool TurboSeek
		{
			get => TastudioMPR.Config.TurboSeek;
			set => TurboSeekCheckbox.Checked = value;
		}

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool AutoRestore
		{
			get => TastudioMPR.Settings.AutoRestoreLastPosition;
			set => AutoRestoreCheckbox.Checked = value;
		}

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool FollowCursor
		{
			get => TastudioMPR.Settings.FollowCursor;
			set => FollowCursorCheckbox.Checked = value;
		}

		[Browsable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool RecordingMode
		{
			get => TastudioMPR.CurrentTasMovie.IsRecording();
			set
			{
				RecordingModeCheckbox.Checked = value;
				TastudioMPR.MovieSession.ReadOnly = !value;
				if (RecordingModeCheckbox.Checked)
				{
					TastudioMPR.CurrentTasMovie.SwitchToRecord();
				}
				else
				{
					TastudioMPR.CurrentTasMovie.SwitchToPlay();
				}

				TastudioMPR.MainForm.SetMainformMovieInfo();
			}
		}

		public PlaybackBoxMPR()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (DesignMode)
			{
				return;
			}

			TurboSeekCheckbox.Checked = TastudioMPR.Config?.TurboSeek ?? false;
			AutoRestoreCheckbox.Checked = TastudioMPR.Settings.AutoRestoreLastPosition;
			FollowCursorCheckbox.Checked = TastudioMPR.Settings.FollowCursor;
			RecordingModeCheckbox.Checked = RecordingMode;

			_loading = false;
		}

		private void PreviousMarkerButton_Click(object sender, EventArgs e)
		{
			TastudioMPR.GoToPreviousMarker();
		}

		private void PauseButton_Click(object sender, EventArgs e)
		{
			TastudioMPR.TogglePause();
		}

		private void NextMarkerButton_Click(object sender, EventArgs e)
		{
			TastudioMPR.GoToNextMarker();
		}

		private void TurboSeekCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading) TastudioMPR.Config.TurboSeek = !TastudioMPR.Config.TurboSeek;
		}

		private void AutoRestoreCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading) TastudioMPR.Settings.AutoRestoreLastPosition = !TastudioMPR.Settings.AutoRestoreLastPosition;
		}

		private void FollowCursorCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				TastudioMPR.Settings.FollowCursor = !TastudioMPR.Settings.FollowCursor;
				if (TastudioMPR.Settings.FollowCursor)
				{
					TastudioMPR.SetVisibleFrame();
					TastudioMPR.RefreshDialog();
				}
			}
		}

		private void RecordingModeCheckbox_MouseClick(object sender, MouseEventArgs e)
		{
			RecordingMode = !RecordingMode;
			TastudioMPR.WasRecording = RecordingMode; // hard reset at manual click and hotkey
		}

		private void RewindButton_MouseDown(object sender, MouseEventArgs e)
		{
			TastudioMPR.MainForm.PressRewind = true;
		}

		private void RewindButton_MouseUp(object sender, MouseEventArgs e)
		{
			TastudioMPR.MainForm.PressRewind = false;
		}

		private void RewindButton_MouseLeave(object sender, EventArgs e)
		{
			TastudioMPR.MainForm.PressRewind = false;
		}

		private void FrameAdvanceButton_MouseDown(object sender, MouseEventArgs e)
		{
			TastudioMPR.MainForm.HoldFrameAdvance = true;
		}

		private void FrameAdvanceButton_MouseLeave(object sender, EventArgs e)
		{
			TastudioMPR.MainForm.HoldFrameAdvance = false;
		}

		private void FrameAdvanceButton_MouseUp(object sender, MouseEventArgs e)
		{
			TastudioMPR.MainForm.HoldFrameAdvance = false;
		}
	}
}
