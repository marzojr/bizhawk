#ifndef CALLBACKS_H
#define CALLBACKS_H

#include <stdint.h>

typedef void (*snes_video_frame_t)(const uint16_t* data, int width, int height, int pitch);
typedef int16_t (*snes_input_poll_t)(int port, int index, int id);
typedef void (*snes_controller_latch_t)(void);
typedef void (*snes_no_lag_t)(bool sgb_poll);
typedef char* (*snes_path_request_t)(int slot, const char* hint, bool required);
typedef void (*snes_trace_t)(const char* disassembly, const char* register_info);
typedef void (*snes_read_hook_t)(uint32_t address, uint8_t value);
typedef void (*snes_write_hook_t)(uint32_t address, uint8_t value);
typedef void (*snes_exec_hook_t)(uint32_t address);
typedef int64_t (*snes_time_t)(void);
typedef void (*snes_msu_open_t)(uint16_t track_id);
typedef void (*snes_msu_seek_t)(long offset, bool relative);
typedef uint8_t (*snes_msu_read_t)(void);
typedef bool (*snes_msu_end_t)(void);

struct SnesCallbacks {
    snes_video_frame_t snes_video_frame;
    snes_input_poll_t snes_input_poll;
    snes_controller_latch_t snes_controller_latch;
    snes_no_lag_t snes_no_lag;
    snes_path_request_t snes_path_request;
    snes_trace_t snes_trace;
    snes_read_hook_t snes_read_hook;
    snes_write_hook_t snes_write_hook;
    snes_exec_hook_t snes_exec_hook;
    snes_time_t snes_time;
    snes_msu_open_t snes_msu_open;
    snes_msu_seek_t snes_msu_seek;
    snes_msu_read_t snes_msu_read;
    snes_msu_end_t snes_msu_end;
};

extern SnesCallbacks snesCallbacks;

#endif
