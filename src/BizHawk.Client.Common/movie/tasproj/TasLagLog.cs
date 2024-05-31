using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.Common
{
	public class TasLagLog
	{
		private Dictionary<int, bool> _lagLog = new Dictionary<int, bool>();
		private Dictionary<int, bool> _wasLag = new Dictionary<int, bool>();

		public bool? this[int frame]
		{
			get
			{
				var result = _lagLog.TryGetValue(frame, out var lag);
				return result ? lag : null;
			}

			set
			{
				if (!value.HasValue)
				{
					RemoveLagEntry(frame);
					return;
				}

				_lagLog[frame] = value.Value;
			}
		}

		public void Clear()
		{
			_wasLag.Clear();
			_lagLog.Clear();
		}

		public bool RemoveFrom(int frame)
		{
			var frames = _lagLog.Keys.Where(k => k > frame).ToList();
			foreach (var f in frames)
			{
				RemoveLagEntry(f);
			}

			return frames.Any();
		}

		public void RemoveHistoryAt(int frame)
		{
			_wasLag.Remove(frame);
		}

		public void InsertHistoryAt(int frame, bool isLag)
		{
			_lagLog[frame] = isLag;
			_wasLag[frame] = isLag;
		}

		public void Save(TextWriter tw)
		{
			tw.WriteLine(JsonSerializer.Serialize(_lagLog));
			tw.WriteLine(JsonSerializer.Serialize(_wasLag));
		}

		public void Load(TextReader tr)
		{
			_lagLog = JsonSerializer.Deserialize<Dictionary<int, bool>>(tr.ReadLine());
			_wasLag = JsonSerializer.Deserialize<Dictionary<int, bool>>(tr.ReadLine());
		}

		public bool? History(int frame)
		{
			var result = _wasLag.TryGetValue(frame, out var wasLag);
			if (result)
			{
				return wasLag;
			}

			return null;
		}

		public void FromLagLog(TasLagLog log)
		{
			_lagLog = log._lagLog.ToDictionary();
			_wasLag = log._wasLag.ToDictionary();
		}

		public void StartFromFrame(int index)
		{
			for (int i = 0; i < index; i++)
			{
				_lagLog.Remove(i);
				_wasLag.Remove(i);
			}
		}

		private void RemoveLagEntry(int frame)
		{
			var result = _lagLog.TryGetValue(frame, out var lag);
			if (result)
			{
				_wasLag[frame] = lag;
			}

			_lagLog.Remove(frame);
		}
	}
}
