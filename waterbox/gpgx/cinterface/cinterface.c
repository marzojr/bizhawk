#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>
#include <emulibc.h>
#include "callbacks.h"

#ifdef _MSC_VER
#define snprintf _snprintf
#endif

#include "shared.h"
#include "genesis.h"
#include "md_ntsc.h"
#include "sms_ntsc.h"
#include "eeprom_i2c.h"
#include "vdp_render.h"

char GG_ROM[256] = "GG_ROM"; // game genie rom
char AR_ROM[256] = "AR_ROM"; // actin replay rom
char SK_ROM[256] = "SK_ROM"; // sanic and knuckles
char SK_UPMEM[256] = "SK_UPMEM"; // sanic and knuckles
char GG_BIOS[256] = "GG_BIOS"; // game gear bootrom
char CD_BIOS_EU[256] = "CD_BIOS_EU"; // cd bioses
char CD_BIOS_US[256] = "CD_BIOS_US";
char CD_BIOS_JP[256] = "CD_BIOS_JP";
char MS_BIOS_US[256] = "MS_BIOS_US"; // master system bioses
char MS_BIOS_EU[256] = "MS_BIOS_EU";
char MS_BIOS_JP[256] = "MS_BIOS_JP";

char romextension[4];

static int16 soundbuffer[4096];
static int nsamples;

int cinterface_render_bga = 1;
int cinterface_render_bgb = 1;
int cinterface_render_bgw = 1;
int cinterface_render_obj = 1;
uint8 cinterface_custom_backdrop = 0;
uint32 cinterface_custom_backdrop_color = 0xffff00ff; // pink
extern uint8 border;

int cinterface_force_sram = 0;

#define GPGX_EX ECL_EXPORT

static int vwidth;
static int vheight;

static uint8_t brm_format[0x40] =
{
	0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x00,0x00,0x00,0x00,0x40,
	0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
	0x53,0x45,0x47,0x41,0x5f,0x43,0x44,0x5f,0x52,0x4f,0x4d,0x00,0x01,0x00,0x00,0x00,
	0x52,0x41,0x4d,0x5f,0x43,0x41,0x52,0x54,0x52,0x49,0x44,0x47,0x45,0x5f,0x5f,0x5f
};

ECL_ENTRY void (*biz_execcb)(unsigned addr);
ECL_ENTRY void (*biz_readcb)(unsigned addr, unsigned int value);
ECL_ENTRY void (*biz_writecb)(unsigned addr, unsigned int value);
CDCallback biz_cdcallback = NULL;
unsigned biz_lastpc = 0;
ECL_ENTRY void (*cdd_readcallback)(int lba, void *dest, int audio);
uint8 *tempsram;

static void update_viewport(void)
{
	vwidth  = bitmap.viewport.w + (bitmap.viewport.x * 2);
	vheight = bitmap.viewport.h + (bitmap.viewport.y * 2);

	if (config.ntsc)
	{
		if (reg[12] & 1)
			vwidth = MD_NTSC_OUT_WIDTH(vwidth);
		else
			vwidth = SMS_NTSC_OUT_WIDTH(vwidth);
	}

	if (config.render && interlaced)
	{
		vheight = vheight * 2;
	}
}

GPGX_EX void gpgx_get_video(int *w, int *h, int *pitch, void **buffer)
{
	if (w)
		*w = vwidth;
	if (h)
		*h = vheight;
	if (pitch)
		*pitch = bitmap.pitch;
	if (buffer)
		*buffer = bitmap.data;
}

GPGX_EX void gpgx_get_audio(int *n, void **buffer)
{
	if (n)
		*n = nsamples;
	if (buffer)
		*buffer = soundbuffer;
}

// this is most certainly wrong for interlacing
GPGX_EX void gpgx_get_fps(int *num, int *den)
{
	if (vdp_pal)
	{
		if (num)
			*num = 53203424;
		if (den)
			*den = 3420 * 313;
	}
	else
	{
		if (num)
			*num = 53693175;
		if (den)
			*den = 3420 * 262;
	}
}

void osd_input_update(void)
{
}

ECL_ENTRY void (*input_callback_cb)(void);

void real_input_callback(void)
{
	if (input_callback_cb)
		input_callback_cb();
}

GPGX_EX void gpgx_set_input_callback(ECL_ENTRY void (*fecb)(void))
{
	input_callback_cb = fecb;
}

