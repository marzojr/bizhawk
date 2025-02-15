#include <stdlib.h>
#include <string.h>
#include <gb.h>

#include "blip_buf.h"

#ifdef _WIN32
	#define EXPORT __declspec(dllexport)
#else
	#define EXPORT __attribute__((visibility("default")))
#endif

typedef int8_t s8;
typedef int16_t s16;
typedef int32_t s32;
typedef int64_t s64;

typedef uint8_t u8;
typedef uint16_t u16;
typedef uint32_t u32;
typedef uint64_t u64;

typedef void (*input_callback_t)(void);
typedef void (*rumble_callback_t)(u32);
typedef void (*trace_callback_t)(u16);
typedef void (*memory_callback_t)(u16, u8);
typedef void (*printer_callback_t)(u32*, u8, u8, u8, u8);
typedef void (*scanline_callback_t)(u32);

typedef struct
{
	GB_gameboy_t gb;
	blip_t* blip_l;
	blip_t* blip_r;
	GB_sample_t sampleLatch;
	u32 nsamps;
	u32 vbuf[256 * 224];
	u32 bg_pal[0x20];
	u32 obj_pal[0x20];
	GB_palette_t custom_pal;
	input_callback_t input_cb;
	rumble_callback_t rumble_cb;
	trace_callback_t trace_cb;
	memory_callback_t read_cb;
	memory_callback_t write_cb;
	memory_callback_t exec_cb;
	printer_callback_t printer_cb;
	scanline_callback_t scanline_cb;
	u32 scanline_sl;
	bool vblank_occurred;
	bool new_frame_present;
	u64 cc;
} biz_t;

static inline u8 PeekIO(biz_t* biz, u8 addr)
{
	u8* io = GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_IO, NULL, NULL);
	return io[addr];
}

static void sample_cb(GB_gameboy_t* gb, GB_sample_t* sample)
{
	biz_t* biz = (biz_t*)gb;

	if (biz->sampleLatch.left != sample->left)
	{
		blip_add_delta(biz->blip_l, biz->nsamps, biz->sampleLatch.left - sample->left);
		biz->sampleLatch.left = sample->left;
	}

	if (biz->sampleLatch.right != sample->right)
	{
		blip_add_delta(biz->blip_r, biz->nsamps, biz->sampleLatch.right - sample->right);
		biz->sampleLatch.right = sample->right;
	}

	biz->nsamps++;
}

static u32 rgb_cb(GB_gameboy_t* gb, u8 r, u8 g, u8 b)
{
    return (0xFF << 24) | (r << 16) | (g << 8) | b;
}

static void vblank_cb(GB_gameboy_t* gb, GB_vblank_type_t type)
{
	biz_t* biz = (biz_t*)gb;
	biz->vblank_occurred = true;
	if (type != GB_VBLANK_TYPE_REPEAT)
	{
		biz->new_frame_present = true;
	}
}

static u8 camera_pixel_cb(GB_gameboy_t* gb, u8 x, u8 y)
{
	// stub for now (also needed for determinism)
	return 0;
}

static void RumbleCallbackRelay(GB_gameboy_t* gb, double rumble_amplitude)
{
	((biz_t*)gb)->rumble_cb(INT32_MAX * rumble_amplitude);
}

static u8 ReadCallbackRelay(GB_gameboy_t* gb, u16 addr, u8 data)
{
	((biz_t*)gb)->read_cb(addr, 0);
	return data;
}

static bool WriteCallbackRelay(GB_gameboy_t* gb, u16 addr, u8 data)
{
	((biz_t*)gb)->write_cb(addr, data);
	return true;
}

static void ExecCallbackRelay(GB_gameboy_t* gb, u16 addr, u8 opcode)
{
	biz_t* biz = (biz_t*)gb;
	if (biz->trace_cb)
	{
		biz->trace_cb(addr);
	}
	if (biz->exec_cb)
	{
		biz->exec_cb(addr, 0);
	}
}

static void PrinterCallbackRelay(GB_gameboy_t* gb, u32* image, u8 height, u8 top_margin, u8 bottom_margin, u8 exposure)
{
	((biz_t*)gb)->printer_cb(image, height, top_margin, bottom_margin, exposure);
}

static void PrinterDoneCallbackRelay(GB_gameboy_t* gb)
{
	// nothing to do
}

