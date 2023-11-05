using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			LibGPGX.RegisterInfo[] regs = new LibGPGX.RegisterInfo[Core.gpgx_getmaxnumregs()];

			int n = Core.gpgx_getregs(regs);
			if (n > regs.Length)
				throw new InvalidOperationException("A buffer overrun has occured!");
			var ret = new Dictionary<string, RegisterValue>();
			using (_elf.EnterExit())
			{
				for (int i = 0; i < n; i++)
				{
					// el hacko
					string name = Marshal.PtrToStringAnsi(regs[i].Name);
					byte size = 32;
					if (name.Contains("68K SR") || name.StartsWithOrdinal("Z80"))
						size = 16;

					ret[name] = new RegisterValue((ulong)regs[i].Value, size);
				}
			}

			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		private readonly MemoryCallbackSystem _memoryCallbacks = new MemoryCallbackSystem(new[] { "M68K BUS" });

		private LibGPGX.mem_cb ExecCallback;
		private LibGPGX.mem_cb ReadCallback;
		private LibGPGX.mem_cb WriteCallback;
		private readonly LibGPGX.CDCallback CDCallback;

		private void InitMemCallbacks()
		{
			ExecCallback = new LibGPGX.mem_cb((a, val) =>
			{
				if (MemoryCallbacks.HasExecutes)
				{
					uint flags = (uint)MemoryCallbackFlags.AccessExecute;
					MemoryCallbacks.CallMemoryCallbacks(a, val, flags, "M68K BUS");
				}
			});
			ReadCallback = new LibGPGX.mem_cb((a, val) =>
			{
				if (MemoryCallbacks.HasReads)
				{
					uint flags = (uint)MemoryCallbackFlags.AccessRead;
					MemoryCallbacks.CallMemoryCallbacks(a, val, flags, "M68K BUS");
				}
			});
			WriteCallback = new LibGPGX.mem_cb((a, val) =>
			{
				if (MemoryCallbacks.HasWrites)
				{
					uint flags = (uint)MemoryCallbackFlags.AccessWrite;
					MemoryCallbacks.CallMemoryCallbacks(a, val, flags, "M68K BUS");
				}
			});
			_memoryCallbacks.ActiveChanged += RefreshMemCallbacks;
		}

		private void RefreshMemCallbacks()
		{
			Core.gpgx_set_mem_callback(
				MemoryCallbacks.HasReads ? ReadCallback : null,
				MemoryCallbacks.HasWrites ? WriteCallback : null,
				MemoryCallbacks.HasExecutes ? ExecCallback : null);
		}

		private void KillMemCallbacks()
		{
			Core.gpgx_set_mem_callback(null, null, null);
		}
	}
}