GPGX_EX void gpgx_set_cdd_callback(ECL_ENTRY void (*cddcb)(int lba, void *dest, int audio))
{
	cdd_readcallback = cddcb;
}

ECL_ENTRY int (*load_archive_cb)(const char *filename, unsigned char *buffer, int maxsize);

// return 0 on failure, else actual loaded size
// extension, if not null, should be populated with the extension of the file loaded
// (up to 3 chars and null terminator, no more)
int load_archive(const char *filename, unsigned char *buffer, int maxsize, char *extension)
{
	if (extension)
		memcpy(extension, romextension, 4);

	return load_archive_cb(filename, buffer, maxsize);
}

GPGX_EX int gpgx_get_control(t_input *dest, int bytes)
{
	if (bytes != sizeof(t_input))
		return 0;
	memcpy(dest, &input, sizeof(t_input));
	return 1;
}

GPGX_EX int gpgx_put_control(t_input *src, int bytes)
{
	if (bytes != sizeof(t_input))
		return 0;
	memcpy(&input, src, sizeof(t_input));
	return 1;
}

GPGX_EX void gpgx_advance(void)
{
	if (system_hw == SYSTEM_MCD)
		system_frame_scd(0);
	else if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
		system_frame_gen(0);
	else
		system_frame_sms(0);

	if (bitmap.viewport.changed & 1)
	{
		bitmap.viewport.changed &= ~1;
		update_viewport();
	}

	nsamples = audio_update(soundbuffer);
}

GPGX_EX void gpgx_swap_disc(const toc_t* toc)
{
	if (system_hw == SYSTEM_MCD)
	{
		cdd_hotswap(toc);
	}
}

typedef struct
{
	uint32 width; // in cells
	uint32 height;
	uint32 baseaddr;
} nametable_t;

typedef struct
{
	uint8 *vram; // 64K vram
	uint8 *patterncache; // every pattern, first normal, then hflip, vflip, bothflip
	uint32 *colorcache; // 64 colors
	nametable_t nta;
	nametable_t ntb;
	nametable_t ntw;
} vdpview_t;


extern uint8 *bg_pattern_cache;
extern uint32 pixel[];

GPGX_EX void gpgx_get_vdp_view(vdpview_t *view)
{
	view->vram = vram;
	view->patterncache = bg_pattern_cache;
	view->colorcache = pixel + 0x40;
	view->nta.width = 1 << (playfield_shift - 1);
	view->ntb.width = 1 << (playfield_shift - 1);
	view->nta.height = (playfield_row_mask + 1) >> 3;
	view->ntb.height = (playfield_row_mask + 1) >> 3;
	view->ntw.width = 1 << (5 + (reg[12] & 1));
	view->ntw.height = 32;
	view->nta.baseaddr = ntab;
	view->ntb.baseaddr = ntbb;
	view->ntw.baseaddr = ntwb;
}

// internal: computes sram size (no brams)
int saveramsize(void)
{
	return sram_get_actual_size();
}

GPGX_EX void gpgx_clear_sram(void)
{
	// clear sram
	if (sram.on)
		memset(sram.sram, 0xff, 0x10000);

	if (cdd.loaded)
	{
		// clear and format bram
		memset(scd.bram, 0, 0x2000);
		brm_format[0x10] = brm_format[0x12] = brm_format[0x14] = brm_format[0x16] = 0x00;
		brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (0x2000 / 64) - 3;
		memcpy(scd.bram + 0x2000 - 0x40, brm_format, 0x40);

		if (scd.cartridge.id)
		{
			// clear and format ebram
			memset(scd.cartridge.area, 0x00, scd.cartridge.mask + 1);
			brm_format[0x10] = brm_format[0x12] = brm_format[0x14] = brm_format[0x16] = (((scd.cartridge.mask + 1) / 64) - 3) >> 8;
			brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (((scd.cartridge.mask + 1) / 64) - 3) & 0xff;
			memcpy(scd.cartridge.area + scd.cartridge.mask + 1 - 0x40, brm_format, 0x40);
		}
	}
}

// a bit hacky:
// in order to present a single memory block to the frontend,
// we copy the both the bram and the ebram to another area

