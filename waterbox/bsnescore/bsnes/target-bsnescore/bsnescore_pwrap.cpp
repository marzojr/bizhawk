//types of messages:
//cmd: frontend->core: "command to core" a command from the frontend which causes emulation to proceed. when sending a command, the frontend should wait for an eMessage_BRK_Complete before proceeding, although a debugger might proceed after any BRK
//query: frontend->core: "query to core" a query from the frontend which can (and should) be satisfied immediately by the core but which does not result in emulation processes (notably, nothing resembling a CMD and nothing which can trigger a BRK)
//sig: core->frontend: "core signal" a synchronous operation called from the emulation process which the frontend should handle immediately without issuing any calls into the core
//brk: core->frontend: "core break" the emulation process has suspended. the frontend is free to do whatever it wishes.

#define BSNESCORE_IMPORT
#include "sfc/sfc.hpp"
#include "bsnescore.hpp"
#include <emulibc.h>

#include <libco.h>

#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <string>
#include <vector>

typedef uint8 u8;
typedef uint16 u16;
typedef uint32 u32;
typedef uint64 u64;

typedef int32 s32;

typedef void(*Action)();

enum eMessage : int32
{
    eMessage_NotSet,

    eMessage_Resume,

    eMessage_QUERY_FIRST,
    eMessage_QUERY_get_memory_size,
    eMessage_QUERY_peek,
    eMessage_QUERY_poke,
    eMessage_QUERY_serialize_size,
    eMessage_QUERY_set_color_lut,
    eMessage_QUERY_GetMemoryIdName,
    eMessage_QUERY_state_hook_exec,
    eMessage_QUERY_state_hook_read,
    eMessage_QUERY_state_hook_write,
    eMessage_QUERY_state_hook_nmi,
    eMessage_QUERY_state_hook_irq,
    eMessage_QUERY_state_hook_exec_smp,
    eMessage_QUERY_state_hook_read_smp,
    eMessage_QUERY_state_hook_write_smp,
    eMessage_QUERY_enable_trace,
    eMessage_QUERY_enable_scanline,
    eMessage_QUERY_enable_audio,
    eMessage_QUERY_set_layer_enable,
    eMessage_QUERY_set_backdropColor,
    eMessage_QUERY_peek_logical_register,
    eMessage_QUERY_peek_cpu_regs,
    eMessage_QUERY_set_cdl,
    eMessage_QUERY_LAST,

    eMessage_CMD_FIRST,
    eMessage_CMD_init,
    eMessage_CMD_power,
    eMessage_CMD_reset,
    eMessage_CMD_run,
    eMessage_CMD_serialize,
    eMessage_CMD_unserialize,
    eMessage_CMD_load_cartridge_normal,
    eMessage_CMD_load_cartridge_sgb,
    eMessage_CMD_term,
    eMessage_CMD_unload_cartridge,
    eMessage_CMD_LAST,

    eMessage_SIG_video_refresh,
    eMessage_SIG_input_poll,
    eMessage_SIG_input_state,
    eMessage_SIG_no_lag,
    eMessage_SIG_audio_flush,
    eMessage_SIG_path_request,
    eMessage_SIG_trace_callback,
    eMessage_SIG_allocSharedMemory,
    eMessage_SIG_freeSharedMemory,

    eMessage_BRK_Complete,
    eMessage_BRK_hook_exec,
    eMessage_BRK_hook_read,
    eMessage_BRK_hook_write,
    eMessage_BRK_hook_nmi,
    eMessage_BRK_hook_irq,
    eMessage_BRK_hook_exec_smp,
    eMessage_BRK_hook_read_smp,
    eMessage_BRK_hook_write_smp,
    eMessage_BRK_scanlineStart,
};

enum eStatus : int32
{
    eStatus_Idle,
    eStatus_CMD,
    eStatus_BRK
};


struct CPURegsComm {
    u32 pc;
    u16 a, x, y, s, d, vector; //6x
    u8 p, db, nothing, nothing2;
    u16 v, h;
};
static_assert(sizeof(CPURegsComm) == 24);

//TODO: do any of these need to be volatile?
// tOdO bTw
struct CommStruct
{
    //the cmd being executed
    eMessage cmd;

    //the status of the core
    eStatus status;

    //the SIG or BRK that the core is halted in
    eMessage reason;

    int32 padding1;

