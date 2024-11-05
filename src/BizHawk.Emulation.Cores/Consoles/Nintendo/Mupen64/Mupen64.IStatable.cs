using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IStatable
{
	private const int SAVESTATE_SIZE = 16788288 + 1024 + 4 + 4096;
	private readonly byte[] _savestateBuffer = new byte[SAVESTATE_SIZE];

	public bool AvoidRewind => false;

	public void SaveStateBinary(BinaryWriter writer)
	{
		writer.Write(Frame);
		writer.Write(LagCount);
		writer.Write(IsLagFrame);

		Mupen64Api.SaveSavestate(_savestateBuffer);
		writer.Write(_savestateBuffer, 0, SAVESTATE_SIZE);
	}

	public void LoadStateBinary(BinaryReader reader)
	{
		Frame = reader.ReadInt32();
		LagCount = reader.ReadInt32();
		IsLagFrame = reader.ReadBoolean();

		bool success = reader.Read(_savestateBuffer, 0, SAVESTATE_SIZE) == SAVESTATE_SIZE;
		if (success)
		{
			success = Mupen64Api.LoadSavestate(_savestateBuffer);
		}

		if (!success)
		{
			Console.Error.WriteLine("Failed to load mupen savestate!");
		}
	}
}
