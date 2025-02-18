using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioMPR
	{
		/// <summary>
		/// Only goes to go to the frame if it is an event before current emulation, otherwise it is just a future event that can freely be edited
		/// </summary>
		private void GoToLastEmulatedFrameIfNecessary(int frame, bool OnLeftMouseDown = false)
		{
			if (frame != Emulator.Frame) // Don't go to a frame if you are already on it!
			{
				if (frame <= Emulator.Frame)
				{
					if ((MainForm.EmulatorPaused || !MainForm.IsSeeking)
						&& !CurrentTasMovie.LastPositionStable && !_playbackInterrupted)
					{
						LastPositionFrame = Emulator.Frame;
						CurrentTasMovie.LastPositionStable = true; // until new frame is emulated
					}

					GoToFrame(frame, false, false, OnLeftMouseDown);
				}
				else
				{
					_triggerAutoRestore = false;
				}
			}
		}

		public void GoToFrame(int frame, bool fromLua = false, bool fromRewinding = false, bool OnLeftMouseDown = false)
		{
			// If seeking to a frame before or at the end of the movie, use StartAtNearestFrameAndEmulate
			// Otherwise, load the latest state (if not already there) and seek while recording.
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;

			if (frame <= CurrentTasMovie.InputLogLength)
			{
				// Get as close as we can then emulate there
				StartAtNearestFrameAndEmulate(frame, fromLua, fromRewinding);
				if (!OnLeftMouseDown) { MaybeFollowCursor(); }			
			}
			else // Emulate to a future frame
			{
				if (frame == Emulator.Frame + 1) // We are at the end of the movie and advancing one frame, therefore we are recording, simply emulate a frame
				{
					bool wasPaused = MainForm.EmulatorPaused;
					MainForm.FrameAdvance();
					if (!wasPaused)
					{
						MainForm.UnpauseEmulator();
					}
				}
				else
				{
					TastudioPlayMode();

					var lastState = GetPriorStateForFramebuffer(frame);
					if (lastState.Key > Emulator.Frame)
					{
						LoadState(lastState, true);
					}

					StartSeeking(frame);
				}
			}
		}

		public void GoToPreviousFrame()
		{
			if (Emulator.Frame > 0)
			{
				GoToFrame(Emulator.Frame - 1);
			}
		}

		public void GoToPreviousMarker()
		{
			if (Emulator.Frame > 0)
			{
				var prevMarker = CurrentTasMovie.Markers.Previous(Emulator.Frame);
				var prev = prevMarker?.Frame ?? 0;
				GoToFrame(prev);
			}
		}

		public void GoToNextMarker()
		{
			var nextMarker = CurrentTasMovie.Markers.Next(Emulator.Frame);
			var next = nextMarker?.Frame ?? CurrentTasMovie.InputLogLength - 1;
			GoToFrame(next);
		}

		/// <summary>
		/// Makes the given frame visible. If no frame is given, makes the current frame visible.
		/// </summary>
		public void SetVisibleFrame(int? frame = null)
		{
			if (TasView1.AlwaysScroll && _leftButtonHeld)
			{
				return;
			}
			foreach(InputRoll tasView in TasViews)
			{
				tasView.ScrollToIndex(frame ?? Emulator.Frame);
			}
			
		}

		private void MaybeFollowCursor()
		{
			if (TasPlaybackBoxMPR.FollowCursor)
			{
				SetVisibleFrame();
			}
		}
	}
}
