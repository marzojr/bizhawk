using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.Cores.Properties {
	internal static class Resources {
		/// <param name="embedPath">Dir separator is '<c>.</c>'. Path is relative to <c>&lt;NS></c>.</param>
		private static byte[] ReadEmbeddedByteArray(string embedPath) => ReflectionCache.EmbeddedResourceStream($"Resources.{embedPath}").ReadAllBytes();

		internal static readonly Lazy<byte[]> CPC_AMSDOS_0_5_ROM = new(() => ReadEmbeddedByteArray("CPC_AMSDOS_0.5.ROM.zst"));
		internal static readonly Lazy<byte[]> CPC_BASIC_1_0_ROM = new(() => ReadEmbeddedByteArray("CPC_BASIC_1.0.ROM.zst"));
		internal static readonly Lazy<byte[]> CPC_BASIC_1_1_ROM = new(() => ReadEmbeddedByteArray("CPC_BASIC_1.1.ROM.zst"));
		internal static readonly Lazy<byte[]> CPC_OS_6128_ROM = new(() => ReadEmbeddedByteArray("CPC_OS_6128.ROM.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_BASE_CONF = new(() => ReadEmbeddedByteArray("dosbox-x.conf.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_2GB = new(() => ReadEmbeddedByteArray("dosbox-x.hd_2gig.img.zst"));
		internal static readonly Lazy<byte[]> DOSBOX_HDD_IMAGE_FAT16_21MB = new(() => ReadEmbeddedByteArray("dosbox-x.hd_21mb.img.zst"));
		internal static readonly Lazy<byte[]> OS_464_ROM = new(() => ReadEmbeddedByteArray("OS_464.ROM.zst"));
		internal static readonly Lazy<byte[]> FastCgbBoot = new(() => ReadEmbeddedByteArray("cgb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> FastAgbBoot = new(() => ReadEmbeddedByteArray("agb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> FastDmgBoot = new(() => ReadEmbeddedByteArray("dmg_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SameboyCgbBoot = new(() => ReadEmbeddedByteArray("sameboy_cgb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SameboyAgbBoot = new(() => ReadEmbeddedByteArray("sameboy_agb_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SameboyDmgBoot = new(() => ReadEmbeddedByteArray("sameboy_dmg_boot.rom.zst"));
		internal static readonly Lazy<byte[]> SgbCartPresent_SPC = new(() => ReadEmbeddedByteArray("sgb-cart-present.spc.zst"));
		internal static readonly Lazy<byte[]> ZX_128_ROM = new(() => ReadEmbeddedByteArray("128.ROM.zst"));
		internal static readonly Lazy<byte[]> ZX_48_ROM = new(() => ReadEmbeddedByteArray("48.ROM.zst"));
		internal static readonly Lazy<byte[]> ZX_plus2_rom = new(() => ReadEmbeddedByteArray("plus2.rom.zst"));
		internal static readonly Lazy<byte[]> ZX_plus2a_rom = new(() => ReadEmbeddedByteArray("plus2a.rom.zst"));
		internal static readonly Lazy<byte[]> TMDS = new(() => ReadEmbeddedByteArray("tmds.zip.zst"));
		internal static readonly Lazy<byte[]> PIF_PAL_ROM = new(() => ReadEmbeddedByteArray("pif.pal.rom.zst"));
		internal static readonly Lazy<byte[]> PIF_NTSC_ROM = new(() => ReadEmbeddedByteArray("pif.ntsc.rom.zst"));
		internal static readonly Lazy<byte[]> JAGUAR_KSERIES_ROM = new(() => ReadEmbeddedByteArray("JAGUAR_KSERIES.ROM.zst"));
		internal static readonly Lazy<byte[]> JAGUAR_MSERIES_ROM = new(() => ReadEmbeddedByteArray("JAGUAR_MSERIES.ROM.zst"));
		internal static readonly Lazy<byte[]> JAGUAR_MEMTRACK_ROM = new(() => ReadEmbeddedByteArray("JAGUAR_MEMTRACK.ROM.zst"));
		internal static readonly Lazy<byte[]> DSDA_DOOM_WAD = new(() => ReadEmbeddedByteArray("dsda-doom.zst"));
	}
}
