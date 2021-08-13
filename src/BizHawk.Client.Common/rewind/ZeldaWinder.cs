using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A rewinder that uses Zelda compression, built on top of a ring buffer
	/// </summary>
	public class ZeldaWinder : IRewinder
	{
		private const int IS_GAP = unchecked((int)0x80000000);

		private readonly ZwinderBuffer _buffer;
		private readonly IStatable _stateSource;

		private byte[] _master = new byte[0];
		private int _masterFrame = -1;
		private int _masterLength = 0;
		private byte[] _scratch = new byte[0];
		private int _count;

		private Task _activeTask = null;
		private bool _active;

		private void Sync()
		{
			_activeTask?.Wait();
			_activeTask = null;
		}
		private void Work(Action work)
		{
			_activeTask = Task.Run(work);
		}

		public ZeldaWinder(IStatable stateSource, IRewindSettings settings)
		{
			_buffer = new ZwinderBuffer(settings);
			_stateSource = stateSource;
			_active = true;
		}

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count { get { Sync(); return _count; } }

		public float FullnessRatio { get { Sync(); return _buffer.Used / (float)_buffer.Size; } }

		/// <summary>
		/// Total size of the _buffer
		/// </summary>
		/// <value></value>
		public long Size => _buffer.Size;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency { get { Sync(); return _buffer.RewindFrequency; } }

		public bool Active
		{
			get { Sync(); return _active; }
			private set { Sync(); _active = value; }
		}

		/// <summary>
		/// Whether the last frame we captured is stale and should be avoided when rewinding
		/// </summary>
		public bool HasStaleFrame { get; private set; }

		public void Suspend()
		{
			Active = false;
		}

		public void Resume()
		{
			Active = true;
		}

		public void Dispose()
		{
			Sync();
			_buffer.Dispose();
		}

		public void Clear()
		{
			Sync();
			_buffer.InvalidateEnd(0);
			_count = 0;
			_masterFrame = -1;
		}

		public unsafe void Capture(int frame)
		{
			Sync();
			if (!_active)
				return;
			HasStaleFrame = false;
			if (_masterFrame == -1)
			{
				var sss = new SaveStateStream(this);
				_stateSource.SaveStateBinary(new BinaryWriter(sss));
				(_master, _scratch) = (_scratch, _master);
				_masterLength = (int)sss.Position;
				_masterFrame = frame;
				_count++;
				HasStaleFrame = true;
				return;
			}
			if (!_buffer.WouldCapture(frame - _masterFrame))
				return;

			HasStaleFrame = true;
			{
				var sss = new SaveStateStream(this);
				_stateSource.SaveStateBinary(new BinaryWriter(sss));

				Work(() =>
				{
					_buffer.Capture(_masterFrame, underlyingStream_ =>
					{
						var zeldas = SpanStream.GetOrBuild(underlyingStream_);
						if (_master.Length < _scratch.Length)
						{
							var replacement = new byte[_scratch.Length];
							Array.Copy(_master, replacement, _master.Length);
							_master = replacement;
						}

						var lengthHolder = _masterLength;
						var lengthHolderSpan = new ReadOnlySpan<byte>(&lengthHolder, 4);

						zeldas.Write(lengthHolderSpan);

						fixed (byte* older_ = _master)
						fixed (byte* newer_ = _scratch)
						{
							int* older = (int*)older_;
							int* newer = (int*)newer_;
							int lastIndex = (Math.Min(_masterLength, (int)sss.Position) + 3) / 4;
							int lastOldIndex = (_masterLength + 3) / 4;
							int* olderEnd = older + lastIndex;

							int* from = older;
							int* to = older;

							while (older < olderEnd)
							{
								if (*older++ == *newer++)
								{
									if (to < from)
									{
										// Save on [to, from]
										lengthHolder = (int)(from - to);
										zeldas.Write(lengthHolderSpan);
										zeldas.Write(new ReadOnlySpan<byte>(to, lengthHolder * 4));
									}
									to = older;
								}
								else
								{
									if (from < to)
									{
										// encode gap [from, to]
										lengthHolder = (int)(to - from) | IS_GAP;
										zeldas.Write(lengthHolderSpan);
									}
									from = older;
								}
							}
							if (from < to)
							{
								// encode gap [from, to]
								lengthHolder = (int)(to - from) | IS_GAP;
								zeldas.Write(lengthHolderSpan);
							}
							if (lastOldIndex > lastIndex)
							{
								from += lastOldIndex - lastIndex;
							}
							if (to < from)
							{
								// Save on [to, from]
								lengthHolder = (int)(from - to);
								zeldas.Write(lengthHolderSpan);
								zeldas.Write(new ReadOnlySpan<byte>(to, lengthHolder * 4));
							}
						}

						(_master, _scratch) = (_scratch, _master);
						_masterLength = (int)sss.Position;
						_masterFrame = frame;
						_count++;
					},
					indexInvalidated: index =>
					{
						_count--;
					},
					force: true);
				});
			}
		}

		private unsafe void RefillMaster(ZwinderBuffer.StateInformation state)
		{
			var lengthHolder = 0;
			var lengthHolderSpan = new Span<byte>(&lengthHolder, 4);
			var zeldas = SpanStream.GetOrBuild(state.GetReadStream());
			zeldas.Read(lengthHolderSpan);
			_masterLength = lengthHolder;
			fixed (byte* buffer_ = _master)
			{
				int* buffer = (int*)buffer_;
				while (zeldas.Read(lengthHolderSpan) == 4)
				{
					if ((lengthHolder & IS_GAP) != 0)
					{
						buffer += lengthHolder & ~IS_GAP;
					}
					else
					{
						zeldas.Read(new Span<byte>(buffer, lengthHolder * 4));
						buffer += lengthHolder;
					}
				}
			}
			_masterFrame = state.Frame;
		}

		public bool Rewind(bool fastForward)
		{
			Sync();
			if (!_active || _count == 0)
				return false;

			if(_count == 1 && HasStaleFrame)
			{
				// load stale frame anyways. See Zwinder::Rewind(...) for details.
				_stateSource.LoadStateBinary(new BinaryReader(new MemoryStream(_master, 0, _masterLength, false)));
				_masterFrame = -1;
			}
			else
			{
				int rewindSteps = (fastForward ? 5 : 1);
				if (HasStaleFrame) ++rewindSteps;
				// last frame is in _master, so we only need to load (rewindSteps - 1) zeldas.
				var index = rewindSteps - 1 >= _count ? 0 : _count - (rewindSteps - 1);
				for(int i=_buffer.Count - 1; i >= index; --i)
				{
					RefillMaster(_buffer.GetState(i));
				}
				HasStaleFrame = true;
				_buffer.InvalidateEnd(index);
			}
			return true;
		}

		private class SaveStateStream : Stream, ISpanStream
		{
			public SaveStateStream(ZeldaWinder owner)
			{
				_owner = owner;
			}
			private ZeldaWinder _owner;
			private byte[] _dest
			{
				get => _owner._scratch;
				set => _owner._scratch = value;
			}
			private int _position;
			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;
			public override long Length => _position;
			public override long Position { get => _position; set => throw new IOException(); }
			public override void Flush() { }
			public override int Read(byte[] buffer, int offset, int count) => throw new IOException();
			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public override void Write(byte[] buffer, int offset, int count)
			{
				Write(new ReadOnlySpan<byte>(buffer, offset, count));
			}
			private void MaybeResize(int requestedSize)
			{
				if (requestedSize > _dest.Length)
				{
					var replacement = new byte[(Math.Max(_dest.Length * 2, requestedSize) + 3) & ~3];
					Array.Copy(_dest, replacement, _dest.Length);
					_dest = replacement;
				}
			}
			public void Write(ReadOnlySpan<byte> buffer)
			{
				var requestedSize = _position + buffer.Length;
				MaybeResize(requestedSize);
				buffer.CopyTo(new Span<byte>(_dest, _position, buffer.Length));
				_position = requestedSize;
			}
			public override void WriteByte(byte value)
			{
				var requestedSize = _position + 1;
				MaybeResize(requestedSize);
				_dest[_position] = value;
				_position = requestedSize;
			}
			public int Read(Span<byte> buffer) => throw new IOException();
		}
	}
}
