using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	/// <remarks>
	/// TODO these colours are obviously related in some way, calculate the lighter/darker shades from a base colour
	/// (I've already used CurrentFrame_InputLog in the definition of SeekFrame_InputLog, they differed only in alpha)
	/// --yoshi
	/// </remarks>
	public readonly struct TAStudioPaletteMPR
	{
		public static readonly TAStudioPaletteMPR Default = new(
//			currentFrame_FrameCol: Color.FromArgb(0xCF, 0xED, 0xFC),
			currentFrame_InputLog: Color.FromArgb(0xB5, 0xE7, 0xF7),
			greenZone_FrameCol: Color.FromArgb(0xDD, 0xFF, 0xDD),
			greenZone_InputLog: Color.FromArgb(0xD2, 0xF9, 0xD3),
			greenZone_InputLog_Stated: Color.FromArgb(0xC4, 0xF7, 0xC8),
			greenZone_InputLog_Invalidated: Color.FromArgb(0xE0, 0xFB, 0xE0),
			lagZone_FrameCol: Color.FromArgb(0xFF, 0xDC, 0xDD),
			lagZone_InputLog: Color.FromArgb(0xF4, 0xDA, 0xDA),
			lagZone_InputLog_Stated: Color.FromArgb(0xF0, 0xD0, 0xD2),
			lagZone_InputLog_Invalidated: Color.FromArgb(0xF7, 0xE5, 0xE5),
			marker_FrameCol: Color.FromArgb(0xF7, 0xFF, 0xC9),
			analogEdit_Col: Color.FromArgb(0x90, 0x90, 0x70)); // SuuperW: When editing an analog value, it will be a gray color.

//		public readonly Color CurrentFrame_FrameCol;

		public readonly Color CurrentFrame_InputLog;

//		public readonly Color SeekFrame_InputLog;

		public readonly Color GreenZone_FrameCol;

		public readonly Color GreenZone_InputLog;

		public readonly Color GreenZone_InputLog_Stated;

		public readonly Color GreenZone_InputLog_Invalidated;

		public readonly Color LagZone_FrameCol;

		public readonly Color LagZone_InputLog;

		public readonly Color LagZone_InputLog_Stated;

		public readonly Color LagZone_InputLog_Invalidated;

		public readonly Color Marker_FrameCol;

		public readonly Color AnalogEdit_Col;

		public TAStudioPaletteMPR(
//			Color currentFrame_FrameCol,
			Color currentFrame_InputLog,
			Color greenZone_FrameCol,
			Color greenZone_InputLog,
			Color greenZone_InputLog_Stated,
			Color greenZone_InputLog_Invalidated,
			Color lagZone_FrameCol,
			Color lagZone_InputLog,
			Color lagZone_InputLog_Stated,
			Color lagZone_InputLog_Invalidated,
			Color marker_FrameCol,
			Color analogEdit_Col)
		{
//			CurrentFrame_FrameCol = currentFrame_FrameCol;
			CurrentFrame_InputLog = currentFrame_InputLog;
//			SeekFrame_InputLog = Color.FromArgb(0x70, currentFrame_InputLog);
			GreenZone_FrameCol = greenZone_FrameCol;
			GreenZone_InputLog = greenZone_InputLog;
			GreenZone_InputLog_Stated = greenZone_InputLog_Stated;
			GreenZone_InputLog_Invalidated = greenZone_InputLog_Invalidated;
			LagZone_FrameCol = lagZone_FrameCol;
			LagZone_InputLog = lagZone_InputLog;
			LagZone_InputLog_Stated = lagZone_InputLog_Stated;
			LagZone_InputLog_Invalidated = lagZone_InputLog_Invalidated;
			Marker_FrameCol = marker_FrameCol;
			AnalogEdit_Col = analogEdit_Col;
		}

		public static implicit operator TAStudioPaletteMPR(TAStudioPalette v)
		{
			throw new NotImplementedException();
		}
	}
}
