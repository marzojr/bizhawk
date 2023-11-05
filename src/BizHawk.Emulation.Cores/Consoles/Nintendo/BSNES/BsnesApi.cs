using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using System.Linq;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public abstract class BsnesCoreImpl
	{
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_audio_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_video_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_layer_enables(ref BsnesApi.LayerEnables layerEnables);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_trace_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_hooks_enabled(bool readHookEnabled, bool writeHookEnabled, bool executeHookEnabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_ppu_sprite_limit_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_overscan_enabled(bool enabled);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_cursor_enabled(bool enabled);

		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr snes_get_audiobuffer_and_size(out int size);
		[BizImport(CallingConvention.Cdecl)]
		public abstract BsnesApi.SNES_REGION snes_get_region();
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr snes_get_board();
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr snes_get_memory_region(int id, out int size, out int wordSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract int snes_peek_logical_register(BsnesApi.SNES_REGISTER register);
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte snes_bus_read(uint address);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_bus_write(uint address, byte value);
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr snes_get_sgb_memory_region(int id, out int size);
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte snes_sgb_bus_read(ushort address);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_sgb_bus_write(ushort address, byte value);
		[BizImport(CallingConvention.Cdecl)]
		public abstract int snes_sgb_battery_size();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_sgb_save_battery(byte[] buffer, int size);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_sgb_load_battery(byte[] buffer, int size);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_callbacks(IntPtr[] snesCallbacks);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_init(ref BsnesApi.SnesInitData initData);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_power();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_term();
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_reset();
		[BizImport(CallingConvention.Cdecl)]
		public abstract bool snes_run(bool breakOnLatch);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_serialize(byte[] serializedData, int serializedSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_unserialize(byte[] serializedData, int serializedSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract int snes_serialized_size();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_load_cartridge_normal(byte[] romData, int romSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_load_cartridge_super_gameboy(byte[] romData, byte[] sgbRomData, int romSize, int sgbRomSize);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_load_cartridge_bsmemory(byte[] romData, byte[] bsmemoryRomData, int romSize, int bsmemoryRomSize);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_get_cpu_registers(ref BsnesApi.CpuRegisters registers);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void snes_set_cpu_register(string register, uint value);
		[BizImport(CallingConvention.Cdecl)]
		public abstract bool snes_cpu_step();

		[BizImport(CallingConvention.Cdecl)]
		public abstract long snes_get_executed_cycles();

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool snes_msu_sync();
	}

	public partial class BsnesApi : IDisposable, IMonitor, IStatable
	{
		internal WaterboxHost exe;
		internal BsnesCoreImpl core;
		private readonly ICallingConventionAdapter _adapter;
		private bool _disposed;

		public void Enter()
		{
			exe.Enter();
		}

		public void Exit()
		{
			exe.Exit();
		}

		private readonly List<string> _readonlyFiles = new();

		public void AddReadonlyFile(byte[] data, string name)
		{
			// current logic potentially requests the same name twice; once for program and once for data
			// because this gets mapped to the same file, we only add it once
			if (!_readonlyFiles.Contains(name))
			{
				exe.AddReadonlyFile(data, name);
				_readonlyFiles.Add(name);
			}
		}

		public void AddReadonlyFile(string path, string name)
		{
			if (!_readonlyFiles.Contains(name))
			{
				try
				{
					exe.AddReadonlyFile(File.ReadAllBytes(path), name);
					_readonlyFiles.Add(name);
				}
				catch
				{
					// ignored
				}
			}
		}

		public void SetCallbacks(SnesCallbacks callbacks)
		{
			var functionPointerArray = callbacks
				.AllDelegatesInMemoryOrder()
				.Select(f => _adapter.GetFunctionPointerForDelegate(f))
				.ToArray();
			core.snes_set_callbacks(functionPointerArray);
		}

		public BsnesApi(string dllPath, CoreComm comm, IEnumerable<Delegate> allCallbacks)
		{
			exe = new WaterboxHost(new WaterboxOptions
			{
				Filename = "bsnes.wbx",
				Path = dllPath,
				SbrkHeapSizeKB = 12 * 1024,
				InvisibleHeapSizeKB = 140 * 1024, // TODO: Roms get saved here and in mmap, consider consolidating?
				MmapHeapSizeKB = 33 * 1024, // TODO: check whether this needs to be larger; it depends on the rom size
				PlainHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 0,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			using (exe.EnterExit())
			{
				// Marshal checks that function pointers passed to GetDelegateForFunctionPointer are
				// _currently_ valid when created, even though they don't need to be valid until
				// the delegate is later invoked.  so GetInvoker needs to be acquired within a lock.
				_adapter = CallingConventionAdapters.MakeWaterbox(allCallbacks, exe);
				this.core = BizInvoker.GetInvoker<BsnesCoreImpl>(exe, exe, _adapter);
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				exe.Dispose();
				exe = null;
				core = null;
			}
		}

		public delegate void snes_video_frame_t(IntPtr data, int width, int height, int pitch);
		public delegate short snes_input_poll_t(int port, int index, int id);
		public delegate void snes_controller_latch_t();
		public delegate void snes_no_lag_t(bool sgb_poll);
		public delegate string snes_path_request_t(int slot, string hint, bool required);
		public delegate void snes_trace_t(string disassembly, string register_info);
		public delegate void snes_read_hook_t(uint address, byte value);
		public delegate void snes_write_hook_t(uint address, byte value);
		public delegate void snes_exec_hook_t(uint address);
		public delegate long snes_time_t();
		public delegate bool snes_msu_open_t(ushort track_id);
		public delegate void snes_msu_seek_t(long offset, bool relative);
		public delegate byte snes_msu_read_t();
		public delegate bool snes_msu_end_t();

		[StructLayout(LayoutKind.Sequential)]
		public struct CpuRegisters
		{
			public uint pc;
			public ushort a, x, y, z, s, d;
			public byte b, p, mdr;
			public bool e;
			public ushort v, h;
		}

		[Flags]
		public enum RegisterFlags : byte
		{
			C = 1,
			Z = 2,
			I = 4,
			D = 8,
			X = 16,
			M = 32,
			V = 64,
			N = 128,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LayerEnables
		{
			public bool BG1_Prio0, BG1_Prio1;
			public bool BG2_Prio0, BG2_Prio1;
			public bool BG3_Prio0, BG3_Prio1;
			public bool BG4_Prio0, BG4_Prio1;
			public bool Obj_Prio0, Obj_Prio1, Obj_Prio2, Obj_Prio3;
		}

		[StructLayout(LayoutKind.Sequential)]
		public sealed class SnesCallbacks
		{
			public snes_video_frame_t videoFrameCb;
			public snes_input_poll_t inputPollCb;
			public snes_controller_latch_t controllerLatchCb;
			public snes_no_lag_t noLagCb;
			public snes_path_request_t pathRequestCb;
			public snes_trace_t traceCb;
			public snes_read_hook_t readHookCb;
			public snes_write_hook_t writeHookCb;
			public snes_exec_hook_t execHookCb;
			public snes_time_t timeCb;
			public snes_msu_open_t msuOpenCb;
			public snes_msu_seek_t msuSeekCb;
			public snes_msu_read_t msuReadCb;
			public snes_msu_end_t msuEndCb;

			private static List<FieldInfo> FieldsInOrder;

			public IEnumerable<Delegate> AllDelegatesInMemoryOrder()
			{
				FieldsInOrder ??= typeof(SnesCallbacks)
					.GetFields()
					.OrderBy(BizInvokerUtilities.ComputeFieldOffset)
					.ToList();
				return FieldsInOrder
					.Select(f => (Delegate)f.GetValue(this));
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SnesInitData
		{
			public ENTROPY entropy;
			public BSNES_PORT1_INPUT_DEVICE left_port;
			public BSNES_INPUT_DEVICE right_port;
			public bool hotfixes;
			public bool fast_ppu;
			public bool fast_dsp;
			public bool fast_coprocessors;
			public REGION_OVERRIDE region_override;
		}

		public void Seal()
		{
			exe.Seal();
			foreach (string s in _readonlyFiles.Where(s => !s.StartsWithOrdinal("msu1/")))
			{
				exe.RemoveReadonlyFile(s);
			}

			_readonlyFiles.RemoveAll(s => !s.StartsWithOrdinal("msu1/"));
		}

		// private int serializedSize;

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			// commented code left for debug purposes; created savestates are native bsnes savestates
			// and therefor compatible across minor core updates

			// if (serializedSize == 0) serializedSize = core.snes_serialized_size();
			// byte[] serializedData = new byte[serializedSize];
			// core.snes_serialize(serializedData, serializedSize);
			// writer.Write(serializedData);
			exe.SaveStateBinary(writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			// if (serializedSize == 0) serializedSize = core.snes_serialized_size();
			// byte[] serializedData = reader.ReadBytes(serializedSize);
			// core.snes_unserialize(serializedData, serializedSize);
			exe.LoadStateBinary(reader);
			core.snes_msu_sync();
		}
	}
}