GPGX_EX void* gpgx_get_sram(int *size)
{
	if (sram.on)
	{
		*size = saveramsize();
		return sram.sram;
	}
	else if (cdd.loaded && scd.cartridge.id)
	{
		int sz = scd.cartridge.mask + 1;
		memcpy(tempsram, scd.cartridge.area, sz);
		memcpy(tempsram + sz, scd.bram, 0x2000);
		*size = sz + 0x2000;
		return tempsram;
	}
	else if (cdd.loaded)
	{
		*size = 0x2000;
		return scd.bram;
	}
	else if (scd.cartridge.id)
	{
		*size = scd.cartridge.mask + 1;
		return scd.cartridge.area;
	}
	else
	{
		*size = 0;
		return NULL;
	}
}

GPGX_EX int gpgx_put_sram(const uint8 *data, int size)
{
	if (sram.on)
	{
		if (size != saveramsize())
			return 0;
		memcpy(sram.sram, data, size);
		return 1;
	}
	else if (cdd.loaded && scd.cartridge.id)
	{
		int sz = scd.cartridge.mask + 1;
		if (size != sz + 0x2000)
			return 0;
		memcpy(scd.cartridge.area, data, sz);
		memcpy(scd.bram, data + sz, 0x2000);
		return 1;
	}
	else if (cdd.loaded)
	{
		if (size != 0x2000)
			return 0;
		memcpy(scd.bram, data, size);
		return 1;
	}
	else if (scd.cartridge.id)
	{
		int sz = scd.cartridge.mask + 1;
		if (size != sz)
			return 0;
		memcpy(scd.cartridge.area, data, size);
		return 1;
	}
	else
	{
		if (size != 0)
			return 0;
		return 1; // "successful"?
	}
}

GPGX_EX void gpgx_poke_cram(int addr, uint8 val)
{
	write_cram_byte(addr, val);
}

GPGX_EX void gpgx_poke_vram(int addr, uint8 val)
{
	write_vram_byte(addr, val);
}

GPGX_EX void gpgx_flush_vram(void)
{
	flush_vram_cache();
}

GPGX_EX const char* gpgx_get_memdom(int which, void **area, int *size)
{
	if (!area || !size)
		return NULL;
	switch (which)
	{
	case 0:
		*area = work_ram;
		*size = 0x10000;
		return "68K RAM";
	case 1:
		*area = zram;
		*size = 0x2000;
		return "Z80 RAM";
	case 2:
		if (!cdd.loaded)
		{
			*area = ext.md_cart.rom;
			*size = ext.md_cart.romsize;
			return "MD CART";
		}
		else if (scd.cartridge.id)
		{
			*area = scd.cartridge.area;
			*size = scd.cartridge.mask + 1;
			return "EBRAM";
		}
		else return NULL;
	case 3:
		if (cdd.loaded)
		{
			*area = scd.bootrom;
			*size = 0x20000;
			return "CD BOOT ROM";
		}
		else return NULL;
	case 4:
		if (cdd.loaded)
		{
			*area = scd.prg_ram;
			*size = 0x80000;
			return "CD PRG RAM";
		}
		else return NULL;
	case 5:
		if (cdd.loaded)
		{
			*area = scd.word_ram[0];
			*size = 0x20000;
			return "CD WORD RAM[0] (1M)";
		}
		else return NULL;
	case 6:
		if (cdd.loaded)
		{
			*area = scd.word_ram[1];
			*size = 0x20000;
			return "CD WORD RAM[1] (1M)";
		}
		else return NULL;
	case 7:
		if (cdd.loaded)
		{
			*area = scd.word_ram_2M;
			*size = 0x40000;
			return "CD WORD RAM (2M)";
		}
		else return NULL;
	case 8:
		if (cdd.loaded)
		{
			*area = scd.bram;
			*size = 0x2000;
			return "CD BRAM";
		}
		else return NULL;
	case 9:
		*area = boot_rom;
		*size = 0x800;
		return "BOOT ROM";
	default:
		return NULL;
	case 10:
		if (sram.on)
		{
			*area = sram.sram;
			*size = saveramsize();
			return "SRAM";
		}
		else return NULL;
	case 11:
		*area = cram;
		*size = 128;
		return "CRAM";
	case 12:
		*area = vsram;
		*size = 128;
		return "VSRAM";
	case 13:
		*area = vram;
		*size = 65536;
		return "VRAM";
	}
}

GPGX_EX void gpgx_write_m68k_bus(unsigned addr, unsigned data)
{
	cpu_memory_map m = m68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.write8)
		m.base[addr & 0xffff ^ 1] = data;
}