static void ScanlineCallbackRelay(GB_gameboy_t* gb, u8 line)
{
	biz_t* biz = (biz_t*)gb;
	if (line == biz->scanline_sl)
	{
		biz->scanline_cb(PeekIO(biz, GB_IO_LCDC));
	}
}

EXPORT biz_t* sameboy_create(u8* romdata, u32 romlen, u8* biosdata, u32 bioslen, GB_model_t model, bool realtime, bool nobounce)
{
	biz_t* biz = calloc(1, sizeof (biz_t));
	GB_random_seed(0);
	GB_init(&biz->gb, model);
	// this will be NULL if a GBS is going to be loaded later
	if (romdata)
	{
		GB_load_rom_from_buffer(&biz->gb, romdata, romlen);
	}
	GB_load_boot_rom_from_buffer(&biz->gb, biosdata, bioslen);
	GB_set_sample_rate(&biz->gb, GB_get_clock_rate(&biz->gb) / 2 / 8);
	GB_set_rumble_mode(&biz->gb, GB_RUMBLE_ALL_GAMES);
	GB_set_rumble_callback(&biz->gb, RumbleCallbackRelay);
	GB_apu_set_sample_callback(&biz->gb, sample_cb);
	GB_set_rgb_encode_callback(&biz->gb, rgb_cb);
	GB_set_vblank_callback(&biz->gb, vblank_cb);
	GB_set_camera_get_pixel_callback(&biz->gb, camera_pixel_cb);
	GB_set_pixels_output(&biz->gb, biz->vbuf);
	GB_set_rtc_mode(&biz->gb, realtime ? GB_RTC_MODE_SYNC_TO_HOST : GB_RTC_MODE_ACCURATE);
	GB_set_emulate_joypad_bouncing(&biz->gb, !nobounce);
	GB_set_allow_illegal_inputs(&biz->gb, true);
	biz->blip_l = blip_new(1024);
	biz->blip_r = blip_new(1024);
	blip_set_rates(biz->blip_l, GB_get_clock_rate(&biz->gb) / 2 / 8, 44100);
	blip_set_rates(biz->blip_r, GB_get_clock_rate(&biz->gb) / 2 / 8, 44100);
	return biz;
}

EXPORT void sameboy_destroy(biz_t* biz)
{
	GB_free(&biz->gb);
	blip_delete(biz->blip_l);
	blip_delete(biz->blip_r);
	free(biz);
}

EXPORT bool sameboy_loadgbs(biz_t* biz, u8* gbs, u32 gbslen, GB_gbs_info_t* gbsinfo)
{
	return GB_load_gbs_from_buffer(&biz->gb, gbs, gbslen, gbsinfo) == 0;
}

EXPORT void sameboy_switchgbstrack(biz_t* biz, u32 track)
{
	GB_gbs_switch_track(&biz->gb, track);
}

EXPORT void sameboy_setinputcallback(biz_t* biz, input_callback_t callback)
{
	biz->input_cb = callback;
}

EXPORT void sameboy_setrumblecallback(biz_t* biz, rumble_callback_t callback)
{
	biz->rumble_cb = callback;
}

static inline double FromRawToG(u16 raw)
{
	return (raw - 0x81D0) / (0x70 * 1.0);
}

EXPORT void sameboy_frameadvance(biz_t* biz, GB_key_mask_t keys, u16 x, u16 y, s16* sbuf, u32* nsamp, u32* vbuf, bool render, bool border)
{
	GB_set_key_mask(&biz->gb, keys);
	if (GB_has_accelerometer(&biz->gb))
	{
		GB_set_accelerometer_values(&biz->gb, FromRawToG(x), FromRawToG(y));
	}
	GB_set_border_mode(&biz->gb, border ? GB_BORDER_ALWAYS : GB_BORDER_NEVER);
	GB_set_rendering_disabled(&biz->gb, !render);

	// todo: switch this hack over to joyp_accessed when upstream fixes problems with it
	if ((PeekIO(biz, GB_IO_JOYP) & 0x30) != 0x30)
	{
		biz->input_cb();
	}

	u32 cycles = 0;
	biz->vblank_occurred = false;
	biz->new_frame_present = false;

	do
	{
		u8 oldjoyp = PeekIO(biz, GB_IO_JOYP) & 0x30;
		u32 ret = GB_run(&biz->gb) >> 2;
		cycles += ret;
		biz->cc += ret;
		u8 newjoyp = PeekIO(biz, GB_IO_JOYP) & 0x30;
		if (oldjoyp != newjoyp && newjoyp != 0x30)
		{
			biz->input_cb();
		}
	}
	while (!biz->vblank_occurred && cycles < 35112);

	blip_end_frame(biz->blip_l, biz->nsamps);
	blip_end_frame(biz->blip_r, biz->nsamps);
	biz->nsamps = 0;

	u32 samps = blip_samples_avail(biz->blip_l);
	blip_read_samples(biz->blip_l, sbuf + 0, samps, 1);
	blip_read_samples(biz->blip_r, sbuf + 1, samps, 1);
	*nsamp = samps;

	if (biz->new_frame_present && render)
	{
		memcpy(vbuf, biz->vbuf, sizeof biz->vbuf);
	}
}

