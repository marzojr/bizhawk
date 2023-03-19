

using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Waterbox.LibWaterboxCore;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract class WaterboxMemoryDomain : MemoryDomain
	{
		protected readonly IntPtr _data;
		protected readonly IMonitor _monitor;
		protected readonly long _addressMangler;

		public MemoryArea Definition { get; }

		public static WaterboxMemoryDomain Create(MemoryArea m, WaterboxHost monitor)
		{
			return m.Flags.HasFlag(MemoryDomainFlags.FunctionHook)
				? new WaterboxMemoryDomainFunc(m, monitor)
				: new WaterboxMemoryDomainPointer(m, monitor);
		}

		protected WaterboxMemoryDomain(MemoryArea m, IMonitor monitor)
		{
			Name = Mershul.PtrToStringUtf8(m.Name);
			EndianType = (m.Flags & MemoryDomainFlags.YugeEndian) != 0 ? Endian.Big : Endian.Little;
			_data = m.Data;
			Size = m.Size;
			Writable = (m.Flags & MemoryDomainFlags.Writable) != 0;
			if ((m.Flags & MemoryDomainFlags.WordSize1) != 0)
				WordSize = 1;
			else if ((m.Flags & MemoryDomainFlags.WordSize2) != 0)
				WordSize = 2;
			else if ((m.Flags & MemoryDomainFlags.WordSize4) != 0)
				WordSize = 4;
			else if ((m.Flags & MemoryDomainFlags.WordSize8) != 0)
				WordSize = 8;
			else
				throw new InvalidOperationException("Unknown word size for memory domain");
			_monitor = monitor;
			if ((m.Flags & MemoryDomainFlags.Swapped) != 0 && EndianType == Endian.Big)
			{
				_addressMangler = WordSize - 1;
			}
			else
			{
				_addressMangler = 0;
			}
			Definition = m;
		}

		public override void Enter()
			=> _monitor.Enter();

		public override void Exit()
			=> _monitor.Exit();
	}

	public unsafe class WaterboxMemoryDomainPointer : WaterboxMemoryDomain
	{
		internal WaterboxMemoryDomainPointer(MemoryArea m, IMonitor monitor)
			: base(m, monitor)
		{
			if (m.Flags.HasFlag(MemoryDomainFlags.FunctionHook))
				throw new InvalidOperationException();
		}

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					return ((byte*)_data)[addr ^ _addressMangler];
				}
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						((byte*)_data)[addr ^ _addressMangler] = val;
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public override void BulkPeekByte(ulong srcStartOffset, Span<byte> dstBuffer)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(srcStartOffset, dstBuffer);
				return;
			}
			if ((ulong) Size <= srcStartOffset) throw new ArgumentOutOfRangeException(paramName: nameof(srcStartOffset));
			if ((ulong) Size < srcStartOffset + (ulong) dstBuffer.Length) throw new ArgumentException(paramName: nameof(dstBuffer), message: ERR_MSG_TOO_MANY_BYTES_REQ);
			try
			{
				_monitor.Enter();
				new Span<byte>((void*) Z.US((ulong) _data + srcStartOffset), dstBuffer.Length).CopyTo(dstBuffer);
			}
			finally
			{
				_monitor.Exit();
			}
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(addresses, values);
				return;
			}

			var start = (ulong)addresses.Start;
			var count = addresses.Count();

			if (start < (ulong)Size && (start + count) <= (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					Marshal.Copy(Z.US((ulong)_data + start), values, 0, (int)count);
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(addresses));
			}
		}
	}

	/// <summary>
	/// For private use only!  Don't touch
	/// </summary>
	public abstract class MemoryDomainAccessStub
	{
		[BizImport(CallingConvention.Cdecl)]
		public abstract void Access(IntPtr buffer, long address, long count, bool write);

		private class StubResolver : IImportResolver
		{
			private readonly IntPtr _p;
			public StubResolver(IntPtr p)
			{
				_p = p;
			}
			public IntPtr GetProcAddrOrThrow(string entryPoint) => _p;
			public IntPtr GetProcAddrOrZero(string entryPoint) => _p;
		}

		public static MemoryDomainAccessStub Create(IntPtr p, WaterboxHost host)
		{
			return BizInvoker.GetInvoker<MemoryDomainAccessStub>(
				new StubResolver(p), host, CallingConventionAdapters.MakeWaterboxDepartureOnly(host));
		}
	}

	public unsafe class WaterboxMemoryDomainFunc : WaterboxMemoryDomain
	{
		private readonly MemoryDomainAccessStub _access;

		internal WaterboxMemoryDomainFunc(MemoryArea m, WaterboxHost monitor)
			: base(m, monitor)
		{
			if (!m.Flags.HasFlag(MemoryDomainFlags.FunctionHook))
				throw new InvalidOperationException();
			_access = MemoryDomainAccessStub.Create(m.Data, monitor);
		}

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				byte ret = 0;
				_access.Access((IntPtr)(byte*)&ret, addr, 1, false);
				return ret;
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					_access.Access((IntPtr)(byte*)&val, addr, 1, true);
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public override void BulkPeekByte(ulong srcStartOffset, Span<byte> dstBuffer)
		{
			if (_addressMangler is not 0)
			{
				base.BulkPeekByte(srcStartOffset, dstBuffer);
				return;
			}
			if ((ulong) Size <= srcStartOffset) throw new ArgumentOutOfRangeException(paramName: nameof(srcStartOffset));
			if ((ulong) Size < srcStartOffset + (ulong) dstBuffer.Length) throw new ArgumentException(paramName: nameof(dstBuffer), message: ERR_MSG_TOO_MANY_BYTES_REQ);
			fixed (byte* p = dstBuffer)
			{
				_access.Access((IntPtr) p, /*offset*/address: (long) srcStartOffset, count: dstBuffer.Length, write: false);
			}
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(addresses, values);
				return;
			}

			var start = (ulong)addresses.Start;
			var count = addresses.Count();

			if (start < (ulong)Size && (start + count) <= (ulong)Size)
			{
				fixed(byte* p = values)
					_access.Access((IntPtr)p, (long)start, (long)count, false);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(addresses));
			}
		}
	}
}