GPGX_EX void gpgx_write_s68k_bus(unsigned addr, unsigned data)
{
	cpu_memory_map m = s68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.write8)
		m.base[addr & 0xffff ^ 1] = data;
}
GPGX_EX unsigned gpgx_peek_m68k_bus(unsigned addr)
{
	cpu_memory_map m = m68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.read8)
		return m.base[addr & 0xffff ^ 1];
	else
		return 0xff;
}
GPGX_EX unsigned gpgx_peek_s68k_bus(unsigned addr)
{
	cpu_memory_map m = s68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.read8)
		return m.base[addr & 0xffff ^ 1];
	else
		return 0xff;
}

struct InitSettings
{
	uint32_t BackdropColor;
	int Region;
	uint16_t LowPassRange;
	int16_t LowFreq;
	int16_t HighFreq;
	int16_t LowGain;
	int16_t MidGain;
	int16_t HighGain;
	uint8_t Filter;
	char InputSystemA;
	char InputSystemB;
	char SixButton;
	char ForceSram;
};

GPGX_EX int gpgx_init(const char* feromextension,
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
	_debug_puts("Initializing GPGX native...");

	cinterface_force_sram = settings->ForceSram;

	memset(&bitmap, 0, sizeof(bitmap));

	strncpy(romextension, feromextension, 3);
	romextension[3] = 0;

	load_archive_cb = feload_archive_cb;

	bitmap.width = 1024;
	bitmap.height = 512;
	bitmap.pitch = 1024 * 4;
	bitmap.data = alloc_invisible(2 * 1024 * 1024);
	tempsram = alloc_invisible(24 * 1024);
	bg_pattern_cache = alloc_invisible(0x80000);

	ext.md_cart.rom = alloc_plain(32 * 1024 * 1024);
	SZHVC_add = alloc_sealed(131072);
	SZHVC_sub = alloc_sealed(131072);
	ym2612_lfo_pm_table = alloc_sealed(131072);
	vdp_bp_lut = alloc_sealed(262144);
	vdp_lut = alloc_sealed(6 * sizeof(*vdp_lut));
	for (int i = 0; i < 6; i++)
		vdp_lut[i] = alloc_sealed(65536);

	/* sound options */
	config.psg_preamp  = 150;
	config.fm_preamp= 100;
	config.hq_fm = 1; /* high-quality resampling */
	config.psgBoostNoise  = 1;
	config.filter = settings->Filter; //0; /* no filter */
	config.lp_range = settings->LowPassRange; //0x9999; /* 0.6 in 16.16 fixed point */
	config.low_freq = settings->LowFreq; //880;
	config.high_freq = settings->HighFreq; //5000;
	config.lg = settings->LowGain; //100;
	config.mg = settings->MidGain; //100;
	config.hg = settings->HighGain; //100;
	config.dac_bits = 14; /* MAX DEPTH */
	config.ym2413= 2; /* AUTO */
	config.mono  = 0; /* STEREO output */

	/* system options */
	config.system = 0; /* AUTO */
	config.region_detect = settings->Region; // see loadrom.c
	config.vdp_mode = 0; /* AUTO */
	config.master_clock = 0; /* AUTO */
	config.force_dtack = 0;
	config.addr_error = 1;
	config.bios = 0;
	config.lock_on = 0;

	/* video options */
	config.overscan = 0;
	config.gg_extra = 0;
	config.ntsc = 0;
	config.render = 1;

	// set overall input system type
	// usual is MD GAMEPAD or NONE
	// TEAMPLAYER, WAYPLAY, ACTIVATOR, XEA1P, MOUSE need to be specified
	// everything else is auto or master system only
	// XEA1P is port 1 only
	// WAYPLAY is both ports at same time only
	input.system[0] = settings->InputSystemA;
	input.system[1] = settings->InputSystemB;

	cinterface_custom_backdrop_color = settings->BackdropColor;

	// apparently, the only part of config.input used is the padtype identifier,
	// and that's used only for choosing pad type when system_md
	{
		int i;
		for (i = 0; i < MAX_INPUTS; i++)
			config.input[i].padtype = settings->SixButton ? DEVICE_PAD6B : DEVICE_PAD3B;
	}

	if (!load_rom("PRIMARY_ROM"))
		return 0;

	audio_init(44100, 0);
	system_init();
	system_reset();

	update_viewport();
	gpgx_clear_sram();

	load_archive_cb = NULL; // don't hold onto load_archive_cb for longer than we need it for

	return 1;
}

