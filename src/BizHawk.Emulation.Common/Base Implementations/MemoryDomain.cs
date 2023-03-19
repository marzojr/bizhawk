#nullable disable

using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A memory region and the functionality to read/write from it
	/// as required by the IMemoryDomains service.
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public abstract class MemoryDomain : IMonitor
	{
		private const string ERR_MSG_DEST_WRONG_LEN = "Invalid length of values array";

		private const string ERR_MSG_NOT_ALIGNED = "The API contract doesn't define what to do for unaligned reads and writes!";

		protected const string ERR_MSG_TOO_MANY_BYTES_REQ = "too many bytes requested, would read beyond bounds of domain";

		public enum Endian
		{
			Big, Little, Unknown
		}

		public string Name { get; protected set; }

		public long Size { get; protected set; }

		public int WordSize { get; protected set; }

		public Endian EndianType { get; protected set; }

		public bool Writable { get; protected set; }

		public abstract byte PeekByte(long addr);

		public abstract void PokeByte(long addr, byte val);

		public override string ToString() => Name;

		public virtual ushort PeekUshort(long addr, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					return (ushort)((PeekByte(addr) << 8) | PeekByte(addr + 1));
				case Endian.Little:
					return (ushort)(PeekByte(addr) | (PeekByte(addr + 1) << 8));
			}
		}

		public virtual uint PeekUint(long addr, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					return (uint)((PeekByte(addr) << 24)
					| (PeekByte(addr + 1) << 16)
					| (PeekByte(addr + 2) << 8)
					| (PeekByte(addr + 3) << 0));
				case Endian.Little:
					return (uint)((PeekByte(addr) << 0)
					| (PeekByte(addr + 1) << 8)
					| (PeekByte(addr + 2) << 16)
					| (PeekByte(addr + 3) << 24));
			}
		}

		public virtual void PokeUshort(long addr, ushort val, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					PokeByte(addr + 0, (byte)(val >> 8));
					PokeByte(addr + 1, (byte)val);
					break;
				case Endian.Little:
					PokeByte(addr + 0, (byte)val);
					PokeByte(addr + 1, (byte)(val >> 8));
					break;
			}
		}

		public virtual void PokeUint(long addr, uint val, bool bigEndian)
		{
			Endian endian = bigEndian ? Endian.Big : Endian.Little;
			switch (endian)
			{
				default:
				case Endian.Big:
					PokeByte(addr + 0, (byte)(val >> 24));
					PokeByte(addr + 1, (byte)(val >> 16));
					PokeByte(addr + 2, (byte)(val >> 8));
					PokeByte(addr + 3, (byte)val);
					break;
				case Endian.Little:
					PokeByte(addr + 0, (byte)val);
					PokeByte(addr + 1, (byte)(val >> 8));
					PokeByte(addr + 2, (byte)(val >> 16));
					PokeByte(addr + 3, (byte)(val >> 24));
					break;
			}
		}

		public virtual byte[] BulkPeekByte(Range<long> addresses)
		{
			var buf = new byte[addresses.Count()];
			BulkPeekByte((ulong) addresses.Start, buf.AsSpan());
			return buf;
		}

		public virtual void BulkPeekByte(ulong srcStartOffset, Span<byte> dstBuffer)
		{
			using var handle = this.EnterExit();
			var iSrc = (long) srcStartOffset;
			var endExcl = (long) srcStartOffset + dstBuffer.Length;
			var iDst = 0;
			while (iSrc != endExcl) dstBuffer[iDst++] = PeekByte(iSrc++);
		}

		public virtual void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			if ((ulong) values.LongLength != addresses.Count())
			{
				throw new InvalidOperationException(ERR_MSG_DEST_WRONG_LEN);
			}

			using (this.EnterExit())
			{
				for (var i = addresses.Start; i <= addresses.EndInclusive; i++)
				{
					values[i - addresses.Start] = PeekByte(i);
				}
			}
		}

		public virtual void BulkPeekUshort(Range<long> addresses,  bool bigEndian, ushort[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			var start = addresses.Start;
			var end  = addresses.EndInclusive + 1;

			if (start % 2 is not 0 || end % 2 is not 0) throw new InvalidOperationException(ERR_MSG_NOT_ALIGNED);
			
			if (checked((ulong) values.LongLength * 2UL) != addresses.Count())
			{
				// a longer array could be valid, but nothing needs that so don't support it for now
				throw new InvalidOperationException(ERR_MSG_DEST_WRONG_LEN);
			}

			using (this.EnterExit())
			{
				for (var i = 0; i < values.Length; i++, start += 2)
					values[i] = PeekUshort(start, bigEndian);
			}
		}

		public virtual void BulkPeekUint(Range<long> addresses, bool bigEndian, uint[] values)
		{
			if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
			if (values is null) throw new ArgumentNullException(paramName: nameof(values));

			var start = addresses.Start;
			var end  = addresses.EndInclusive + 1;

			if (start % 4 is not 0 || end % 4 is not 0) throw new InvalidOperationException(ERR_MSG_NOT_ALIGNED);
			
			if (checked((ulong) values.LongLength * 4UL) != addresses.Count())
			{
				// a longer array could be valid, but nothing needs that so don't support it for now
				throw new InvalidOperationException(ERR_MSG_DEST_WRONG_LEN);
			}

			using (this.EnterExit())
			{
				for (var i = 0; i < values.Length; i++, start += 4)
					values[i] = PeekUint(start, bigEndian);
			}
		}

		public virtual void SendCheatToCore(int addr, byte value, int compare, int compare_type) { }

		/// <summary>
		/// only use this if you are expecting to do a lot of peeks/pokes
		/// no-op if the domain has no monitor
		/// </summary>
		public virtual void Enter() { }

		/// <summary>
		/// only use this if you are expecting to do a lot of peeks/pokes
		/// no-op if the domain has no monitor
		/// </summary>
		public virtual void Exit() { }
	}
}