EXPORT void sameboy_reset(biz_t* biz)
{
	GB_random_seed(0);
	GB_reset(&biz->gb);
}

EXPORT bool sameboy_iscgbdmg(biz_t* biz)
{
	return !GB_is_cgb_in_cgb_mode(&biz->gb);
}

EXPORT void sameboy_savesram(biz_t* biz, u8* dest)
{
	GB_save_battery_to_buffer(&biz->gb, dest, GB_save_battery_size(&biz->gb));
}

EXPORT void sameboy_loadsram(biz_t* biz, u8* data, u32 len)
{
	GB_load_battery_from_buffer(&biz->gb, data, len);
}

EXPORT u32 sameboy_sramlen(biz_t* biz)
{
	return GB_save_battery_size(&biz->gb);
}

EXPORT void sameboy_savestate(biz_t* biz, u8* data)
{
	GB_save_state_to_buffer(&biz->gb, data);
}

EXPORT u32 sameboy_loadstate(biz_t* biz, u8* data, u32 len)
{
	return GB_load_state_from_buffer(&biz->gb, data, len);
}

EXPORT u32 sameboy_statelen(biz_t* biz)
{
	return GB_get_save_state_size(&biz->gb);
}

static void UpdatePal(biz_t* biz, bool bg)
{
	u32* pal = bg ? biz->bg_pal : biz->obj_pal;
	if (GB_is_cgb_in_cgb_mode(&biz->gb))
	{
		u16* rawPal = GB_get_direct_access(&biz->gb, bg ? GB_DIRECT_ACCESS_BGP : GB_DIRECT_ACCESS_OBP, NULL, NULL);
		for (u32 i = 0; i < 0x20; i++)
		{
			pal[i] = GB_convert_rgb15(&biz->gb, rawPal[i] & 0x7FFF, false);
		}
	}
	else
	{
		if (bg)
		{
			u32 bgPal[4];
			if (GB_is_cgb(&biz->gb))
			{
				u16* rawPal = GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_BGP, NULL, NULL);
				for (u32 i = 0; i < 4; i++)
				{
					bgPal[i] = GB_convert_rgb15(&biz->gb, rawPal[i] & 0x7FFF, false);
				}
			}
			else
			{
				const GB_palette_t* rawPal = GB_get_palette(&biz->gb);
				for (u32 i = 0; i < 4; i++)
				{
					bgPal[3 - i] = rgb_cb(&biz->gb, rawPal->colors[i].r, rawPal->colors[i].g, rawPal->colors[i].b);
				}
			}
			u8 bgp = PeekIO(biz, GB_IO_BGP);
			for (u32 i = 0; i < 4; i++)
			{
				pal[i] = bgPal[(bgp >> (i * 2)) & 3];
			}
			for (u32 i = 4; i < 0x20; i++)
			{
				pal[i] = GB_convert_rgb15(&biz->gb, 0x7FFF, false);
			}
		}
		else
		{
			u32 obj0Pal[4];
			u32 obj1Pal[4];
			if (GB_is_cgb(&biz->gb))
			{
				u16* rawPal = GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_OBP, NULL, NULL);
				for (u32 i = 0; i < 4; i++)
				{
					obj0Pal[i] = GB_convert_rgb15(&biz->gb, rawPal[i + 0] & 0x7FFF, false);
					obj1Pal[i] = GB_convert_rgb15(&biz->gb, rawPal[i + 4] & 0x7FFF, false);
				}
			}
			else
			{
				const GB_palette_t* rawPal = GB_get_palette(&biz->gb);
				for (u32 i = 0; i < 4; i++)
				{
					obj0Pal[3 - i] = rgb_cb(&biz->gb, rawPal->colors[i].r, rawPal->colors[i].g, rawPal->colors[i].b);
					obj1Pal[3 - i] = rgb_cb(&biz->gb, rawPal->colors[i].r, rawPal->colors[i].g, rawPal->colors[i].b);
				}
			}
			u8 obp0 = PeekIO(biz, GB_IO_OBP0);
			u8 obp1 = PeekIO(biz, GB_IO_OBP1);
			for (u32 i = 0; i < 4; i++)
			{
				pal[i + 0] = obj0Pal[(obp0 >> (i * 2)) & 3];
				pal[i + 4] = obj1Pal[(obp1 >> (i * 2)) & 3];
			}
			for (u32 i = 8; i < 0x20; i++)
			{
				pal[i] = GB_convert_rgb15(&biz->gb, 0x7FFF, false);
			}
		}
	}
}

