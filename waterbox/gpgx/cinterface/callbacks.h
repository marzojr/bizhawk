#ifndef CALLBACKS_H
#define CALLBACKS_H

#include <emulibc.h>

#include "types.h"

typedef ECL_ENTRY void (*CDCallback)(int32 addr, int32 addrtype, int32 flags);

extern ECL_ENTRY void (*biz_execcb)(unsigned addr);
extern ECL_ENTRY void (*biz_readcb)(unsigned addr, unsigned int value);
extern ECL_ENTRY void (*biz_writecb)(unsigned addr);
extern CDCallback biz_cdcallback;
extern unsigned biz_lastpc;

extern ECL_ENTRY void (*cdd_readcallback)(int lba, void *dest, int audio);

enum eCDLog_AddrType
{
	eCDLog_AddrType_MDCART, eCDLog_AddrType_RAM68k, eCDLog_AddrType_RAMZ80, eCDLog_AddrType_SRAM,
};

enum eCDLog_Flags
{
	eCDLog_Flags_Exec68k = 0x01,
	eCDLog_Flags_Data68k = 0x04,
	eCDLog_Flags_ExecZ80First = 0x08,
	eCDLog_Flags_ExecZ80Operand = 0x10,
	eCDLog_Flags_DataZ80 = 0x20,
	eCDLog_Flags_DMASource = 0x40
};

#endif
