﻿using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : ISettable<ChannelF.ChannelFSettings, ChannelF.ChannelFSyncSettings>
	{
		internal ChannelFSettings Settings = new ChannelFSettings();
		internal ChannelFSyncSettings SyncSettings = new ChannelFSyncSettings();

		public ChannelFSettings GetSettings()
		{
			return Settings.Clone();
		}

		public ChannelFSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(ChannelFSettings o)
		{
			Settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(ChannelFSyncSettings o)
		{
			bool ret = ChannelFSyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class ChannelFSettings
		{
			public ChannelFSettings Clone()
			{
				return (ChannelFSettings)MemberwiseClone();
			}

			public ChannelFSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		[CoreSettings]
		public class ChannelFSyncSettings
		{
			[DisplayName("Deterministic Emulation")]
			[Description("If true, the core agrees to behave in a completely deterministic manner")]
			[DefaultValue(true)]
			public bool DeterministicEmulation { get; set; }
			[DisplayName("Region")]
			[Description("NTSC or PAL - Affects the CPU clock speed and refresh rate")]
			[DefaultValue(RegionType.NTSC)]
			public RegionType Region { get; set; }
			[DisplayName("Version")]
			[Description("Both versions are the same from an emulation perspective. Channel F II has a very slightly different BIOS to Channel F")]
			[DefaultValue(ConsoleVersion.ChannelF)]
			public ConsoleVersion Version { get; set; }

			public ChannelFSyncSettings Clone()
			{
				return (ChannelFSyncSettings) MemberwiseClone();
			}

			public ChannelFSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(ChannelFSyncSettings x, ChannelFSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		public enum RegionType
		{
			NTSC,
			PAL
		}

		public enum ConsoleVersion
		{
			ChannelF,
			ChannelF_II
		}
	}
}