EXPORT bool sameboy_getmemoryarea(biz_t* biz, GB_direct_access_t which, void** data, size_t* len)
{
	if (which == GB_DIRECT_ACCESS_IE + 1)
	{
		UpdatePal(biz, true);
		*data = biz->bg_pal;
		*len = sizeof biz->bg_pal;
		return true;
	}
	else if (which == GB_DIRECT_ACCESS_IE + 2)
	{
		UpdatePal(biz, false);
		*data = biz->obj_pal;
		*len = sizeof biz->obj_pal;
		return true;
	}

	if (which > GB_DIRECT_ACCESS_IE || which < GB_DIRECT_ACCESS_ROM)
	{
		return false;
	}

	*data = GB_get_direct_access(&biz->gb, which, len, NULL);
	return true;
}

EXPORT u8 sameboy_cpuread(biz_t* biz, u16 addr)
{
	GB_set_read_memory_callback(&biz->gb, NULL);
	u8 ret = GB_safe_read_memory(&biz->gb, addr);
	GB_set_read_memory_callback(&biz->gb, biz->read_cb ? ReadCallbackRelay : NULL);
	return ret;
}

EXPORT void sameboy_cpuwrite(biz_t* biz, u16 addr, u8 value)
{
	GB_set_write_memory_callback(&biz->gb, NULL);
	GB_write_memory(&biz->gb, addr, value);
	GB_set_write_memory_callback(&biz->gb, biz->write_cb ? WriteCallbackRelay : NULL);
}

EXPORT u64 sameboy_getcyclecount(biz_t* biz)
{
	return biz->cc;
}

EXPORT void sameboy_setcyclecount(biz_t* biz, u64 newCc)
{
	biz->cc = newCc;
}

EXPORT void sameboy_settracecallback(biz_t* biz, trace_callback_t callback)
{
	biz->trace_cb = callback;
	GB_set_execution_callback(&biz->gb, (callback || biz->exec_cb) ? ExecCallbackRelay : NULL);
}

EXPORT void sameboy_getregs(biz_t* biz, u32* buf)
{
	GB_registers_t* regs = GB_get_registers(&biz->gb);
	buf[0] = regs->pc & 0xFFFF;
	buf[1] = regs->a & 0xFF;
	buf[2] = regs->f & 0xFF;
	buf[3] = regs->b & 0xFF;
	buf[4] = regs->c & 0xFF;
	buf[5] = regs->d & 0xFF;
	buf[6] = regs->e & 0xFF;
	buf[7] = regs->h & 0xFF;
	buf[8] = regs->l & 0xFF;
	buf[9] = regs->sp & 0xFFFF;
}

EXPORT void sameboy_setreg(biz_t* biz, u32 which, u32 value)
{
	GB_registers_t* regs = GB_get_registers(&biz->gb);
	switch (which)
	{
		case 0:
			regs->pc = value & 0xFFFF;
			break;
		case 1:
			regs->a = value & 0xFF;
			break;
		case 2:
			regs->f = value & 0xFF;
			break;
		case 3:
			regs->b = value & 0xFF;
			break;
		case 4:
			regs->c = value & 0xFF;
			break;
		case 5:
			regs->d = value & 0xFF;
			break;
		case 6:
			regs->e = value & 0xFF;
			break;
		case 7:
			regs->h = value & 0xFF;
			break;
		case 8:
			regs->l = value & 0xFF;
			break;
		case 9:
			regs->sp = value & 0xFFFF;
			break;
	}
}

