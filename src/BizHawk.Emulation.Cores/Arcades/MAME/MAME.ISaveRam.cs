using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : ISaveRam
	{
		private readonly LibMAME.FilenameCallbackDelegate _filenameCallback;
		private readonly List<string> _nvramFilenames = new();
		private const string NVRAM_MAGIC = "MAMEHAWK_NVRAM";

		private void GetNVRAMFilenames() => _core.mame_nvram_get_filenames(_filenameCallback);

		public bool SaveRamModified => _nvramFilenames.Count > 0;

		public byte[] CloneSaveRam()
		{
			if (_nvramFilenames.Count == 0)
			{
				return null;
			}

			for (int i = 0; i < _nvramFilenames.Count; i++)
			{
				_exe.AddTransientFile(Array.Empty<byte>(), _nvramFilenames[i]);
			}

			_core.mame_nvram_save();

			using MemoryStream ms = new();
			using BinaryWriter writer = new(ms);

			writer.Write(NVRAM_MAGIC);
			writer.Write(_nvramFilenames.Count);

			for (int i = 0; i < _nvramFilenames.Count; i++)
			{
				byte[] res = _exe.RemoveTransientFile(_nvramFilenames[i]);
				writer.Write(_nvramFilenames[i]);
				writer.Write(res.Length);
				writer.Write(res);
			}

			return ms.ToArray();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_nvramFilenames.Count == 0)
			{
				return;
			}

			using MemoryStream ms = new(data, false);
			using BinaryReader reader = new(ms);

			if (reader.ReadString() != NVRAM_MAGIC)
			{
				throw new InvalidOperationException("Bad NVRAM magic!");
			}

			int cnt = reader.ReadInt32();
			if (cnt != _nvramFilenames.Count)
			{
				throw new InvalidOperationException($"Wrong NVRAM file count! (got {cnt}, expected {_nvramFilenames.Count})");
			}

			List<string> nvramFilesToClose = new();
			void RemoveFiles()
			{
				foreach (string nvramFileToClose in nvramFilesToClose)
				{
					_exe.RemoveReadonlyFile(nvramFileToClose);
				}
			}

			try
			{
				for (int i = 0; i < cnt; i++)
				{
					string name = reader.ReadString();
					if (name != _nvramFilenames[i])
					{
						throw new InvalidOperationException($"Wrong NVRAM filename! (got {name}, expected {_nvramFilenames[i]})");
					}

					int len = reader.ReadInt32();
					byte[] buf = reader.ReadBytes(len);

					if (len != buf.Length)
					{
						throw new InvalidOperationException($"Unexpected NVRAM size difference! (got {buf.Length}, expected {len})");
					}

					_exe.AddReadonlyFile(buf, name);
					nvramFilesToClose.Add(name);
				}
			}
			catch
			{
				RemoveFiles();
				throw;
			}

			_core.mame_nvram_load();
			RemoveFiles();
		}
	}
}
