//#define USE_UPSTREAM_STATES // really more for testing due to needing to use these anyways for initial state code. could potentially be used outright for states

using System.IO;
using System.Text.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IStatable, ITextStatable
	{
		public bool AvoidRewind => false;

		public void SaveStateText(TextWriter writer)
		{
			var s = SaveState();
			writer.Write(JsonSerializer.Serialize(s, _options));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = JsonSerializer.Deserialize<TextState<TextStateData>>(reader.ReadToEnd(), _options);
			LoadState(s);
			reader.ReadToEnd();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
#if USE_UPSTREAM_STATES
			int size = LibGambatte.gambatte_savestate(GambatteState, null, 160, _stateBuf);
			if (size != _stateBuf.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}
#else
			if (!LibGambatte.gambatte_newstatesave(GambatteState, _stateBuf, _stateBuf.Length))
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_newstatesave)}() returned false");
			}
#endif

			writer.Write(_stateBuf.Length);
			writer.Write(_stateBuf);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(frameOverflow);
			writer.Write(_cycleCount);
			writer.Write(IsCgb);
			writer.Write(IsSgb);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var length = reader.ReadInt32();
			if (length != _stateBuf.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			reader.Read(_stateBuf, 0, _stateBuf.Length);

#if USE_UPSTREAM_STATES
			if (!LibGambatte.gambatte_loadstate(GambatteState, _stateBuf, _stateBuf.Length))
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadstate)}() returned false");
			}
#else
			if (!LibGambatte.gambatte_newstateload(GambatteState, _stateBuf, _stateBuf.Length))
			{
				throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_newstateload)}() returned false");
			}
#endif

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			frameOverflow = reader.ReadUInt32();
			_cycleCount = reader.ReadUInt64();
			IsCgb = reader.ReadBoolean();
			IsSgb = reader.ReadBoolean();
		}

		private byte[] _stateBuf;

		private void NewSaveCoreSetBuff()
#if USE_UPSTREAM_STATES
			=> _stateBuf = new byte[LibGambatte.gambatte_savestate(GambatteState, null, 160, null)];
#else
			=> _stateBuf = new byte[LibGambatte.gambatte_newstatelen(GambatteState)];
#endif

		private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

		// other data in the text state besides core
		internal class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public ulong _cycleCount;
			public uint frameOverflow;
			public bool IsCgb;
			public bool IsSgb;
		}

		internal TextState<TextStateData> SaveState()
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			LibGambatte.gambatte_newstatesave_ex(GambatteState, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;
			s.ExtraData.frameOverflow = frameOverflow;
			s.ExtraData._cycleCount = _cycleCount;
			s.ExtraData.IsCgb = IsCgb;
			s.ExtraData.IsSgb = IsSgb;
			return s;
		}

		internal void LoadState(TextState<TextStateData> s)
		{
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			LibGambatte.gambatte_newstateload_ex(GambatteState, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
			frameOverflow = s.ExtraData.frameOverflow;
			_cycleCount = s.ExtraData._cycleCount;
			IsCgb = s.ExtraData.IsCgb;
			IsSgb = s.ExtraData.IsSgb;
		}
	}
}
