﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA Sunsoft-2 chip (SUNSOFT-3 pcb)
	//game=Tenka no Goikenban: Mito Koumon ; chip=sunsoft-2 ; pcb = SUNSOFT-3
	//this is confusing. see docs/sunsoft.txt
	internal sealed class Mapper89 : NesBoardBase
	{
		private int chr;
		private int prg_bank_mask_16k;
		private byte prg_bank_16k;
		private byte[] prg_banks_16k = new byte[2];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER089":
					break;
				case "SUNSOFT-2":
					if (Cart.Pcb != "SUNSOFT-3") return false;
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.PadH, Cart.PadV);
			prg_bank_mask_16k = (Cart.PrgSize / 16) - 1;
			prg_banks_16k[1] = 0xFF;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg_bank_mask_16k), ref prg_bank_mask_16k);
			ser.Sync(nameof(prg_bank_16k), ref prg_bank_16k);
			ser.Sync(nameof(prg_banks_16k), ref prg_banks_16k, false);
		}

		private void SyncPRG() => prg_banks_16k[0] = prg_bank_16k;

		public override void WritePrg(int addr, byte value)
		{
			prg_bank_16k = (byte)((value >> 4) & 7);
			SyncPRG();

			if (value.Bit(3) == false)
				SetMirrorType(EMirrorType.OneScreenA);
			else
				SetMirrorType(EMirrorType.OneScreenB);

			chr = ((value & 0x07) + ((value >> 7) * 0x08));
		}

		public override byte ReadPrg(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			bank_16k &= prg_bank_mask_16k;
			addr = (bank_16k << 14) | ofs;
			return Rom[addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPpu(addr);
		}
	}
}