    //flexible in/out parameters
    //these are all "overloaded" a little so it isn't clear what's used for what in for any particular message..
    //but I think it will beat having to have some kind of extremely verbose custom layouts for every message
    char* str;
    void* ptr;
    uint32 id, addr, value, size;
    int32 port, device, index, slot;
    int32 width, height;
    int32 scanline;
    SuperFamicom::ID::Device inports[2];

    int32 padding2;

    //always used in pairs
    void* buf[3];
    int32 buf_size[3];

    int32 padding3;

    int64 cdl_ptr[16];
    int32 cdl_size[16];

    CPURegsComm cpuregs;
    LayerEnablesComm layerEnables;

    //static configuration-type information which can be grabbed off the core at any time without even needing a QUERY command
    uint32 region;
    uint32 mapper;
    uint32 BLANKO;

    //===========================================================

    //private stuff
    void* privbuf[3]; //TODO remember to tidy this.. gj

    void CopyBuffer(int id, void* ptr, int32 size)
    {
        if (privbuf[id]) free(privbuf[id]);
        buf[id] = privbuf[id] = malloc(size);
        memcpy(buf[id], ptr, size);
        buf_size[id] = size;
    }

    void SetBuffer(int id, void* ptr, int32 size)
    {
        if (privbuf[id]) free(privbuf[id]);
        privbuf[id] = nullptr;
        buf[id] = ptr;
        buf_size[id] = size;
    }


} comm;

//coroutines
cothread_t co_control, co_emu;

Action CMD_cb;

void BREAK(eMessage msg)
{
    comm.status = eStatus_BRK;
    comm.reason = msg;
    // setting this is necessary for some reason; I believe bsnes uses own cothreads which may switch from time to time
    co_emu = co_active();
    co_switch(co_control);
    comm.status = eStatus_CMD;
}

// void snes_scanlineStart(int line)
// {
//     comm.scanline = line;
//     BREAK(eMessage_BRK_scanlineStart);
// }

// void* snes_allocSharedMemory(const char* memtype, size_t amt)
// {
//     //its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
//     //if(!running) return NULL;

//     void* ret;

//     ret = alloc_plain(amt);

//     comm.str = (char*)memtype;
//     comm.size = amt;
//     comm.ptr = ret;

//     BREAK(eMessage_SIG_allocSharedMemory);

//     return comm.ptr;
// }

// void snes_freeSharedMemory(void* ptr)
// {
//     //its important that this happen before the message marshaling because allocation/free attempts can happen before the marshaling is setup (or at shutdown time, in case of errors?)
//     //if(!running) return;

//     if (!ptr) return;

//     comm.ptr = ptr;

//     BREAK(eMessage_SIG_freeSharedMemory);
// }

static void debug_op_exec(uint24 addr)
{
    comm.addr = addr;
    BREAK(eMessage_BRK_hook_exec);
}

static void debug_op_read(uint24 addr)
{
    comm.addr = addr;
    BREAK(eMessage_BRK_hook_read);
}

static void debug_op_write(uint24 addr, uint8 value)
{
    comm.addr = addr;
    comm.value = value;
    BREAK(eMessage_BRK_hook_write);
}

static void debug_op_nmi()
{
    BREAK(eMessage_BRK_hook_nmi);
}

static void debug_op_irq()
{
    BREAK(eMessage_BRK_hook_irq);
}

static void debug_op_exec_smp(uint24 addr)
{
    comm.addr = addr;
    BREAK(eMessage_BRK_hook_exec_smp);
}

static void debug_op_read_smp(uint24 addr)
{
    comm.addr = addr;
    BREAK(eMessage_BRK_hook_read_smp);
}

static void debug_op_write_smp(uint24 addr, uint8 value)
{
    comm.addr = addr;
    comm.value = value;
    BREAK(eMessage_BRK_hook_write_smp);
}


static void Analyze()
{
    //gather some "static" type information, so we dont have to poll annoyingly for it later
    comm.mapper = snes_get_mapper();
    comm.region = snes_get_region();
}

void CMD_LoadCartridgeNormal()
{
    bool ret = snes_load_cartridge_normal((const char*) comm.buf[0], (const uint8_t*)comm.buf[1], comm.buf_size[1]);
    comm.value = ret;

    if(ret)
        Analyze();
}

void CMD_LoadCartridgeSGB()
{
    bool ret = snes_load_cartridge_super_game_boy((const char*)comm.buf[0], (const uint8_t*)comm.buf[1], comm.buf_size[1], (const uint8_t*)comm.buf[2], comm.buf_size[2]);
    comm.value = ret;

    if(ret)
        Analyze();
}

