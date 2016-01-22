﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private int _backgroundColor0;
		private int _backgroundColor1;
		private int _backgroundColor2;
		private int _backgroundColor3;
		private int _baCount;
		private bool _badline;
		private bool _badlineEnable;
		private int _bitmapColumn;
		private bool _bitmapMode;
		private int _borderB;
		private bool _borderCheckLEnable;
		private bool _borderCheckREnable;
		private int _borderColor;
		private int _borderL;
		private bool _borderOnMain;
		private bool _borderOnVertical;
		private int _borderR;
		private int _borderT;
		private readonly int[] _bufferC;
		private readonly int[] _bufferG;
		private int _cycle;
		private int _cycleIndex;
		private bool _columnSelect;
		private int _dataC;
		private int _dataG;
		private bool _displayEnable;
		private int _displayC;
		private bool _enableIntLightPen;
		private bool _enableIntRaster;
		private bool _enableIntSpriteCollision;
		private bool _enableIntSpriteDataCollision;
		private bool _extraColorMode;
		private bool _hblank;
		private bool _idle;
		private bool _intLightPen;
		private bool _intRaster;
		private bool _intSpriteCollision;
		private bool _intSpriteDataCollision;
		private readonly int _hblankEnd;
		private readonly int _hblankStart;
		private bool _hblankCheckEnableL;
		private bool _hblankCheckEnableR;
		private int _lastRasterLine;
		private int _lightPenX;
		private int _lightPenY;
		private bool _multicolorMode;
		private bool _pinAec = true;
		private bool _pinBa = true;
		private bool _pinIrq = true;
		private int _pointerCb;
		private int _pointerVm;
		private int _rasterInterruptLine;
		private int _rasterLine;
		private int _rasterX;
		private bool _rasterXHold;
		private int _rc;
		private int _refreshCounter;
		private bool _renderEnabled;
		private bool _rowSelect;
		private int _spriteMulticolor0;
	    private int _spriteMulticolor1;
	    private readonly Sprite _sprite0;
        private readonly Sprite _sprite1;
        private readonly Sprite _sprite2;
        private readonly Sprite _sprite3;
        private readonly Sprite _sprite4;
        private readonly Sprite _sprite5;
        private readonly Sprite _sprite6;
        private readonly Sprite _sprite7;
        private readonly Sprite[] _sprites;
		private int _sr;
		private int _srMask;
		private int _srMask1;
		private int _srMask2;
		private int _srMask3;
		private int _srMaskMc;
		private int _srSpriteMask;
		private int _srSpriteMask1;
		private int _srSpriteMask2;
		private int _srSpriteMask3;
		private int _srSpriteMaskMc;
		private bool _vblank;
		private readonly int _vblankEnd;
		private readonly int _vblankStart;
		private int _vc;
		private int _vcbase;
		private int _vmli;
		private int _xScroll;
		private int _yScroll;

		public void HardReset()
		{
			// *** SHIFT REGISTER BITMASKS ***
			_srMask1 = 0x20000;
			_srMask2 = _srMask1 << 1;
			_srMask3 = _srMask1 | _srMask2;
			_srMask = _srMask2;
			_srMaskMc = _srMask3;
			_srSpriteMask1 = 0x400000;
			_srSpriteMask2 = _srSpriteMask1 << 1;
			_srSpriteMask3 = _srSpriteMask1 | _srSpriteMask2;
			_srSpriteMask = _srSpriteMask2;
			_srSpriteMaskMc = _srSpriteMask3;

			_pinAec = true;
			_pinBa = true;
			_pinIrq = true;

			_bufOffset = 0;

			_backgroundColor0 = 0;
			_backgroundColor1 = 0;
			_backgroundColor2 = 0;
			_backgroundColor3 = 0;
			_baCount = BaResetCounter;
			_badline = false;
			_badlineEnable = false;
			_bitmapMode = false;
			_borderCheckLEnable = false;
			_borderCheckREnable = false;
			_borderColor = 0;
			_borderOnMain = true;
			_borderOnVertical = true;
			_columnSelect = false;
			_displayEnable = false;
			_enableIntLightPen = false;
			_enableIntRaster = false;
			_enableIntSpriteCollision = false;
			_enableIntSpriteDataCollision = false;
			_extraColorMode = false;
			_hblank = true;
			_idle = true;
			_intLightPen = false;
			_intRaster = false;
			_intSpriteCollision = false;
			_intSpriteDataCollision = false;
			_lastRasterLine = 0;
			_lightPenX = 0;
			_lightPenY = 0;
			_multicolorMode = false;
			_pointerCb = 0;
			_pointerVm = 0;
			_rasterInterruptLine = 0;
			_rasterLine = 0;
			_rasterX = 0;
			_rc = 7;
			_refreshCounter = 0xFF;
			_rowSelect = false;
			_spriteMulticolor0 = 0;
			_spriteMulticolor1 = 0;
			_sr = 0;
			_vblank = true;
			_vc = 0;
			_vcbase = 0;
			_vmli = 0;
			_xScroll = 0;
			_yScroll = 0;

			// reset sprites
			for (var i = 0; i < 8; i++)
				_sprites[i].HardReset();

			// clear C buffer
			for (var i = 0; i < 40; i++)
			{
				_bufferC[i] = 0;
				_bufferG[i] = 0;
			}

			_pixBuffer = new int[PixBufferSize];
			UpdateBorder();
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
			for (var i = 0; i < 8; i++)
			{
				ser.BeginSection("sprite" + i.ToString());
				SaveState.SyncObject(ser, _sprites[i]);
				ser.EndSection();
			}
		}
	}
}
