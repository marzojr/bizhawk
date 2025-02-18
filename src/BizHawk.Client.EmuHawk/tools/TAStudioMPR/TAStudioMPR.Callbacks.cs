using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioMPR
	{
		// Everything here is currently for Lua
		public Func<int, string, Color?> QueryItemBgColorCallback { get; set; }
		public Func<int, string, string> QueryItemTextCallback { get; set; }
		public Func<int, string, Bitmap> QueryItemIconCallback { get; set; }

		public Action<int> GreenzoneInvalidatedCallback { get; set; }
		public Action<int> BranchLoadedCallback { get; set; }
		public Action<int> BranchSavedCallback { get; set; }
		public Action<int> BranchRemovedCallback { get; set; }

		private void GreenzoneInvalidated(int index)
		{
			GreenzoneInvalidatedCallback?.Invoke(index);
		}

		private void BranchLoaded(int index)
		{
			BranchLoadedCallback?.Invoke(index);
		}

		private void BranchSaved(int index)
		{
			BranchSavedCallback?.Invoke(index);
		}

		private void BranchRemoved(int index)
		{
			BranchRemovedCallback?.Invoke(index);
		}
	}
}