void CMD_init()
{
    snes_init(comm.value);

    SuperFamicom::controllerPort1.connect(*(uint*) &comm.inports[0]);
    SuperFamicom::controllerPort2.connect(*(uint*) &comm.inports[1]);
}

static void CMD_Run()
{
    snes_run();
}

// void QUERY_state_hook_exec() {
//     // SuperFamicom::cpu.debugger.op_exec = comm.value ? debug_op_exec : hook<void(uint24)>();
// }
// void QUERY_state_hook_read() {
//     // SuperFamicom::cpu.debugger.op_read = comm.value ? debug_op_read : hook<void(uint24)>();
// }
// void QUERY_state_hook_write() {
//     // SuperFamicom::cpu.debugger.op_write = comm.value ? debug_op_write : hook<void(uint24, uint8)>();
// }
// void QUERY_state_hook_nmi() {
//     // SuperFamicom::cpu.debugger.op_nmi = comm.value ? debug_op_nmi : hook<void()>();
// }
// void QUERY_state_hook_irq() {
//     // SuperFamicom::cpu.debugger.op_irq = comm.value ? debug_op_irq : hook<void()>();
// }
// void QUERY_state_hook_exec_smp() {
//     // SuperFamicom::smp.debugger.op_exec = comm.value ? debug_op_exec_smp : hook<void(uint24)>();
// }
// void QUERY_state_hook_read_smp() {
//     // SuperFamicom::smp.debugger.op_read = comm.value ? debug_op_read_smp : hook<void(uint24)>();
// }
// void QUERY_state_hook_write_smp() {
//     // SuperFamicom::smp.deb
//     // SuperFamicom::smp.debugger.op_write = comm.value ? debug_op_write_smp : hook<void(uint24, uint8)>();
// }
// void QUERY_peek_cpu_regs() {
//     // comm.cpuregs.pc = SuperFamicom::cpu.p
//     // comm.cpuregs.pc = (u32)SuperFamicom::cpu.regs.pc;
//     // comm.cpuregs.a = SuperFamicom::cpu.regs.a;
//     // comm.cpuregs.x = SuperFamicom::cpu.regs.x;
//     // comm.cpuregs.y = SuperFamicom::cpu.regs.y;
//     // comm.cpuregs.s = SuperFamicom::cpu.regs.s;
//     // comm.cpuregs.d = SuperFamicom::cpu.regs.d;
//     // comm.cpuregs.db = SuperFamicom::cpu.regs.db;
//     // comm.cpuregs.vector = SuperFamicom::cpu.regs.vector;
//     // comm.cpuregs.p = SuperFamicom::cpu.regs.p;
//     comm.cpuregs.nothing = 0;
//     comm.cpuregs.nothing2 = 0;
//     comm.cpuregs.v = SuperFamicom::cpu.vcounter();
//     comm.cpuregs.h = SuperFamicom::cpu.hdot();
// }
// void QUERY_peek_set_cdl() {
//     for (int i = 0; i<16; i++)
//     {
//         // cdlInfo.blocks[i] = (uint8*)comm.cdl_ptr[i];
//         // cdlInfo.blockSizes[i] = comm.cdl_size[i];
//     }
// }

const Action kHandlers_CMD[] = {
    CMD_init,
    snes_power,
    snes_reset,
    CMD_Run,
    nullptr,
    nullptr,
    CMD_LoadCartridgeNormal,
    CMD_LoadCartridgeSGB,
    snes_term,
    snes_unload_cartridge,
};

//all this does is run commands on the emulation thread infinitely forever
//(I should probably make a mechanism for bailing...)
void new_emuthread()
{
    for (;;)
    {
        //process the current CMD
        CMD_cb();

        //when that returned, we're definitely done with the CMD--so we're now IDLE
        comm.status = eStatus_Idle;

        co_switch(co_control);
    }
}

//------------------------------------------------
//DLL INTERFACE

