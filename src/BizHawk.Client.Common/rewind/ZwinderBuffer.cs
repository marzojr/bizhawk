using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderBuffer : IDisposable
	{
		/*
		Main goals:
		1. No copies, ever.  States are deposited directly to, and read directly from, one giant ring buffer.
			As a consequence, there is no multi-threading because there is nothing to thread.
		2. Support for arbitrary and changeable state sizes.  Frequency is calculated dynamically.
		3. No delta compression.  Keep it simple.  If there are cores that benefit heavily from delta compression, we should
			maintain a separate rewinder alongside this one that is customized for those cores.
		*/
		public ZwinderBuffer(IRewindSettings settings)
		{
			if (settings == null)
				throw new ArgumentException("ZwinderBuffer's settings cannot be null.");

			long targetSize = settings.BufferSize * 1024 * 1024;
			if (settings.TargetFrameLength < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(settings.TargetFrameLength));
			}

			Size = 1L << (int)Math.Floor(Math.Log(targetSize, 2));
			_sizeMask = Size - 1;
			_backingStoreType = settings.BackingStore;
			switch (settings.BackingStore)
			{
				case IRewindSettings.BackingStoreType.Memory:
				{
					var buffer = new MemoryBlock((ulong)Size);
					buffer.Protect(buffer.Start, buffer.Size, MemoryBlock.Protection.RW);
					_disposables.Add(buffer);
					_backingStore = new MemoryViewStream(true, true, (long)buffer.Start, (long)buffer.Size);
					_disposables.Add(_backingStore);
					break;
				}
				case IRewindSettings.BackingStoreType.TempFile:
				{
					var filename = TempFileManager.GetTempFilename("ZwinderBuffer");
					var filestream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
					filestream.SetLength(Size);
					_backingStore = filestream;
					_disposables.Add(filestream);
					break;
				}
				default:
					throw new ArgumentException("Unsupported store type for ZwinderBuffer.");
			}
			if (settings.UseFixedRewindInterval)
			{
				_fixedRewindInterval = true;
				_targetRewindInterval = settings.TargetRewindInterval;
			}
			else
			{
				_fixedRewindInterval = false;
				_targetFrameLength = settings.TargetFrameLength;
			}
			_states = new StateInfo[STATEMASK + 1];
			_useCompression = settings.UseCompression;
		}

		public void Dispose()
		{
			foreach (var d in (_disposables as IEnumerable<IDisposable>).Reverse())
				d.Dispose();
			_disposables.Clear();
		}

		private readonly List<IDisposable> _disposables = new List<IDisposable>();

		/// <summary>
		/// Number of states that could be in the state ringbuffer, Mask for the state ringbuffer
		/// </summary>
		private const int STATEMASK = 262143;

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count => (_nextStateIndex - _firstStateIndex) & STATEMASK;

		/// <summary>
		/// total number of bytes used
		/// </summary>
		/// <value></value>
		public long Used => Count == 0
			? 0
			: (_states[HeadStateIndex].Start
				+ _states[HeadStateIndex].Size
				- _states[_firstStateIndex].Start
			) & _sizeMask;

		/// <summary>
		/// Total size of the _buffer
		/// </summary>
		/// <value></value>
		public long Size { get; }

		private readonly long _sizeMask;

		private readonly bool _fixedRewindInterval;
		private readonly int _targetFrameLength;
		private readonly int _targetRewindInterval;

		private struct StateInfo
		{
			public long Start;
			public int Size;
			public int Frame;
		}

		private readonly Stream _backingStore;
		// this is only used to compare settings with a RewindConfig
		private readonly IRewindSettings.BackingStoreType _backingStoreType;

		private readonly StateInfo[] _states;
		private int _firstStateIndex;
		private int _nextStateIndex;
		private int HeadStateIndex => (_nextStateIndex - 1) & STATEMASK;

		private readonly bool _useCompression;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency => ComputeIdealRewindInterval();

		private int ComputeIdealRewindInterval()
		{
			if (Count == 0)
			{
				return 1; // shrug
			}
			
			if (_fixedRewindInterval)
			{
				return _targetRewindInterval;
			}

			// assume that the most recent state size is representative of stuff
			var sizeRatio = Size / (float)_states[HeadStateIndex].Size;
			var frameRatio = _targetFrameLength / sizeRatio;

			var idealInterval = (int)Math.Round(frameRatio);
			return Math.Max(idealInterval, 1);
		}

		public bool MatchesSettings(RewindConfig settings)
		{
			long targetSize = settings.BufferSize * 1024 * 1024;
			long size = 1L << (int)Math.Floor(Math.Log(targetSize, 2));
			return Size == size &&
				_useCompression == settings.UseCompression &&
				_fixedRewindInterval == settings.UseFixedRewindInterval &&
				(_fixedRewindInterval ? _targetRewindInterval == settings.TargetRewindInterval : _targetFrameLength == settings.TargetFrameLength) &&
				_backingStoreType == settings.BackingStore;
		}

		private bool ShouldCaptureForFrameDiff(int frameDiff)
		{
			if (Count == 0)
			{
				return true;
			}
			if (frameDiff < 1)
			{
				// non-linear time is from a combination of other state changing mechanisms and the rewinder
				// not much we can say here, so just take a state
				return true;
			}
			return frameDiff >= ComputeIdealRewindInterval();
		}

		private bool ShouldCapture(int frame)
		{
			var frameDiff = frame - _states[HeadStateIndex].Frame;
			return ShouldCaptureForFrameDiff(frameDiff);
		}

		/// <summary>
		/// Predict whether Capture() would capture a state, assuming a particular frame delta.
		/// </summary>
		/// <param name="frameDelta">The assumed frame delta.  Normally this will be equal to `nextStateFrame - GetState(Count - 1).Frame`.</param>
		/// <returns>Whether Capture(nextStateFrame) would actually capture, assuming the frameDelta matched.</returns>
		public bool WouldCapture(int frameDelta)
		{
			return ShouldCaptureForFrameDiff(frameDelta);
		}

		/// <summary>
		/// Maybe captures a state, if the conditions are favorable
		/// </summary>
		/// <param name="frame">frame number to capture</param>
		/// <param name="callback">will be called with the stream if capture is to be performed</param>
		/// <param name="indexInvalidated">
		/// If provided, will be called with the index of states that are about to be removed.  This will happen during
		/// calls to Write() inside `callback`, and any reading of the old state must be finished before this returns.
		/// </param>
		public void Capture(int frame, Action<Stream> callback, Action<int> indexInvalidated = null, bool force = false)
		{
			if (!force && !ShouldCapture(frame))
				return;

			if (Count == STATEMASK)
			{
				indexInvalidated?.Invoke(0);
				_firstStateIndex = (_firstStateIndex + 1) & STATEMASK;
			}

			var start = (_states[HeadStateIndex].Start + _states[HeadStateIndex].Size) & _sizeMask;
			var initialMaxSize = Count > 0
					? (_states[_firstStateIndex].Start - start) & _sizeMask
					: Size;
			Func<long> notifySizeReached = () =>
			{
				if (Count == 0)
					throw new IOException("A single state must not be larger than the buffer");
				indexInvalidated?.Invoke(0);
				_firstStateIndex = (_firstStateIndex + 1) & STATEMASK;
				return Count > 0
					? (_states[_firstStateIndex].Start - start) & _sizeMask
					: Size;
			};
			var stream = new SaveStateStream(_backingStore, start, _sizeMask, initialMaxSize, notifySizeReached);

			if (_useCompression)
			{
				using var compressor = new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: true);
				callback(compressor);
			}
			else
			{
				callback(stream);
			}

			_states[_nextStateIndex].Frame = frame;
			_states[_nextStateIndex].Start = start;
			_states[_nextStateIndex].Size = (int)stream.Length;
			_nextStateIndex = (_nextStateIndex + 1) & STATEMASK;

			//Util.DebugWriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		private Stream MakeLoadStream(int index)
		{
			Stream stream = new LoadStateStream(_backingStore, _states[index].Start, _states[index].Size, _sizeMask);
			if (_useCompression)
				stream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true);
			return stream;
		}

		public class StateInformation
		{
			private readonly int _index;
			public int Frame => _parent._states[_index].Frame;
			public int Size => _parent._states[_index].Size;
			private readonly ZwinderBuffer _parent;
			public Stream GetReadStream()
			{
				return _parent.MakeLoadStream(_index);
			}
			internal StateInformation(ZwinderBuffer parent, int index)
			{
				_index = index;
				_parent = parent;
			}
		}

		/// <summary>
		/// Retrieve information about a state from 0..Count - 1.
		/// The information contained within is valid only until the collection is modified.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public StateInformation GetState(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new IndexOutOfRangeException();
			return new StateInformation(this, (index + _firstStateIndex) & STATEMASK);
		}

		/// <summary>
		/// Invalidate states from GetState(index) on to the end of the buffer, so that Count == index afterwards
		/// </summary>
		/// <param name="index"></param>
		public void InvalidateEnd(int index)
		{
			if ((uint)index > (uint)Count)
				throw new IndexOutOfRangeException();
			_nextStateIndex = (index + _firstStateIndex) & STATEMASK;
			//Util.DebugWriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			// version number
			writer.Write((byte)1);
			SaveStateBodyBinary(writer);
		}

		private void SaveStateBodyBinary(BinaryWriter writer)
		{
			writer.Write(Count);
			for (var i = _firstStateIndex; i != _nextStateIndex; i = (i + 1) & STATEMASK)
			{
				writer.Write(_states[i].Frame);
				writer.Write(_states[i].Size);
			}
			if (Count != 0)
			{
				var startByte = _states[_firstStateIndex].Start;
				var endByte = (_states[HeadStateIndex].Start + _states[HeadStateIndex].Size) & _sizeMask;
				var destStream = SpanStream.GetOrBuild(writer.BaseStream);
				if (startByte > endByte)
				{
					_backingStore.Position = startByte;
					WaterboxUtils.CopySome(_backingStore, writer.BaseStream, Size - startByte);
					startByte = 0;
				}
				{
					_backingStore.Position = startByte;
					WaterboxUtils.CopySome(_backingStore, writer.BaseStream, endByte - startByte);
				}
			}
		}

		private void LoadStateBodyBinary(BinaryReader reader)
		{
			_firstStateIndex = 0;
			_nextStateIndex = reader.ReadInt32();
			long nextByte = 0;
			for (var i = 0; i < _nextStateIndex; i++)
			{
				_states[i].Frame = reader.ReadInt32();
				_states[i].Size = reader.ReadInt32();
				_states[i].Start = nextByte;
				nextByte += _states[i].Size;
			}
			_backingStore.Position = 0;
			WaterboxUtils.CopySome(reader.BaseStream, _backingStore, nextByte);
		}

		public static ZwinderBuffer Create(BinaryReader reader, RewindConfig rewindConfig, bool hackyV0 = false)
		{
			ZwinderBuffer ret;

			// Initial format had no version number, but I think it's a safe bet no valid file has buffer size 2^56 or more so this should work.
			int version = hackyV0 ? 0 : reader.ReadByte();
			if (version == 0)
			{
				byte[] sizeArr = new byte[8];
				reader.Read(sizeArr, 1, 7);
				var size = BitConverter.ToInt64(sizeArr, 0);
				var sizeMask = reader.ReadInt64();
				var targetFrameLength = reader.ReadInt32();
				var useCompression = reader.ReadBoolean();
				ret = new ZwinderBuffer(new RewindConfig
				{
					BufferSize = (int)(size >> 20),
					UseFixedRewindInterval = false,
					TargetFrameLength = targetFrameLength,
					TargetRewindInterval = 5,
					UseCompression = useCompression
				});
				if (ret.Size != size || ret._sizeMask != sizeMask)
				{
					throw new InvalidOperationException("Bad format");
				}
			}
			else if (version == 1)
				ret = new ZwinderBuffer(rewindConfig);
			else
				throw new InvalidOperationException("Bad format");

			ret.LoadStateBodyBinary(reader);
			return ret;
		}

		private sealed class SaveStateStream : Stream, ISpanStream
		{
			/// <summary>
			///
			/// </summary>
			/// <param name="backingStore">The ringbuffer to write into</param>
			/// <param name="offset">Offset into the buffer to start writing (and treat as position 0 in the stream)</param>
			/// <param name="mask">Buffer size mask, used to wrap values in the ringbuffer correctly</param>
			/// <param name="notifySize">
			/// If the stream will exceed this size, notifySizeReached must be called before clobbering any data
			/// </param>
			/// <param name="notifySizeReached">
			/// The callback that will be called when notifySize is about to be exceeded.  Can either return a new larger notifySize,
			/// or abort processing with an IOException.  This must fail if size is going to exceed buffer.Length, as nothing else
			/// is preventing that case.
			/// </param>
			public SaveStateStream(Stream backingStore, long offset, long mask, long notifySize, Func<long> notifySizeReached)
			{
				_backingStore = backingStore;
				_backingStoreSS = SpanStream.GetOrBuild(backingStore);
				_offset = offset;
				_mask = mask;
				_notifySize = notifySize;
				_notifySizeReached = notifySizeReached;
			}

			private readonly Stream _backingStore;
			private readonly ISpanStream _backingStoreSS;
			private readonly long _offset;
			private readonly long _mask;
			private long _position;
			private long _notifySize;
			private readonly Func<long> _notifySizeReached;
			private long BufferLength => _mask + 1;

			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;
			public override long Length => _position;

			public override long Position { get => _position; set => throw new IOException(); }

			public override void Flush() {}

			public override int Read(byte[] buffer, int offset, int count) => throw new IOException();
			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public int Read(Span<byte> buffer) => throw new IOException();

			public override void Write(byte[] buffer, int offset, int count)
			{
				Write(new ReadOnlySpan<byte>(buffer, offset, count));
			}

			public void Write(ReadOnlySpan<byte> buffer)
			{
				long requestedSize = _position + buffer.Length;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				long n = buffer.Length;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					_backingStore.Position = start;
					if (end < start)
					{
						long m = BufferLength - start;
						_backingStoreSS.Write(buffer.Slice(0, (int)m));
						buffer = buffer.Slice((int)m);
						n -= m;
						_position += m;
						start = 0;
						_backingStore.Position = start;
					}
					if (n > 0)
					{
						_backingStoreSS.Write(buffer);
						_position += n;
					}
				}
			}

			public override void WriteByte(byte value)
			{
				long requestedSize = _position + 1;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				_backingStore.Position = (_position + _offset) & _mask;
				_backingStore.WriteByte(value);
				_position++;
			}
		}

		private sealed class LoadStateStream : Stream, ISpanStream
		{
			public LoadStateStream(Stream backingStore, long offset, long size, long mask)
			{
				_backingStore = backingStore;
				_backingStoreSS = SpanStream.GetOrBuild(backingStore);
				_offset = offset;
				_size = size;
				_mask = mask;
				_backingStore.Position = _offset;
			}

			private readonly Stream _backingStore;
			private readonly ISpanStream _backingStoreSS;
			private readonly long _offset;
			private readonly long _size;
			private long _position;
			private readonly long _mask;
			private long BufferLength => _mask + 1;

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => _size;
			public override long Position
			{
				get => _position;
				set => throw new IOException();
			}
			public override void Flush() {}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return Read(new Span<byte>(buffer, offset, count));
			}

			public int Read(Span<byte> buffer)
			{
				long n = Math.Min(_size - _position, buffer.Length);
				int ret = (int)n;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					if (end < start)
					{
						long m = BufferLength - start;
						if (_backingStoreSS.Read(buffer.Slice(0, (int)m)) != (int)m)
							throw new IOException("Unexpected end of underlying buffer");
						buffer = buffer.Slice((int)m);
						n -= m;
						_position += m;
						start = 0;
						_backingStore.Position = start;
					}
					if (n > 0)
					{
						if (_backingStoreSS.Read(buffer.Slice(0, (int)n)) != (int)n)
							throw new IOException("Unexpected end of underlying buffer");
						_position += n;
					}
				}
				return ret;
			}

			public override int ReadByte()
			{
				if (_position < _size)
				{
					var ret = _backingStore.ReadByte();
					if (ret == -1)
						throw new IOException("Unexpected end of underlying buffer");
					_position++;
					if (_position + _offset == BufferLength)
						_backingStore.Position = 0;
					return ret;
				}
				else
				{
					return -1;
				}
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public override void Write(byte[] buffer, int offset, int count) => throw new IOException();

			public void Write(ReadOnlySpan<byte> buffer) => throw new IOException();
		}
	}
}
