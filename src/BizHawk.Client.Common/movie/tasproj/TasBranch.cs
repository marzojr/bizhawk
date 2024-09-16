using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using BizHawk.Bizware.Graphics;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Client.Common
{
	public class TasBranch
	{
		internal struct ForSerialization
		{
			public readonly int Frame;

			public readonly DateTime TimeStamp;

			public readonly Guid UniqueIdentifier;

			public ForSerialization(int frame, DateTime timeStamp, Guid uniqueIdentifier)
			{
				Frame = frame;
				TimeStamp = timeStamp;
				UniqueIdentifier = uniqueIdentifier;
			}
		}

		public int Frame { get; set; }
		public byte[] CoreData { get; set; }
		public IStringLog InputLog { get; set; }
		public BitmapBuffer CoreFrameBuffer { get; set; }
		public BitmapBuffer OSDFrameBuffer { get; set; }
		public TasMovieChangeLog ChangeLog { get; set; }
		public DateTime TimeStamp { get; set; }
		public TasMovieMarkerList Markers { get; set; }
		public Guid Uuid { get; set; }
		public string UserText { get; set; }

		internal ForSerialization ForSerial => new(Frame, TimeStamp, Uuid);

		public TasBranch Clone() => (TasBranch)MemberwiseClone();
	}

	public interface ITasBranchCollection : IList<TasBranch>
	{
		int Current { get; set; }
		string NewBranchText { get; set; }

		void Swap(int b1, int b2);
		void Replace(TasBranch old, TasBranch newBranch);

		void Save(ZipStateSaver bs);
		void Load(ZipStateLoader bl, ITasMovie movie);
	}

	public class TasBranchCollection : List<TasBranch>, ITasBranchCollection
	{
		private readonly ITasMovie _movie;

		public TasBranchCollection(ITasMovie movie)
		{
			_movie = movie;
		}

		public int Current { get; set; } = -1;
		public string NewBranchText { get; set; } = "";

		public void Swap(int b1, int b2)
		{
			var branch = this[b1];

			if (b2 >= Count)
			{
				b2 = Count - 1;
			}

			Remove(branch);
			Insert(b2, branch);
			_movie.FlagChanges();
		}

		public void Replace(TasBranch old, TasBranch newBranch)
		{
			int index = IndexOf(old);
			newBranch.Uuid = old.Uuid;
			if (newBranch.UserText == "")
			{
				newBranch.UserText = old.UserText;
			}

			this[index] = newBranch;
			_movie.FlagChanges();
		}

		public new TasBranch this[int index]
		{
			get => index >= Count || index < 0
				? null
				: base [index];
			set => base[index] = value;
		}

		public new void Add(TasBranch item)
		{
			if (item is null) throw new ArgumentNullException(paramName: nameof(item));

			if (item.Uuid == Guid.Empty)
			{
				item.Uuid = Guid.NewGuid();
			}

			base.Add(item);
			_movie.FlagChanges();
		}

		public new bool Remove(TasBranch item)
		{
			var result = base.Remove(item);
			if (result)
			{
				_movie.FlagChanges();
			}

			return result;
		}

		public void Save(ZipStateSaver bs)
		{
			var nheader = new IndexedStateLump(BinaryStateLump.BranchHeader);
			var ncore = new IndexedStateLump(BinaryStateLump.BranchCoreData);
			var ninput = new IndexedStateLump(BinaryStateLump.BranchInputLog);
			var nframebuffer = new IndexedStateLump(BinaryStateLump.BranchFrameBuffer);
			var ncoreframebuffer = new IndexedStateLump(BinaryStateLump.BranchCoreFrameBuffer);
			var nmarkers = new IndexedStateLump(BinaryStateLump.BranchMarkers);
			var nusertext = new IndexedStateLump(BinaryStateLump.BranchUserText);
			foreach (var b in this)
			{
				bs.PutLump(nheader, tw => tw.WriteLine(JsonSerializer.Serialize(b.ForSerial)));

				bs.PutLump(ncore, (Stream s) => s.Write(b.CoreData, 0, b.CoreData.Length));

				bs.PutLump(ninput, tw =>
				{
					int todo = b.InputLog.Count;
					for (int i = 0; i < todo; i++)
					{
						tw.WriteLine(b.InputLog[i]);
					}
				});

				bs.PutLump(nframebuffer, s =>
				{
					var vp = new BitmapBufferVideoProvider(b.OSDFrameBuffer);
					QuickBmpFile.Save(vp, s, b.OSDFrameBuffer.Width, b.OSDFrameBuffer.Height);
				});

				bs.PutLump(ncoreframebuffer, s =>
				{
					var vp = new BitmapBufferVideoProvider(b.CoreFrameBuffer);
					QuickBmpFile.Save(vp, s, b.CoreFrameBuffer.Width, b.CoreFrameBuffer.Height);
				});

				bs.PutLump(nmarkers, tw => tw.WriteLine(b.Markers.ToString()));

				bs.PutLump(nusertext, tw => tw.WriteLine(b.UserText));

				nheader.Increment();
				ncore.Increment();
				ninput.Increment();
				nframebuffer.Increment();
				ncoreframebuffer.Increment();
				nmarkers.Increment();
				nusertext.Increment();
			}
		}

		public void Load(ZipStateLoader bl, ITasMovie movie)
		{
			var nheader = new IndexedStateLump(BinaryStateLump.BranchHeader);
			var ncore = new IndexedStateLump(BinaryStateLump.BranchCoreData);
			var ninput = new IndexedStateLump(BinaryStateLump.BranchInputLog);
			var nframebuffer = new IndexedStateLump(BinaryStateLump.BranchFrameBuffer);
			var ncoreframebuffer = new IndexedStateLump(BinaryStateLump.BranchCoreFrameBuffer);
			var nmarkers = new IndexedStateLump(BinaryStateLump.BranchMarkers);
			var nusertext = new IndexedStateLump(BinaryStateLump.BranchUserText);

			Clear();

			while (true)
			{
				var b = new TasBranch();

				if (!bl.GetLump(nheader, abort: false, tr =>
				{
					var header = JsonSerializer.Deserialize<JsonNode>(tr.ReadLine());
					b.Frame = header["Frame"]!.GetValue<int>();

					var timestamp = header["TimeStamp"];

					b.TimeStamp = timestamp?.GetValue<DateTime>() ?? DateTime.Now;

					var identifier = header["UniqueIdentifier"];
					b.Uuid = identifier?.GetValue<Guid>() ?? Guid.NewGuid();
				}))
				{
					return;
				}

				bl.GetLump(ncore, abort: true, (s, _) =>
				{
					b.CoreData = s.ReadAllBytes();
				});

				bl.GetLump(ninput, abort: true, tr =>
				{
					b.InputLog = StringLogUtil.MakeStringLog();
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						b.InputLog.Add(line);
					}
				});

				bl.GetLump(nframebuffer, abort: true, (s, _) =>
				{
					QuickBmpFile.LoadAuto(s, out var vp);
					b.OSDFrameBuffer = new BitmapBuffer(vp.BufferWidth, vp.BufferHeight, vp.GetVideoBuffer());
				});

				bl.GetLump(ncoreframebuffer, abort: false, (s, _) =>
				{
					QuickBmpFile.LoadAuto(s, out var vp);
					b.CoreFrameBuffer = new BitmapBuffer(vp.BufferWidth, vp.BufferHeight, vp.GetVideoBuffer());
				});

				b.Markers = new TasMovieMarkerList(movie);
				bl.GetLump(nmarkers, abort: false, tr =>
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							b.Markers.Add(new TasMovieMarker(line));
						}
					}
				});

				bl.GetLump(nusertext, abort: false, tr =>
				{
					string line;
					if ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							b.UserText = line;
						}
					}
				});

				Add(b);

				nheader.Increment();
				ncore.Increment();
				ninput.Increment();
				nframebuffer.Increment();
				ncoreframebuffer.Increment();
				nmarkers.Increment();
				nusertext.Increment();
			}
		}
	}

	public static class TasBranchExtensions
	{
		public static int IndexOfFrame(this IList<TasBranch> list, int frame)
		{
			// intentionally not using linq here because this is called many times per frame
			int index = -1;
			var timeStamp = DateTime.MinValue;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Frame == frame && list[i].TimeStamp > timeStamp)
				{
					index = i;
					timeStamp = list[i].TimeStamp;
				}
			}

			return index;
		}

		// TODO: stop relying on the index value of a branch
		public static int IndexOfHash(this IList<TasBranch> list, Guid uuid)
		{
			var branch = list.SingleOrDefault(b => b.Uuid == uuid);
			return branch == null
				? -1
				: list.IndexOf(branch);
		}
	}
}
