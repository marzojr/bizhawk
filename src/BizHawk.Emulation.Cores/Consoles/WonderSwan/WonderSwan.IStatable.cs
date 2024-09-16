using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan: ITextStatable
	{
		private void InitIStatable()
			=> savebuff = new byte[BizSwan.bizswan_binstatesize(Core)];

		private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

		[StructLayout(LayoutKind.Sequential)]
		private class TextStateData
		{
			public bool IsLagFrame;
			public int LagCount;
			public int Frame;
		}

		private void LoadTextStateData(TextStateData d)
		{
			IsLagFrame = d.IsLagFrame;
			LagCount = d.LagCount;
			Frame = d.Frame;
		}

		private void SaveTextStateData(TextStateData d)
		{
			d.IsLagFrame = IsLagFrame;
			d.LagCount = LagCount;
			d.Frame = Frame;
		}

		public void SaveStateText(TextWriter writer)
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			BizSwan.bizswan_txtstatesave(Core, ref ff);
			SaveTextStateData(s.ExtraData);
			writer.Write(JsonSerializer.Serialize(s, _options));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = JsonSerializer.Deserialize<TextState<TextStateData>>(reader.ReadToEnd(), _options);
			if (s is not null)
			{
				s.Prepare();
				var ff = s.GetFunctionPointersLoad();
				BizSwan.bizswan_txtstateload(Core, ref ff);
				LoadTextStateData(s.ExtraData);
			}
			else
			{
				throw new InvalidOperationException("Failed to deserialize state");
			}
		}

		private byte[] savebuff;

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!BizSwan.bizswan_binstatesave(Core, savebuff, savebuff.Length))
			{
				throw new InvalidOperationException($"{nameof(BizSwan.bizswan_binstatesave)}() returned false!");
			}

			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var length = reader.ReadInt32();
			if (length != savebuff.Length)
			{
				throw new InvalidOperationException("Save buffer size mismatch!");
			}

			reader.Read(savebuff, 0, length);
			if (!BizSwan.bizswan_binstateload(Core, savebuff, savebuff.Length))
			{
				throw new InvalidOperationException($"{nameof(BizSwan.bizswan_binstateload)}() returned false!");
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}
	}
}