EXPORT void sameboy_getbankregs(biz_t* biz, u32* buf)
{
	size_t size;
	uint16_t bank;
	GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_ROM0, &size, &bank);
	buf[0] = bank;
	GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_ROM, &size, &bank);
	buf[1] = bank;
	GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_RAM, &size, &bank);
	buf[2] = bank;
	GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_CART_RAM, &size, &bank);
	buf[3] = bank;
	GB_get_direct_access(&biz->gb, GB_DIRECT_ACCESS_VRAM, &size, &bank);
	buf[4] = bank;
}

EXPORT void sameboy_setmemorycallback(biz_t* biz, u32 which, memory_callback_t callback)
{
	switch (which)
	{
		case 0:
			biz->read_cb = callback;
			GB_set_read_memory_callback(&biz->gb, callback ? ReadCallbackRelay : NULL);
			break;
		case 1:
			biz->write_cb = callback;
			GB_set_write_memory_callback(&biz->gb, callback ? WriteCallbackRelay : NULL);
			break;
		case 2:
			biz->exec_cb = callback;
			GB_set_execution_callback(&biz->gb, (callback || biz->trace_cb) ? ExecCallbackRelay : NULL);
			break;
	}
}

EXPORT void sameboy_setprintercallback(biz_t* biz, printer_callback_t callback)
{
	biz->printer_cb = callback;
	if (callback)
	{
		GB_connect_printer(&biz->gb, PrinterCallbackRelay, PrinterDoneCallbackRelay);
	}
	else
	{
		GB_disconnect_serial(&biz->gb);
	}
}

EXPORT void sameboy_setscanlinecallback(biz_t* biz, scanline_callback_t callback, u32 sl)
{
	biz->scanline_cb = callback;
	biz->scanline_sl = sl;
	GB_set_lcd_line_callback(&biz->gb, callback ? ScanlineCallbackRelay : NULL);
}

EXPORT void sameboy_setrtcdivisoroffset(biz_t* biz, int offset)
{
	double base = GB_get_unmultiplied_clock_rate(&biz->gb) * 2.0;
	GB_set_rtc_multiplier(&biz->gb, (base + offset) / base);
}

typedef struct
{
	u32 palette;
	u32 custom_palette[5];
	GB_color_correction_mode_t color_correction_mode;
	s32 light_temperature;
	GB_highpass_mode_t highpass_filter;
	s32 interference_volume;
	u32 channel_mask;
	bool background_enabled;
	bool objects_enabled;
} settings_t;

static inline struct GB_color_s argb_to_rgb(u32 argb)
{
	struct GB_color_s ret;
	ret.r = argb >> 16 & 0xFF;
	ret.g = argb >>  8 & 0xFF;
	ret.b = argb >>  0 & 0xFF;
	return ret;
}

EXPORT void sameboy_setsettings(biz_t* biz, settings_t* settings)
{
	for (u32 i = 0; i < 4; i++)
	{
		biz->custom_pal.colors[3 - i] = argb_to_rgb(settings->custom_palette[i]);
	}

	biz->custom_pal.colors[4] = argb_to_rgb(settings->custom_palette[4]);

	switch (settings->palette)
	{
		case 0:
			GB_set_palette(&biz->gb, &GB_PALETTE_GREY);
			break;
		case 1:
			GB_set_palette(&biz->gb, &GB_PALETTE_DMG);
			break;
		case 2:
			GB_set_palette(&biz->gb, &GB_PALETTE_MGB);
			break;
		case 3:
			GB_set_palette(&biz->gb, &GB_PALETTE_GBL);
			break;
		case 4:
			GB_set_palette(&biz->gb, &biz->custom_pal);
			break;
	}

	for (u32 i = 0; i < GB_N_CHANNELS; i++)
	{
		GB_set_channel_muted(&biz->gb, i, !(settings->channel_mask >> i & 1));
	}

	GB_set_color_correction_mode(&biz->gb, settings->color_correction_mode);
	GB_set_light_temperature(&biz->gb, settings->light_temperature / 10.0);
	GB_set_highpass_filter_mode(&biz->gb, settings->highpass_filter);
	GB_set_interference_volume(&biz->gb, settings->interference_volume / 100.0);
	GB_set_background_rendering_disabled(&biz->gb, !settings->background_enabled);
	GB_set_object_rendering_disabled(&biz->gb, !settings->objects_enabled);
}
