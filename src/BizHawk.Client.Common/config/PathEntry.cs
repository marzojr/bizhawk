using System.Text.Json.Serialization;

namespace BizHawk.Client.Common
{
	public sealed class PathEntry
	{
		public string Type { get; set; }
		private string _path;
		public string Path
		{
			get => _path;
			set => _path = value.Replace('\\', '/');
		}
		public string System { get; set; }

		[JsonIgnore]
		public readonly int Ordinal;

		public PathEntry(string system, string type, string path)
		{
			Ordinal = type switch
			{
				// all
				"Base" => 0x00,
				"ROM" => 0x01,

				// only Global
				"Firmware" => 0x10,
				"Movies" => 0x11,
				"Movie backups" => 0x12,
				"A/V Dumps" => 0x13,
				"Tools" => 0x14,
				"Lua" => 0x15,
				"Watch (.wch)" => 0x16,
				"Debug Logs" => 0x17,
				"Macros" => 0x18,
				"Multi-Disk Bundles" => 0x1A,
				"External Tools" => 0x1B,
				"Temp Files" => 0x1C,

				// only Libretro
				"Cores" => 0x10,
				"System" => 0x11,

				// all cores incl. Libretro
				"Savestates" => 0x20,
				"Save RAM" => 0x21,
				"Screenshots" => 0x22,
				"Cheats" => 0x23,

				// some cores
				"Palettes" => 0x30,

				// currently Encore only
				// potentially applicable for future cores (Dolphin?)
				"User" => 0x40,

				_ => 0x50
			};
			Path = path;
			System = system;
			Type = type;
		}

		internal bool IsSystem(string systemID)
			=> PathEntryCollection.InGroup(systemID, System);
	}
}
