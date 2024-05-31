using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BizHawk.Client.Common
{
	public class RecentFiles
	{
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private List<string> recentlist;

		public RecentFiles()
			: this(8)
		{
		}

		public RecentFiles(int max)
		{
			recentlist = new List<string>();
			MAX_RECENT_FILES = max;
		}

		public int MAX_RECENT_FILES { get; set; }
		public bool AutoLoad { get; set; }

		/// <summary>
		/// If true, the list can not change, or be cleared
		/// </summary>
		public bool Frozen { get; set; }

		[JsonIgnore]
		public bool Empty => !recentlist.Any();

		[JsonIgnore]
		public int Count => recentlist.Count;

		[JsonIgnore]
		public string MostRecent => recentlist.Any() ? recentlist[0] : "";

		public string this[int index] => recentlist.Any() ? recentlist[index] : "";

		public IEnumerator<string> GetEnumerator() => recentlist.GetEnumerator();

		public void Clear()
		{
			if (!Frozen)
			{
				recentlist.Clear();
			}
		}

		public void Add(string newFile)
		{
			if (!Frozen)
			{
				Remove(newFile);
				recentlist.Insert(0, newFile);

				if (recentlist.Count > MAX_RECENT_FILES)
				{
					recentlist.RemoveAt(recentlist.Count - 1);
				}
			}
		}

		public bool Remove(string newFile)
		{
			if (!Frozen)
			{
				return recentlist.RemoveAll(recent => string.Equals(newFile, recent, StringComparison.OrdinalIgnoreCase)) != 0; // none removed => return false
			}

			return false;
		}

		public void ToggleAutoLoad()
			=> AutoLoad = !AutoLoad;
	}
}