GPGX_EX void gpgx_reset(int hard)
{
	if (hard)
		system_reset();
	else
		gen_reset(0);
}

GPGX_EX void gpgx_set_mem_callback(ECL_ENTRY void (*read)(unsigned), ECL_ENTRY void (*write)(unsigned), ECL_ENTRY void (*exec)(unsigned))
{
	biz_readcb = read;
	biz_writecb = write;
	biz_execcb = exec;
}

GPGX_EX void gpgx_set_cd_callback(CDCallback cdcallback)
{
	biz_cdcallback = cdcallback;
}

GPGX_EX void gpgx_set_draw_mask(int mask)
{
	cinterface_render_bga = !!(mask & 1);
	cinterface_render_bgb = !!(mask & 2);
	cinterface_render_bgw = !!(mask & 4);
	cinterface_render_obj = !!(mask & 8);
	cinterface_custom_backdrop = !!(mask & 16);
	if (cinterface_custom_backdrop)
		color_update_m5(0, 0);
	else
		color_update_m5(0x00, *(uint16 *)&cram[border << 1]);
}

GPGX_EX void gpgx_set_sprite_limit_enabled(int enabled)
{
	config.no_sprite_limit = !enabled;
}

GPGX_EX void gpgx_invalidate_pattern_cache(void)
{
	vdp_invalidate_full_cache();
}

typedef struct
{
	unsigned int value;
	const char *name;
} gpregister_t;

GPGX_EX int gpgx_getmaxnumregs(void)
{
	return 57;
}

GPGX_EX int gpgx_getregs(gpregister_t *regs)
{
	int ret = 0;

	// 22
#define MAKEREG(x) regs->name = "M68K " #x; regs->value = m68k_get_reg(M68K_REG_##x); regs++; ret++;
	MAKEREG(D0);
	MAKEREG(D1);
	MAKEREG(D2);
	MAKEREG(D3);
	MAKEREG(D4);
	MAKEREG(D5);
	MAKEREG(D6);
	MAKEREG(D7);
	MAKEREG(A0);
	MAKEREG(A1);
	MAKEREG(A2);
	MAKEREG(A3);
	MAKEREG(A4);
	MAKEREG(A5);
	MAKEREG(A6);
	MAKEREG(A7);
	MAKEREG(PC);
	MAKEREG(SR);
	MAKEREG(SP);
	MAKEREG(USP);
	MAKEREG(ISP);
	MAKEREG(IR);
#undef MAKEREG

	(regs-6)->value = biz_lastpc; // during read/write callbacks, PC runs away due to prefetch. restore it.

	// 13
#define MAKEREG(x) regs->name = "Z80 " #x; regs->value = Z80.x.d; regs++; ret++;
	MAKEREG(pc);
	MAKEREG(sp);
	MAKEREG(af);
	MAKEREG(bc);
	MAKEREG(de);
	MAKEREG(hl);
	MAKEREG(ix);
	MAKEREG(iy);
	MAKEREG(wz);
	MAKEREG(af2);
	MAKEREG(bc2);
	MAKEREG(de2);
	MAKEREG(hl2);
#undef MAKEREG

	// 22
	if (system_hw == SYSTEM_MCD)
	{
#define MAKEREG(x) regs->name = "S68K " #x; regs->value = s68k_get_reg(M68K_REG_##x); regs++; ret++;
	MAKEREG(D0);
	MAKEREG(D1);
	MAKEREG(D2);
	MAKEREG(D3);
	MAKEREG(D4);
	MAKEREG(D5);
	MAKEREG(D6);
	MAKEREG(D7);
	MAKEREG(A0);
	MAKEREG(A1);
	MAKEREG(A2);
	MAKEREG(A3);
	MAKEREG(A4);
	MAKEREG(A5);
	MAKEREG(A6);
	MAKEREG(A7);
	MAKEREG(PC);
	MAKEREG(SR);
	MAKEREG(SP);
	MAKEREG(USP);
	MAKEREG(ISP);
	MAKEREG(IR);
#undef MAKEREG
	}

	return ret;
}

// at the moment, this dummy is not called
int main(void)
{
	return 0;
}
