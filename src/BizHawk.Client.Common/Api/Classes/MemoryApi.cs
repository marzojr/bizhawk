using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class MemoryApi : IMemoryApi
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		public IMemoryDomains MemoryDomainCore { get; set; }

		private readonly Action<string> LogCallback;

		private bool _isBigEndian;

		private MemoryDomain _currentMemoryDomain;
		private MemoryDomain Domain
		{
			get
			{
				MemoryDomain LazyInit()
				{
					if (MemoryDomainCore == null)
					{
						var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
						LogCallback(error);
						throw new NotImplementedException(error);
					}
					return MemoryDomainCore.HasSystemBus ? MemoryDomainCore.SystemBus : MemoryDomainCore.MainMemory;
				}
				_currentMemoryDomain ??= LazyInit();
				return _currentMemoryDomain;
			}
			set => _currentMemoryDomain = value;
		}

		public IMemoryDomains DomainList
		{
			get
			{
				if (MemoryDomainCore == null)
				{
					var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
					LogCallback(error);
					throw new NotImplementedException(error);
				}
				return MemoryDomainCore;
			}
		}

		public string MainMemoryName
		{
			get
			{
				if (MemoryDomainCore == null)
				{
					var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
					LogCallback(error);
					throw new NotImplementedException(error);
				}
				return MemoryDomainCore.MainMemory.Name;
			}
		}

		public MemoryApi(Action<string> logCallback) => LogCallback = logCallback;

		private MemoryDomain NamedDomainOrCurrent(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				try
				{
					var found = DomainList[name];
					if (found != null) return found;
				}
				catch
				{
					// ignored
				}
				LogCallback($"Unable to find domain: {name}, falling back to current");
			}
			return Domain;
		}

		private static int U2S(uint u, int size)
		{
			var sh = 8 * (4 - size);
			return ((int) u << sh) >> sh;
		}

		private uint ReadUnsignedLittle(long addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; i++) v |= ReadUnsigned(addr + i, 1, domain) << (8 * i);
			return v;
		}

		private uint ReadUnsignedBig(long addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; i++) v |= ReadUnsigned(addr + i, 1, domain) << (8 * (size - 1 - i));
			return v;
		}

		private void WriteUnsignedLittle(long addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; i++) WriteUnsigned(addr + i, (v >> (8 * i)) & 0xFF, 1, domain);
		}

		private void WriteUnsignedBig(long addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; i++) WriteUnsigned(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF, 1, domain);
		}

		private int ReadSigned(long addr, int size, string domain = null) => U2S(ReadUnsigned(addr, size, domain), size);

		private uint ReadUnsigned(long addr, int size, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (addr < 0 || addr + size > d.Size)
			{
				LogCallback($"Warning: attempted read of {addr} outside the memory size of {d.Size}");
				return 0;
			}

			return size switch
			{
				1 => d.PeekByte(addr),
				2 => d.PeekUshort(addr, _isBigEndian),
				3 => _isBigEndian ? ReadUnsignedBig(addr, 3, domain) : ReadUnsignedLittle(addr, 3, domain),
				4 => d.PeekUint(addr, _isBigEndian),
				_ => 0
			};
		}

		private void WriteSigned(long addr, int value, int size, string domain = null) => WriteUnsigned(addr, (uint) value, size, domain);

		private void WriteUnsigned(long addr, uint value, int size, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!d.Writable)
			{
				LogCallback($"Error: the domain {d.Name} is not writable");
				return;
			}
			if (addr < 0 || addr + size > d.Size)
			{
				LogCallback($"Warning: attempted write to {addr} outside the memory size of {d.Size}");
				return;
			}

			switch (size)
			{
				case 1:
					{
						d.PokeByte(addr, (byte)value);
						break;
					}
				case 2:
					{
						d.PokeUshort(addr, (ushort)value, _isBigEndian);
						break;
					}
				case 3:
					{
						if (_isBigEndian)
							WriteUnsignedBig(addr, value, 3, domain);
						else
							WriteUnsignedLittle(addr, value, 3, domain);
						break;
					}
				case 4:
					{
						d.PokeUint(addr, value, _isBigEndian);
						break;
					}
			}
		}

		public void SetBigEndian(bool enabled = true) => _isBigEndian = enabled;

		public IReadOnlyCollection<string> GetMemoryDomainList()
			=> DomainList.Select(static domain => domain.Name).ToList();

		public uint GetMemoryDomainSize(string name = null) => (uint) NamedDomainOrCurrent(name).Size;

		public string GetCurrentMemoryDomain() => Domain.Name;

		public uint GetCurrentMemoryDomainSize() => (uint) Domain.Size;

		public bool UseMemoryDomain(string domain)
		{
			try
			{
				var found = DomainList[domain];
				if (found != null)
				{
					Domain = found;
					return true;
				}
			}
			catch
			{
				// ignored
			}
			LogCallback($"Unable to find domain: {domain}");
			return false;
		}

		/// <exception cref="ArgumentOutOfRangeException">range defined by <paramref name="addr"/> and <paramref name="count"/> extends beyond the bound of <paramref name="domain"/> (or <see cref="Domain"/> if null)</exception>
		public string HashRegion(long addr, int count, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!0L.RangeToExclusive(d.Size).Contains(addr))
			{
				var error = $"Address {addr} is outside the bounds of domain {d.Name}";
				LogCallback(error);
				throw new ArgumentOutOfRangeException(error);
			}
			if (addr + count > d.Size)
			{
				var error = $"Address {addr} + count {count} is outside the bounds of domain {d.Name}";
				LogCallback(error);
				throw new ArgumentOutOfRangeException(error);
			}
			var data = new byte[count];
			using (d.EnterExit())
			{
				for (var i = 0; i < count; i++) data[i] = d.PeekByte(addr + i);
			}
			return SHA256Checksum.ComputeDigestHex(data);
		}

		public uint ReadByte(long addr, string domain = null) => ReadUnsigned(addr, 1, domain);

		public void WriteByte(long addr, uint value, string domain = null) => WriteUnsigned(addr, value, 1, domain);

		public IReadOnlyList<byte> ReadByteRange(long addr, int length, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (addr < 0) LogCallback($"Warning: Attempted reads on addresses {addr}..-1 outside range of domain {d.Name} in {nameof(ReadByteRange)}()");
			var lastReqAddr = addr + length - 1;
			var indexAfterLast = Math.Min(Math.Max(-1L, lastReqAddr), d.Size - 1L) + 1L;
			var iSrc = Math.Min(Math.Max(0L, addr), d.Size);
			var iDst = iSrc - addr;
			var bytes = new byte[length];
			if (iSrc < indexAfterLast)
			{
				d.BulkPeekByte((ulong) iSrc, bytes.AsSpan().Slice(start: (int) iDst, length: (int) iSrc.RangeToExclusive(indexAfterLast).Count()));
			}
			if (lastReqAddr >= d.Size) LogCallback($"Warning: Attempted reads on addresses {d.Size}..{lastReqAddr} outside range of domain {d.Name} in {nameof(ReadByteRange)}()");
			return bytes;
		}

		public bool ReadByteRange(ulong srcStartOffset, Span<byte> dstBuffer, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (srcStartOffset >= (ulong) d.Size)
			{
				LogCallback($"Warning: attempted read of {srcStartOffset} outside the memory size of {d.Size}");
				return false;
			}
			try
			{
				d.BulkPeekByte(srcStartOffset, dstBuffer);
				return true;
			}
			catch (Exception e)
			{
				LogCallback($"bulk-peek threw {e.GetType().Name}: {e.Message}");
				return false;
			}
		}

		public void WriteByteRange(long addr, IReadOnlyList<byte> memoryblock, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!d.Writable)
			{
				LogCallback($"Error: the domain {d.Name} is not writable");
				return;
			}
			if (addr < 0) LogCallback($"Warning: Attempted writes on addresses {addr}..-1 outside range of domain {d.Name} in {nameof(WriteByteRange)}()");
			var lastReqAddr = addr + memoryblock.Count - 1;
			var indexAfterLast = Math.Min(Math.Max(-1L, lastReqAddr), d.Size - 1L) + 1L;
			var iDst = Math.Min(Math.Max(0L, addr), d.Size);
			var iSrc = checked((int) (iDst - addr));
			using (d.EnterExit())
			{
				while (iDst < indexAfterLast) d.PokeByte(iDst++, memoryblock[iSrc++]);
			}
			if (lastReqAddr >= d.Size) LogCallback($"Warning: Attempted writes on addresses {d.Size}..{lastReqAddr} outside range of domain {d.Name} in {nameof(WriteByteRange)}()");
		}

		public float ReadFloat(long addr, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (addr >= d.Size)
			{
				LogCallback($"Warning: Attempted read {addr} outside memory size of {d.Size}");
				return default;
			}
			return BitConverter.ToSingle(BitConverter.GetBytes(d.PeekUint(addr, _isBigEndian)), 0);
		}

		public void WriteFloat(long addr, double value, string domain = null)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!d.Writable)
			{
				LogCallback($"Error: the domain {Domain.Name} is not writable");
				return;
			}
			if (addr >= d.Size)
			{
				LogCallback($"Warning: Attempted write {addr} outside memory size of {d.Size}");
				return;
			}
			d.PokeUint(addr, BitConverter.ToUInt32(BitConverter.GetBytes((float) value), 0), _isBigEndian);
		}

		public int ReadS8(long addr, string domain = null) => (sbyte) ReadUnsigned(addr, 1, domain);

		public uint ReadU8(long addr, string domain = null) => (byte) ReadUnsigned(addr, 1, domain);

		public void WriteS8(long addr, int value, string domain = null) => WriteSigned(addr, value, 1, domain);

		public void WriteU8(long addr, uint value, string domain = null) => WriteUnsigned(addr, value, 1, domain);

		public int ReadS16(long addr, string domain = null) => (short) ReadSigned(addr, 2, domain);

		public uint ReadU16(long addr, string domain = null) => (ushort) ReadUnsigned(addr, 2, domain);

		public void WriteS16(long addr, int value, string domain = null) => WriteSigned(addr, value, 2, domain);

		public void WriteU16(long addr, uint value, string domain = null) => WriteUnsigned(addr, value, 2, domain);

		public int ReadS24(long addr, string domain = null) => ReadSigned(addr, 3, domain);

		public uint ReadU24(long addr, string domain = null) => ReadUnsigned(addr, 3, domain);

		public void WriteS24(long addr, int value, string domain = null) => WriteSigned(addr, value, 3, domain);

		public void WriteU24(long addr, uint value, string domain = null) => WriteUnsigned(addr, value, 3, domain);

		public int ReadS32(long addr, string domain = null) => ReadSigned(addr, 4, domain);

		public uint ReadU32(long addr, string domain = null) => ReadUnsigned(addr, 4, domain);

		public void WriteS32(long addr, int value, string domain = null) => WriteSigned(addr, value, 4, domain);

		// goes here
		public void WriteU32(long addr, uint value, string domain = null) => WriteUnsigned(addr, value, 4, domain);
	}
}
