//http://wiki.superfamicom.org/snes/show/Backgrounds
//http://board.zsnes.com/phpBB3/viewtopic.php?f=10&t=13029&start=75 yoshis island offset per tile demos. and other demos of advanced modes
//but we wont worry about offset per tile modes here.

//helpful detailed reg list
//http://wiki.superfamicom.org/snes/show/Registers

//TODO
//when a BG is not available, the last rendered BG still shows up. should clear it

using BizHawk.Common;

using System.Drawing;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe interface ISNESGraphicsDecoder : IMonitor
	{
		public interface OAMInfo
		{
			ushort X { get; }

			byte Y { get; }

			int Tile { get; }

			bool Table { get; }

			int Palette { get; }

			byte Priority { get; }

			bool VFlip { get; }

			bool HFlip { get; }

			bool Size { get; }

			int Address { get; }
		}

		void CacheTiles();

		int Colorize(int rgb555);

		void Colorize(int* buf, int offset, int numpixels);

		OAMInfo CreateOAMInfo(SNESGraphicsDecoder.ScreenInfo si, int num);

		void DecodeBG(
			int* screen,
			int stride,
			SNESGraphicsDecoder.TileEntry[] map,
			int tiledataBaseAddr,
			SNESGraphicsDecoder.ScreenSize size,
			int bpp,
			int tilesize,
			int paletteStart);

		void DecodeMode7BG(int* screen, int stride, bool extBg);

		void DirectColorify(int* screen, int numPixels);

		SNESGraphicsDecoder.TileEntry[] FetchMode7Tilemap();

		SNESGraphicsDecoder.TileEntry[] FetchTilemap(int addr, SNESGraphicsDecoder.ScreenSize size);

		int[] GetPalette();

		void Paletteize(int* buf, int offset, int startcolor, int numpixels);

		void RenderMode7TilesToScreen(
			int* screen,
			int stride,
			bool ext,
			bool directColor,
			int tilesWide = 16,
			int startTile = 0,
			int numTiles = 256);

		void RenderSpriteToScreen(
			int* screen,
			int stride,
			int destx,
			int desty,
			SNESGraphicsDecoder.ScreenInfo si,
			int spritenum,
			OAMInfo oam = null,
			int xlimit = 1024,
			int ylimit = 1024,
			byte[,] spriteMap = null);

		void RenderTilesToScreen(
			int* screen,
			int stride,
			int bpp,
			int startcolor,
			int startTile = 0,
			int numTiles = -1);

		SNESGraphicsDecoder.ScreenInfo ScanScreenInfo();

		void SetBackColor(int snescol = -1);
	}

	public static class SNESGraphicsDecoder
	{
		public class PaletteSelection
		{
			public PaletteSelection() { }
			public PaletteSelection(int start, int size)
			{
				this.start = start;
				this.size = size;
			}
			public int start, size;
		}

		public struct Dimensions
		{
			public Dimensions(int w, int h) { Width = w; Height = h; }
			public int Width, Height;
			public override string ToString() => $"{Width}x{Height}";
		}

		public enum ScreenSize
		{
			AAAA_32x32 = 0, ABAB_64x32 = 1, AABB_32x64 = 2, ABCD_64x64 = 3,
			Hacky_1x1 = 4,
		}

		public static Dimensions SizeInTilesForBGSize(ScreenSize size)
		{
			if (size == ScreenSize.Hacky_1x1) return new Dimensions(1, 1);
			var ret = SizeInBlocksForBGSize(size);
			ret.Width *= 32;
			ret.Height *= 32;
			return ret;
		}

		public static readonly Size[,] ObjSizes =
		{
			{ new(8,8), new(16,16) },
			{ new(8,8), new(32,32) },
			{ new(8,8), new(64,64) },
			{ new(16,16), new(32,32) },
			{ new(16,16), new(64,64) },
			{ new(32,32), new(64,64) },
			{ new(16,32), new(32,64) },
			{ new(16,32), new(32,32) }
		};

		public static Dimensions SizeInBlocksForBGSize(ScreenSize size)
		{
			return size switch
			{
				ScreenSize.AAAA_32x32 => new Dimensions(1, 1),
				ScreenSize.ABAB_64x32 => new Dimensions(2, 1),
				ScreenSize.AABB_32x64 => new Dimensions(1, 2),
				ScreenSize.ABCD_64x64 => new Dimensions(2, 2),
				_ => throw new InvalidOperationException()
			};
		}

		public enum BGMode
		{
			Unavailable, Text, Mode7, Mode7Ext, Mode7DC, OBJ
		}

		/// <summary>
		/// is a BGMode a mode7 type (mode7, mode7ext, mode7DC)
		/// </summary>
		public static bool BGModeIsMode7Type(BGMode bgMode) => bgMode == BGMode.Mode7 || bgMode == BGMode.Mode7DC || bgMode == BGMode.Mode7Ext;

		/// <summary>
		/// this class is not 'smart' - it wont recompute values for you. it's meant to be read only (we should find some way to protect write access to make that clear)
		/// </summary>
		public class BGInfo
		{
			public BGInfo(int num)
			{
			}

			/// <summary>
			/// what type of BG is it?
			/// </summary>
			public BGMode BGMode;

			/// <summary>
			/// is this BGMode a mode7 type (mode7, mode7ext, mode7DC)
			/// </summary>
			public bool BGModeIsMode7Type => BGModeIsMode7Type(BGMode);

			/// <summary>
			/// Is the layer even enabled?
			/// </summary>
			public bool Enabled => Bpp != 0;

			/// <summary>
			/// screen and tiledata register values
			/// </summary>
			public int SCADDR, TDADDR;

			/// <summary>
			/// SCSIZE register
			/// </summary>
			public int SCSIZE;

			/// <summary>
			/// which Mode this BG came from
			/// </summary>
			public int Mode;

			/// <summary>
			/// the address of the screen data
			/// </summary>
			public int ScreenAddr;

			/// <summary>
			/// the address of the tile data
			/// </summary>
			public int TiledataAddr;

			/// <summary>
			/// Screen size (shape, really.)
			/// </summary>
			public ScreenSize ScreenSize => (ScreenSize)SCSIZE;

			/// <summary>
			/// the BPP of the BG, as derived from the current mode
			/// </summary>
			public int Bpp;

			/// <summary>
			/// value of the tilesize register; 1 implies 16x16 tiles
			/// </summary>
			public int TILESIZE;

			/// <summary>
			/// enabled on MAIN Screen via $212C
			/// </summary>
			public bool MainEnabled;

			/// <summary>
			/// enabled on SUB Screen via $212D
			/// </summary>
			public bool SubEnabled;

			/// <summary>
			/// enabled for color math via $2131
			/// </summary>
			public bool MathEnabled;

			/// <summary>
			/// scroll registers
			/// </summary>
			public int HOFS, VOFS;

			/// <summary>
			/// TileSize; 8 or 16
			/// </summary>
			public int TileSize => TILESIZE == 1 ? 16 : 8;

			/// <summary>
			/// The size of the layer, in tiles
			/// </summary>
			public Dimensions ScreenSizeInTiles => BGMode == BGMode.Text
				? SizeInTilesForBGSize(ScreenSize)
				: new Dimensions(128, 128);

			/// <summary>
			/// The size of the layer, in pixels. This has factored in the selection of 8x8 or 16x16 tiles
			/// </summary>
			public Dimensions ScreenSizeInPixels => new Dimensions(ScreenSizeInTiles.Width * TileSize, ScreenSizeInTiles.Height * TileSize);

			/// <summary>
			/// returns information about what colors could possibly be used for this bg
			/// </summary>
			public PaletteSelection PaletteSelection;
		}

		public class BGInfos
		{
			private readonly BGInfo[] bgs = new BGInfo[4] { new BGInfo(1), new BGInfo(2), new BGInfo(3), new BGInfo(4) };
			public BGInfo BG1 => bgs[0];
			public BGInfo BG2 => bgs[1];
			public BGInfo BG3 => bgs[2];
			public BGInfo BG4 => bgs[3];
			public BGInfo this[int index] => bgs[index - 1];
		}

		public class ScreenInfo
		{
			public Size ObjSizeBounds;
			public Size ObjSizeBoundsSquare;

			public BGInfos BG = new BGInfos();

			public int Mode { get; init; }
			public bool Mode1_BG3_Priority { get; init; }

			public bool SETINI_Mode7ExtBG { get; init; }
			public bool SETINI_HiRes { get; init; }
			public bool SETINI_Overscan { get; init; }
			public bool SETINI_ObjInterlace { get; init; }
			public bool SETINI_ScreenInterlace { get; init; }

			public int CGWSEL_ColorMask { get; init; }
			public int CGWSEL_ColorSubMask { get; init; }
			public int CGWSEL_AddSubMode { get; init; }
			public bool CGWSEL_DirectColor { get; init; }
			public int CGADSUB_AddSub { get; init; }
			public bool CGADSUB_Half { get; init; }

			public int OBSEL_Size { get; init; }
			public int OBSEL_NameSel { get; init; }
			public int OBSEL_NameBase { get; init; }

			public int OBJTable0Addr { get; init; }
			public int OBJTable1Addr { get; init; }

			public bool OBJ_MainEnabled { get; init; }
			public bool OBJ_SubEnabled { get; init; }
			public bool OBJ_MathEnabled { get; init; }
			public bool BK_MathEnabled { get; init; }

			public int M7HOFS { get; init; }
			public int M7VOFS { get; init; }
			public int M7A { get; init; }
			public int M7B { get; init; }
			public int M7C { get; init; }
			public int M7D { get; init; }
			public int M7X { get; init; }
			public int M7Y { get; init; }
			public int M7SEL_REPEAT { get; init; }
			public bool M7SEL_HFLIP { get; init; }
			public bool M7SEL_VFLIP { get; init; }
		}

		public static readonly int[,] ModeBpps = {
				{2,2,2,2},
				{4,4,2,0},
				{4,4,0,0},
				{8,4,0,0},
				{8,2,0,0},
				{4,2,0,0},
				{4,0,0,0},
				{8,0,0,0},
				{8,7,0,0}
			};

		//the same basic color table that libsnes uses to convert from snes 555 to rgba32
		private static readonly int[] directColorTable = new int[256]; //8bpp gfx -> rgb555
		static SNESGraphicsDecoder()
		{
			//make directColorTable
			for (int i = 0; i < 256; i++)
			{
				int r = i & 7;
				int g = (i >> 3) & 7;
				int b = (i >> 6) & 3;
				r <<= 2;
				g <<= 2;
				b <<= 3;
				int color = (b << 10) | (g << 5) | r;
				directColorTable[i] = color;
			}
		}

		public struct TileEntry
		{
			public ushort tilenum;
			public byte palette;
			public TileEntryFlags flags;
			public int address;
		}

		[Flags]
		public enum TileEntryFlags : byte
		{
			None = 0, Priority = 1, Horz = 2, Vert = 4,
		}
	} //class SNESGraphicsDecoder
} //namespace
