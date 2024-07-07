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

	public bool AvoidRewind => false;

	public void SaveStateBinary(BinaryWriter writer)
	{
		writer.Write(Frame);

		var tempFileName = TempFileManager.GetTempFilename("mupen64-savestate");
		using var tempFile = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write);

		Mupen64Api.CoreDoCommand(Mupen64Api.m64p_command.M64CMD_STATE_SAVE, (int)SavestatesType.M64P, tempFileName);
		tempFile.CopyTo(writer.BaseStream);
	}

	public void LoadStateBinary(BinaryReader reader)
	{
		Frame = reader.ReadInt32();

		var tempFileName = TempFileManager.GetTempFilename("mupen64-savestate");
		using var tempFile = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
		reader.BaseStream.CopyTo(tempFile);

		var error = Mupen64Api.CoreDoCommand(Mupen64Api.m64p_command.M64CMD_STATE_LOAD, (int)SavestatesType.M64P, tempFileName);
	}
}