EXPORT void* DllInit()
{
    #define T(s,n) static_assert(offsetof(CommStruct,s)==n,#n)
    T(cmd, 0);
    T(status, 4);
    T(reason, 8);
    T(str, 16);
    T(ptr, 24);
    T(id, 32);
    T(port, 48);
    T(width, 64);
    T(scanline, 72);
    T(inports, 76);
    T(buf, 88);
    T(buf_size, 112);
    T(cdl_ptr, 128);
    T(cdl_size, 256);
    T(cpuregs, 320);
    T(layerEnables, 344);
    T(region, 356);
    T(mapper, 360);
    T(BLANKO, 364);
    // start of private stuff
    T(privbuf, 368);
    #undef T

    memset(&comm, 0, sizeof(comm));

    fprintf(stderr, "THIS DLLINIT FUNCTION WAS CALLED!!!\n\n\n");

    //make a coroutine thread to run the emulation in. we'll switch back to this cothread when communicating with the frontend
    co_control = co_active();
    if (co_emu)
    {
        // if this was called again, that's OK; delete the old emuthread
        co_delete(co_emu);
        co_emu = nullptr;
    }
    co_emu = co_create(32768 * sizeof(void*), new_emuthread);

    return &comm;
}

EXPORT void Message(eMessage msg)
{
    switch (msg)
    {
        case eMessage_Resume: {
            co_switch(co_emu);
            break;
        }
        case eMessage_QUERY_get_memory_size: {
            comm.value = snes_get_memory_size(comm.value);
            break;
        }
        case eMessage_QUERY_peek: {
            if (comm.id == SNES_MEMORY_SYSBUS)
                comm.value = bus_read(comm.addr);
            else comm.value = snes_get_memory_data(comm.id)[comm.addr];
            break;
        }
        case eMessage_QUERY_poke: {
            if (comm.id == SNES_MEMORY_SYSBUS)
                bus_write(comm.addr, comm.value);
            else
                snes_write_memory_data(comm.id, comm.addr, comm.value);
            break;
        }
        case eMessage_QUERY_serialize_size: {
            // was never implemented?
            break;
        }
        case eMessage_QUERY_GetMemoryIdName: {
            comm.str = (char*) snes_get_memory_id_name(comm.id);
            break;
        }
        case eMessage_QUERY_state_hook_exec: {

        }
        case eMessage_QUERY_state_hook_read: {

        }
        case eMessage_QUERY_state_hook_write: {

        }
        case eMessage_QUERY_state_hook_nmi: {

        }
        case eMessage_QUERY_state_hook_irq: {

        }
        case eMessage_QUERY_state_hook_exec_smp: {

        }
        case eMessage_QUERY_state_hook_read_smp: {

        }
        case eMessage_QUERY_state_hook_write_smp: {
            break;
        }
        case eMessage_QUERY_enable_trace: {
            // snes_set_trace_callback(comm.value, snes_trace);
            break;
        }
        case eMessage_QUERY_enable_scanline: {
            // if (comm.value)
                // snes_set_scanlineStart(snes_scanlineStart);
            // else snes_set_scanlineStart(nullptr);
            break;
        }
        case eMessage_QUERY_enable_audio: {
            // audio_enabled = comm.value;
            break;
        }
        case eMessage_QUERY_set_layer_enable: {
            break;
        }
        case eMessage_QUERY_set_backdropColor: {
            backdropColor = comm.value;
        }
        case eMessage_QUERY_peek_logical_register: {
            comm.value = snes_peek_logical_register(comm.id);
            break;
        }
        case eMessage_QUERY_peek_cpu_regs: {

        }
        case eMessage_QUERY_set_cdl: {
            break;
        }
        case eMessage_CMD_init ... eMessage_CMD_unload_cartridge:
            //CMD is only valid if status is idle
            if (comm.status != eStatus_Idle) {
                fprintf(stderr, "ERROR: cmd during non-idle\n");
                return;
            }

            comm.status = eStatus_CMD;
            comm.cmd = msg;

            CMD_cb = kHandlers_CMD[msg - eMessage_CMD_FIRST - 1];
            co_switch(co_emu);

    //     // nested switch to allow separating the CMDs
    // switch (msg)
    // {
    //     case eMessage_CMD_init:
    //         fprintf(stderr, "message was exactly init: %d\n", msg);
    //         break;
    // }

            // we could be in ANY STATE when we return from here
    }
}


//receives the given buffer and COPIES it. use this for returning values from SIGs
EXPORT void CopyBuffer(int id, void* ptr, int32 size)
{
    comm.CopyBuffer(id, ptr, size);
}

//receives the given buffer and STASHES IT. use this (carefully) for sending params for CMDs
EXPORT void SetBuffer(int id, void* ptr, int32 size)
{
    comm.SetBuffer(id, ptr, size);
}

EXPORT void PostLoadState()
{
    // SuperFamicom::ppu.
    // SuperFamicom::ppu.flush_tiledata_cache();
}

int main()
{
    return 0;
}
