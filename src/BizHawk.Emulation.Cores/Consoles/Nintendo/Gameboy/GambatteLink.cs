using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	[PortedCore(CoreNames.GambatteLink, "sinamas/natt")]
	public partial class GambatteLink : ILinkable, ILinkedGameBoyCommon, IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.GBL)]
		public GambatteLink(CoreLoadParameters<GambatteLinkSettings, GambatteLinkSyncSettings> lp)
		{
			if (lp.Roms.Count < MIN_PLAYERS || lp.Roms.Count > MAX_PLAYERS)
				throw new InvalidOperationException("Wrong number of roms");

			_numCores = lp.Roms.Count;

			_serviceProvider = new BasicServiceProvider(this);
			_settings = lp.Settings ?? new GambatteLinkSettings();
			_syncSettings = lp.SyncSettings ?? new GambatteLinkSyncSettings();

			_linkedCores = new Gameboy[_numCores];
			_linkedConts = new SaveController[_numCores];
			_linkedBlips = new BlipBuffer[_numCores];
			_linkedLatches = new int[_numCores];
			_linkedOverflow = new int[_numCores];

			RomDetails = "";

			var scopes = new string[_numCores * 7];
			for (int i = 0; i < _numCores; i++)
			{
				scopes[i * 7 + 0] = $"P{i + 1} System Bus";
				scopes[i * 7 + 1] = $"P{i + 1} ROM";
				scopes[i * 7 + 2] = $"P{i + 1} VRAM";
				scopes[i * 7 + 3] = $"P{i + 1} SRAM";
				scopes[i * 7 + 4] = $"P{i + 1} WRAM";
				scopes[i * 7 + 5] = $"P{i + 1} OAM";
				scopes[i * 7 + 6] = $"P{i + 1} HRAM";
			}

			_memoryCallbacks = new MemoryCallbackSystem(scopes);

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i] = new Gameboy(lp.Comm, lp.Roms[i].Game, lp.Roms[i].RomData, _settings._linkedSettings[i], _syncSettings._linkedSyncSettings[i], lp.DeterministicEmulationRequested);
				_linkedCores[i].ConnectInputCallbackSystem(_inputCallbacks);
				_linkedCores[i].ConnectMemoryCallbackSystem(_memoryCallbacks, i);
				IsAnySgb |= IsSgb(i);
				_linkedConts[i] = new SaveController(Gameboy.CreateControllerDefinition(sgb: IsSgb(i), sub: false, tilt: false, rumble: false, remote: false));
				_linkedBlips[i] = new BlipBuffer(1024);
				_linkedBlips[i].SetRates(2097152 * 2, 44100);
				_linkedOverflow[i] = 0;
				_linkedLatches[i] = 0;
				RomDetails += $"P{i + 1}:\r\n" + _linkedCores[i].RomDetails;
			}

			LinkConnected = true;

			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			SoundBuffer = new short[MaxSampsPerFrame * _numCores];

			FrameBuffer = CreateVideoBuffer();
			VideoBuffer = CreateVideoBuffer();
			SgbVideoBuffer = CreateSGBVideoBuffer();

			GBLinkController = CreateControllerDefinition();

			_linkedDebuggable = new LinkedDebuggable(_linkedCores, _numCores, _memoryCallbacks);
			_serviceProvider.Register<IDebuggable>(_linkedDebuggable);

			_linkedDisassemblable = new LinkedDisassemblable(new GBDisassembler(), _numCores);
			_serviceProvider.Register<IDisassemblable>(_linkedDisassemblable);

			_linkedMemoryDomains = new LinkedMemoryDomains(_linkedCores, _numCores, _linkedDisassemblable);
			_serviceProvider.Register<IMemoryDomains>(_linkedMemoryDomains);

			_linkedSaveRam = new LinkedSaveRam(_linkedCores, _numCores);
			_serviceProvider.Register<ISaveRam>(_linkedSaveRam);
		}

		private readonly BasicServiceProvider _serviceProvider;

		private readonly MemoryCallbackSystem _memoryCallbacks;

		public string RomDetails { get; }

		public bool LinkConnected
		{
			get => _linkConnected;
			set
			{
				_linkConnected = value;
				for (int i = 0; i < _numCores; i++)
				{
					LibGambatte.gambatte_linkstatus(_linkedCores[i].GambatteState, _linkConnected ? 264 : 265);
				}
			}
		}

		private int _numCores = 0;
		private readonly Gameboy[] _linkedCores;

		private readonly LinkedDebuggable _linkedDebuggable;
		private readonly LinkedDisassemblable _linkedDisassemblable;
		private readonly LinkedMemoryDomains _linkedMemoryDomains;
		private readonly LinkedSaveRam _linkedSaveRam;

		// counters to ensure we do 35112 samples per frame
		private readonly int[] _linkedOverflow;

		// if true, the link connection is currently active
		private bool _linkConnected = true;

		// if true, the link is currently shifted (3x/4x only)
		private bool _linkShifted = false;

		// if true, the link is currently spaced outwards (3x/4x only)
		private bool _linkSpaced = false;

		// if true, the link toggle signal is currently asserted
		private bool _linkDiscoSignal = false;

		// if true, the link shift signal is currently asserted
		private bool _linkShiftSignal = false;

		// if true, the link cable spacing signal is currently asserted
		private bool _linkSpaceSignal = false;

		private const int SampPerFrame = 35112;
		private const int MaxSampsPerFrame = (SampPerFrame + 2064) * 2;

		private readonly SaveController[] _linkedConts;

		public IGameboyCommon First
			=> _linkedCores[0];

		private ControllerDefinition GBLinkController { get; }

		private ControllerDefinition CreateControllerDefinition()
		{
			static void AddGBButtonsForPlayer(int p, ControllerDefinition ret)
			{
				var pfx = $"P{p} ";
				ret.BoolButtons.AddRange(new[] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" }
					.Select(s => pfx + s));
			}
			ControllerDefinition ret = new($"GB Link {_numCores}x Controller");
			for (int i = 0, p = 1; i < _numCores; i++)
			{
				AddGBButtonsForPlayer(p, ret);
				ret.BoolButtons.Add($"P{p} Power");
				p++;
				if (IsSgb(i))
				{
					// add 3 more gamepads without a Power button
					for (int e = p + 3; p < e; p++) AddGBButtonsForPlayer(p, ret);
				}
			}
			ret.BoolButtons.Add("Toggle Link Connection");
			if (_numCores > 2)
			{
				ret.BoolButtons.Add("Toggle Link Shift");
				ret.BoolButtons.Add("Toggle Link Spacing");
			}
			return ret.MakeImmutable();
		}

		private bool IsSgb(int i)
			=> _linkedCores[i].IsSgb;

		public bool IsAnySgb { get; private set; }

		private bool ShowBorder(int i)
			=> IsSgb(i) && _settings._linkedSettings[i].ShowBorder;

		public bool ShowAnyBorder()
		{
			for (var i = 0; i < _numCores; i++) if (ShowBorder(i)) return true;
			return false;
		}

		private const int P1 = 0;
		private const int P2 = 1;
		private const int P3 = 2;
		private const int P4 = 3;

		private const int MIN_PLAYERS = 2;
		private const int MAX_PLAYERS = 4;
	}
}
