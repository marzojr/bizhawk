#nullable disable

using System;
using System.IO;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomainStream : Stream
	{
		public MemoryDomainStream(MemoryDomain d)
		{
			_d = d;
		}
		private readonly MemoryDomain _d;
		private long _position;

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => _d.Writable;

		public override long Length => _d.Size;

		public override long Position
		{
			get => _position;
			set
			{
				if (value < 0 || value > _d.Size)
					throw new IOException("Position out of range");
				_position = value;
			}
		}

		public override void Flush()
		{
		}

		public override int ReadByte()
		{
			if (Position >= Length)
				return -1;
			return _d.PeekByte(Position++);
		}

		public override void WriteByte(byte value)
		{
			if (Position >= Length)
				throw new IOException("Can't resize stream");
			_d.PokeByte(Position++, value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (offset < 0 || offset + count > buffer.Length)
				throw new ArgumentException("start or end not within bounds of buffer", nameof(offset));
			count = (int)Math.Min(count, Length - Position);
			if (count == 0)
				return 0;
			_d.BulkPeekByte((ulong) Position, buffer.AsSpan().Slice(start: offset, length: count));
			Position += count;
			return count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (offset < 0 || offset + count > buffer.Length)
				throw new ArgumentException("start or end not within bounds of buffer", nameof(offset));
			for (var i = offset; i < offset + count; i++)
				_d.PokeByte(Position++, buffer[i]);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					Position = offset;
					break;
				case SeekOrigin.Current:
					Position += offset;
					break;
				case SeekOrigin.End:
					Position = Length + offset;
					break;
				default:
					throw new ArgumentOutOfRangeException(paramName: nameof(origin));
			}
			return Position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException("Stream cannot be resized");
		}
	}
}
