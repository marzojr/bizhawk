using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IMemoryApi : IExternalApi
	{
		string MainMemoryName { get; }

		void SetBigEndian(bool enabled = true);

		IReadOnlyCollection<string> GetMemoryDomainList();
		uint GetMemoryDomainSize(string name = "");
		string GetCurrentMemoryDomain();
		uint GetCurrentMemoryDomainSize();
		bool UseMemoryDomain(string domain);
		string HashRegion(long addr, int count, string domain = null);

		uint ReadByte(long addr, string domain = null);
		IReadOnlyList<byte> ReadByteRange(long addr, int length, string domain = null);

		/// <summary>copies a region of memory into <paramref name="dstBuffer"/>, starting at <paramref name="srcStartOffset"/> and taking as many bytes as will fit in the buffer</summary>
		/// <returns>
		/// <see langword="false"/> iff the internal API call threw an exception (for example, if you request bytes beyond the end of the domain).<br/>
		/// If this happens, the state of <paramref name="dstBuffer"/> is undefined. Anywhere from <c>0</c> bytes to all of them could have been copied.
		/// The exception is when <paramref name="srcStartOffset"/> falls outside the domain, as that is checked first and results in a no-op.
		/// </returns>
		bool ReadByteRange(ulong srcStartOffset, Span<byte> dstBuffer, string domain = null);

		float ReadFloat(long addr, string domain = null);

		int ReadS8(long addr, string domain = null);
		int ReadS16(long addr, string domain = null);
		int ReadS24(long addr, string domain = null);
		int ReadS32(long addr, string domain = null);

		uint ReadU8(long addr, string domain = null);
		uint ReadU16(long addr, string domain = null);
		uint ReadU24(long addr, string domain = null);
		uint ReadU32(long addr, string domain = null);

		void WriteByte(long addr, uint value, string domain = null);
		void WriteByteRange(long addr, IReadOnlyList<byte> memoryblock, string domain = null);
		void WriteFloat(long addr, double value, string domain = null);

		void WriteS8(long addr, int value, string domain = null);
		void WriteS16(long addr, int value, string domain = null);
		void WriteS24(long addr, int value, string domain = null);
		void WriteS32(long addr, int value, string domain = null);

		void WriteU8(long addr, uint value, string domain = null);
		void WriteU16(long addr, uint value, string domain = null);
		void WriteU24(long addr, uint value, string domain = null);
		void WriteU32(long addr, uint value, string domain = null);
	}
}
