namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public static class OctoshockDll
	{
		public enum ePeripheralType : int
		{
			None = 0, //can be used to signify disconnection

			Pad = 1, //SCPH-1080
			DualShock = 2, //SCPH-1200
			DualAnalog = 3, //SCPH-1180

			NegCon = 4,

			Multitap = 10,
		}
	}
}