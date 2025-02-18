using System.Collections.Generic;
using System.ComponentModel;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ITasMovie : IMovie, INotifyPropertyChanged, IDisposable
	{
		bool BindMarkersToInput { get; set; }
		bool LastPositionStable { get; set; }

		IMovieChangeLog ChangeLog { get; }
		IStateManager TasStateManager { get; }
		Func<string> InputRollSettingsForSave { get; set; }
		string InputRollSettings { get; }
		ITasMovieRecord this[int index] { get; }
		ITasSession TasSession { get; }
		TasMovieMarkerList Markers { get; }
		ITasBranchCollection Branches { get; }
		TasLagLog LagLog { get; }
		IStringLog VerificationLog { get; }
		int LastEditedFrame { get; }

		Action<int> GreenzoneInvalidated { get; set; }

		string DisplayValue(int frame, string buttonName);
		void FlagChanges();
		void ClearChanges();

		/// <summary>
		/// Replaces the given frame's input with an empty frame
		/// </summary>
		void ClearFrame(int frame);

		void ClearFrameMPR(int frame, int startOffset, int currentControlLength);

		void GreenzoneCurrentFrame();
		void ToggleBoolState(int frame, string buttonName);
		void SetAxisState(int frame, string buttonName, int val);
		void SetAxisStates(int frame, int count, string buttonName, int val);
		void SetBoolState(int frame, string buttonName, bool val);
		void SetBoolStates(int frame, int count, string buttonName, bool val);

		void InsertInput(int frame, string inputState);
		void InsertInput(int frame, IEnumerable<string> inputLog);
		void InsertInput(int frame, IEnumerable<IController> inputStates);
		void InsertInputMPR(int frame, IEnumerable<string> inputLog, int startOffset, int currentControlLength);
		void InsertEmptyFrameMPR(int frame, int startOffset, int currentControlLength, int count = 1);
		void InsertEmptyFramesMPR(int frame, int startOffset, int currentControlLength, int count = 1);
		void InsertEmptyFrame(int frame, int count = 1);

		int CopyOverInput(int frame, IEnumerable<IController> inputStates);
		int CopyOverInputMPR(int frame, IEnumerable<IController> inputStates, int startOffset, int currentControlLength);
		int CopyOverDestInputMPR(int frame, IEnumerable<string> frames, int startOffset, int currentControlLength, int sourceStartOffset);

		void RemoveFrame(int frame);
		void RemoveFrames(ICollection<int> frames);
		void RemoveFrames(int removeStart, int removeUpTo);
		void RemoveFramesMPR(ICollection<int> frames, int startOffset, int currentControlLength);

		void TruncateFramesMPR(int frame, int startOffset, int currentControlLength);

		void SetFrame(int frame, string source);

		void LoadBranch(TasBranch branch);

		void CopyVerificationLog(IEnumerable<string> log);
	}
}
