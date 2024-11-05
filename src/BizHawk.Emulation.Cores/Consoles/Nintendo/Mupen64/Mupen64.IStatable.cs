using System.Buffers;
using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IStatable
{
	private enum SavestatesType
	{
		Unknown,
		M64P,
		Pj64Zip,
		Pj64Unc
	}

	private const int SAVESTATE_SIZE = 16788288 + 1024 + 4 + 4096;

	public bool AvoidRewind => false;

	public void SaveStateBinary(BinaryWriter writer)
	{
		writer.Write(Frame);
		writer.Write(LagCount);
		writer.Write(IsLagFrame);

		byte[] savestateBuffer = ArrayPool<byte>.Shared.Rent(SAVESTATE_SIZE);

		Mupen64Api.SaveSavestate(savestateBuffer);
		writer.Write(savestateBuffer, 0, SAVESTATE_SIZE);

		ArrayPool<byte>.Shared.Return(savestateBuffer);
	}

	public void LoadStateBinary(BinaryReader reader)
	{
		Frame = reader.ReadInt32();
		LagCount = reader.ReadInt32();
		IsLagFrame = reader.ReadBoolean();

		var tempFileName = TempFileManager.GetTempFilename("mupen64-savestate");
		using var tempFile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
		reader.BaseStream.CopyTo(tempFile);

		Mupen64Api.CoreDoCommand(Mupen64Api.m64p_command.STATE_LOAD, (int)SavestatesType.M64P, tempFileName);
	}
}
