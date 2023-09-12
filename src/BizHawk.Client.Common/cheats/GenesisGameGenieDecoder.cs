﻿using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common.cheats
{
	public static class GenesisGameGenieDecoder
	{
		private static readonly Dictionary<char, long> GameGenieTable = new()
		{
			['A'] = 0,
			['B'] = 1,
			['C'] = 2,
			['D'] = 3,
			['E'] = 4,
			['F'] = 5,
			['G'] = 6,
			['H'] = 7,
			['J'] = 8,
			['K'] = 9,
			['L'] = 10,
			['M'] = 11,
			['N'] = 12,
			['P'] = 13,
			['R'] = 14,
			['S'] = 15,
			['T'] = 16,
			['V'] = 17,
			['W'] = 18,
			['X'] = 19,
			['Y'] = 20,
			['Z'] = 21,
			['0'] = 22,
			['1'] = 23,
			['2'] = 24,
			['3'] = 25,
			['4'] = 26,
			['5'] = 27,
			['6'] = 28,
			['7'] = 29,
			['8'] = 30,
			['9'] = 31
		};

		public static IDecodeResult Decode(string code)
		{
			if (code == null)
			{
				throw new ArgumentNullException(nameof(code));
			}

			if (code.IndexOf("-", StringComparison.Ordinal) != 4)
			{
				return new InvalidCheatCode("Game Genie Codes need to contain a dash after the fourth character");
			}
			if (code.Contains("I") | code.Contains("O") | code.Contains("Q") | code.Contains("U"))
			{
				return new InvalidCheatCode("Game Genie Codes do not use I, O, Q or U.");
			}

			// Remove the -
			code = code.Remove(4, 1);
			long hexCode = 0;

			// convert code to a long binary string
			foreach (char t in code)
			{
				hexCode <<= 5;
				_ = GameGenieTable.TryGetValue(t, out long y);
				hexCode |= y;
			}

			long decoded = (hexCode & 0xFF00000000) >> 32;
			decoded |= hexCode & 0x00FF000000;
			decoded |= (hexCode & 0x0000FF0000) << 16;
			decoded |= (hexCode & 0x00000000700) << 5;
			decoded |= (hexCode & 0x000000F800) >> 3;
			decoded |= (hexCode & 0x00000000FF) << 16;

			return new DecodeResult
			{
				Size = WatchSize.Word,
				Value = (int)(decoded & 0x000000FFFF),
				Address= (int)((decoded & 0xFFFFFF0000) >> 16)
			};
		}
	}
}
