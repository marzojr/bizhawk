namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioMPR : IControlMainform
	{
		private bool _suppressAskSave;

		public bool NamedStatePending { get; set; }

		public bool WantsToControlSavestates => !NamedStatePending;

		public void SaveState()
		{
			BookMarkControlMPR.UpdateBranchExternal();
		}

		public bool LoadState()
			=> BookMarkControlMPR.LoadBranchExternal();

		public void SaveStateAs()
		{
			// dummy
		}

		public bool LoadStateAs()
			=> false;

		public void SaveQuickSave(int slot)
			=> BookMarkControlMPR.UpdateBranchExternal(slot - 1);

		public bool LoadQuickSave(int slot)
			=> BookMarkControlMPR.LoadBranchExternal(slot - 1);

		public bool SelectSlot(int slot)
		{
			BookMarkControlMPR.SelectBranchExternal(slot - 1);
			return false;
		}

		public bool PreviousSlot()
		{
			BookMarkControlMPR.SelectBranchExternal(false);
			return false;
		}

		public bool NextSlot()
		{
			BookMarkControlMPR.SelectBranchExternal(true);
			return false;
		}

		public bool WantsToControlReadOnly => true;

		public void ToggleReadOnly()
		{
			TastudioToggleReadOnly();
		}

		public bool WantsToControlStopMovie { get; private set; }

		public void StopMovie(bool suppressSave)
		{
			if (!MainForm.GameIsClosing)
			{
				Activate();
				_suppressAskSave = suppressSave;
				StartNewTasMovie();
				_suppressAskSave = false;
			}
		}

		public bool WantsToControlRewind { get; private set; } = true;

		public void CaptureRewind()
		{
			// Do nothing, Tastudio handles this just fine
		}

		public bool Rewind()
		{
			// copy pasted from TasView_MouseWheel(), just without notch logic
			if (MainForm.IsSeeking && !MainForm.EmulatorPaused)
			{
				MainForm.PauseOnFrame--;

				// that's a weird condition here, but for whatever reason it works best
				if (Emulator.Frame >= MainForm.PauseOnFrame)
				{
					MainForm.PauseEmulator();
					StopSeeking();
					GoToPreviousFrame();
				}

				RefreshDialog();
			}
			else
			{
				StopSeeking(); // late breaking memo: don't know whether this is needed
				GoToPreviousFrame();
			}

			return true;
		}

		public bool WantsToControlRestartMovie { get; }

		public bool RestartMovie()
		{
			if (!AskSaveChanges()) return false;
			var success = StartNewMovieWrapper(CurrentTasMovie, isNew: false);
			RefreshDialog();
			return success;
		}

		public bool WantsToControlReboot => false;
		public void RebootCore() => throw new NotSupportedException("This should never be called");
	}
}
