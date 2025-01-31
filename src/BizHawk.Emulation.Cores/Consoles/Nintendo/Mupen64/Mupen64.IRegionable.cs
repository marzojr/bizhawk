using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Mupen64;

public partial class Mupen64 : IRegionable
{
	private readonly DisplayType _region;

	public DisplayType Region
	{
		get => _region;
		private init
		{
			_region = value;
			VsyncNumerator = _region switch
			{
				DisplayType.PAL => 50,
				_ => 60
			};
		}
	}
}
