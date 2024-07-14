using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IDisassemblable
{
	public string Cpu { get; set; } = "R4300";

	public string PCRegisterName => "PC";

	public IEnumerable<string> AvailableCpus { get; } = [ "R4300" ];

	public string Disassemble(MemoryDomain m, uint addr, out int length)
	{
		uint instruction = m.PeekUint(addr, m.EndianType is MemoryDomain.Endian.Big);

		byte[] opBuffer = ArrayPool<byte>.Shared.Rent(128);
		byte[] argsBuffer = ArrayPool<byte>.Shared.Rent(128);
		Mupen64Api.DebugDecodeOp(instruction, opBuffer, argsBuffer, (int)addr);
		string op = Encoding.UTF8.GetString(opBuffer, 0, Array.IndexOf<byte>(opBuffer, 0));
		string args = Encoding.UTF8.GetString(argsBuffer, 0, Array.IndexOf<byte>(argsBuffer, 0));

		ArrayPool<byte>.Shared.Return(opBuffer);
		ArrayPool<byte>.Shared.Return(argsBuffer);

		string ret = $"{op}: {args}";
		length = ret.Length;

		return ret;
	}
}
