using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : ISaveRam
{
	public byte[] CloneSaveRam()
	{
		int saveRamSize = 0;
		Mupen64Api.GetSaveRamSize(ref saveRamSize);
		byte[] saveRamBuffer = new byte[saveRamSize];

		Mupen64Api.GetSaveRam(saveRamBuffer);

		return saveRamBuffer;
	}

	public void StoreSaveRam(byte[] data)
	{
		int saveRamSize = 0;
		Mupen64Api.GetSaveRamSize(ref saveRamSize);
		if (saveRamSize != data.Length)
		{
			throw new InvalidOperationException($"Core expects a savestate of size {saveRamSize}, but got {data.Length} bytes!");
		}

		Mupen64Api.PutSaveRam(data);
	}

	public bool SaveRamModified => true;
}
