using System.Threading;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	[PortedCore(CoreNames.Mupen64Plus + "b", "", "2.0", "https://code.google.com/p/mupen64plus/", singleInstance: true)]
	public partial class N64 : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IDisassemblable, IRegionable,
		ISettable<N64Settings, N64SyncSettings>
	{
		/// <summary>
		/// Create mupen64plus Emulator
		/// </summary>
		/// <param name="game">Game information of game to load</param>
		/// <param name="file">Rom that should be loaded</param>
		/// <param name="rom">rom data with consistent endianness/order</param>
		/// <param name="syncSettings">N64SyncSettings object</param>
		[CoreConstructor(VSystemID.Raw.N64)]
		public N64(GameInfo game, byte[] file, byte[] rom, N64Settings settings, N64SyncSettings syncSettings)
		{
			if (OSTailoredCode.IsUnixHost) throw new NotImplementedException();

			ServiceProvider = new BasicServiceProvider(this);
			InputCallbacks = new InputCallbackSystem();

			_memoryCallbacks.CallbackAdded += AddBreakpoint;
			_memoryCallbacks.CallbackRemoved += RemoveBreakpoint;

			int SaveType = 0;
			if (game.OptionValue("SaveType") == "EEPROM_16K")
			{
				SaveType = 1;
			}

			_syncSettings = syncSettings ?? new N64SyncSettings();
			_settings = settings ?? new N64Settings();

			_disableExpansionSlot = _syncSettings.DisableExpansionSlot;

			// Override the user's expansion slot setting if it is mentioned in the gamedb (it is mentioned but the game MUST have this setting or else not work
			if (game.OptionValue("expansionpak") != null && game.OptionValue("expansionpak") == "1")
			{
				_disableExpansionSlot = false;
				IsOverridingUserExpansionSlotSetting = true;
			}

			byte country_code = rom[0x3E];
			switch (country_code)
			{
				// PAL codes
				case 0x44:
				case 0x46:
				case 0x49:
				case 0x50:
				case 0x53:
				case 0x55:
				case 0x58:
				case 0x59:
					_display_type = DisplayType.PAL;
					break;

				// NTSC codes
				case 0x37:
				case 0x41:
				case 0x45:
				case 0x4a:
				default: // Fallback for unknown codes
					_display_type = DisplayType.NTSC;
					break;
			}

			StartThreadLoop();

			var videosettings = _syncSettings.GetVPS(game, _settings.VideoSizeX, _settings.VideoSizeY);
			var coreType = _syncSettings.Core;

			//zero 19-apr-2014 - added this to solve problem with SDL initialization corrupting the main thread (I think) and breaking subsequent emulators (for example, NES)
			//not sure why this works... if we put the plugin initializations in here, we get deadlocks in some SDL initialization. doesnt make sense to me...
			RunThreadAction(() =>
			{
				api = new mupen64plusApi(this, file, videosettings, SaveType, (int)coreType, _disableExpansionSlot);
			});

			// Order is important because the register with the mupen core
			_videoProvider = new N64VideoProvider(api, videosettings);
			_audioProvider = new N64Audio(api);
			_inputProvider = new N64Input(this.AsInputPollable(), api, _syncSettings.Controllers);
			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(_videoProvider);
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(_audioProvider.Resampler);

			switch (Region)
			{
				case DisplayType.NTSC:
					_videoProvider.VsyncNumerator = 60000;
					_videoProvider.VsyncDenominator = 1001;
					break;
				default:
					_videoProvider.VsyncNumerator = 50;
					_videoProvider.VsyncDenominator = 1;
					break;
			}

			string rsp;
			if (_syncSettings.VideoPlugin is PluginType.GLideN64) // GLideN64 can use either HLE or LLE RSP
			{
				rsp = _syncSettings.Rsp switch
				{
					N64SyncSettings.RspType.Rsp_cxd4 => "mupen64plus-rsp-cxd4-sse2.dll",
					_ => "mupen64plus-rsp-hle.dll",
				};
			}
			else if (_syncSettings.VideoPlugin is PluginType.Angrylion) // Angrylion can only use LLE RSP
			{
				rsp = "mupen64plus-rsp-cxd4-sse2.dll";
			}
			else // the rest can only use HLE RSP
			{
				rsp = "mupen64plus-rsp-hle.dll";
			}

			api.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_RSP, rsp);

			InitMemoryDomains();
			//if (_syncSettings.Core != N64SyncSettings.CoreType.Dynarec)
			{
				ConnectTracer();
				SetBreakpointHandler();
			}

			api.AsyncExecuteEmulator();

			// Hack: Saving a state on frame 0 has been shown to not be sync stable. Advance past that frame to avoid the problem.
			// Advancing 2 frames was chosen to deal with a problem with the dynamic recompiler. The dynarec seems to take 2 frames to set 
			// things up correctly. If a state is loaded on frames 0 or 1 mupen tries to access null pointers and the emulator crashes, so instead
			// advance past both to again avoid the problem.
			api.frame_advance();
			api.frame_advance();

			SetControllerButtons(_syncSettings);
		}

		private readonly N64Input _inputProvider;
		private readonly N64VideoProvider _videoProvider;
		private readonly N64Audio _audioProvider;

		private readonly EventWaitHandle _pendingThreadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly EventWaitHandle _completeThreadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

		private mupen64plusApi api; // mupen64plus DLL Api

		private N64SyncSettings _syncSettings;
		private N64Settings _settings;

		private bool _pendingThreadTerminate;

		private readonly DisplayType _display_type = DisplayType.NTSC;

		private Action _pendingThreadAction;

		private readonly bool _disableExpansionSlot = true;

		public IEmulatorServiceProvider ServiceProvider { get; }

		public bool UsingExpansionSlot => !_disableExpansionSlot;

		public bool IsOverridingUserExpansionSlotSetting { get; set; }

		public void Dispose()
		{
			RunThreadAction(() =>
			{
				_videoProvider.Dispose();
				_audioProvider.Dispose();
				api.Dispose();
			});

			EndThreadLoop();
		}

		private void ThreadLoop()
		{
			for (; ; )
			{
				_pendingThreadEvent.WaitOne();
				_pendingThreadAction();
				if (_pendingThreadTerminate)
				{
					break;
				}

				_completeThreadEvent.Set();
			}

			_pendingThreadTerminate = false;
			_completeThreadEvent.Set();
		}

		private void RunThreadAction(Action action)
		{
			_pendingThreadAction = action;
			_pendingThreadEvent.Set();
			_completeThreadEvent.WaitOne();
		}

		private void StartThreadLoop()
		{
			var thread = new Thread(ThreadLoop) { IsBackground = true };
			thread.Start(); // will this solve the hanging process problem?
		}

		private void EndThreadLoop()
		{
			RunThreadAction(() => { _pendingThreadTerminate = true; });
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_inputProvider.Controller = controller;

			FrameFinished = false;

			api.setTraceCallback(Tracer?.IsEnabled() is true ? _tracecb : null);

			_audioProvider.RenderSound = rendersound;

			if (controller.IsPressed("Reset"))
			{
				api.soft_reset();
			}

			if (controller.IsPressed("Power"))
			{
				api.hard_reset();
			}

			api.frame_advance();


			if (IsLagFrame)
			{
				LagCount++;
			}

			if(!api.IsCrashed)
				Frame++;

			return true;
		}

		public string SystemId => VSystemID.Raw.N64;

		public DisplayType Region => _display_type;

		public ControllerDefinition ControllerDefinition { get; private set; } = new("Nintendo 64 Controller");

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public bool DeterministicEmulation => false;
	}
}
