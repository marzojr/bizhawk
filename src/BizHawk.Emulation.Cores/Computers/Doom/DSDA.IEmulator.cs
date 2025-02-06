﻿using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Computers.Doom.CInterface;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool renderVideo, bool renderAudio)
		{
			// Declaring inputs
			PackedPlayerInput player1Inputs = new PackedPlayerInput();
			PackedPlayerInput player2Inputs = new PackedPlayerInput();
			PackedPlayerInput player3Inputs = new PackedPlayerInput();
			PackedPlayerInput player4Inputs = new PackedPlayerInput();

			if (_syncSettings.Player1Present)
			{
				player1Inputs._RunSpeed      = _controllerDeck.ReadPot1(controller, 0);
				player1Inputs._StrafingSpeed = _controllerDeck.ReadPot1(controller, 1);
				player1Inputs._TurningSpeed  = _controllerDeck.ReadPot1(controller, 2);
				player1Inputs._WeaponSelect  = _controllerDeck.ReadPot1(controller, 3);
				player1Inputs._Fire          = (_controllerDeck.ReadPort1(controller) & 0b00001) > 0 ? 1 : 0;
				player1Inputs._Action        = (_controllerDeck.ReadPort1(controller) & 0b00010) > 0 ? 1 : 0;
				player1Inputs._AltWeapon     = (_controllerDeck.ReadPort1(controller) & 0b00100) > 0 ? 1 : 0;

				// Raven Games
				if (_syncSettings.InputFormat == DoomControllerTypes.Heretic || _syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
				  player1Inputs._FlyLook = _controllerDeck.ReadPot1(controller, 4);
				  player1Inputs._ArtifactUse = _controllerDeck.ReadPot1(controller, 5);
				}
				if (_syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
				  player1Inputs._Jump = (_controllerDeck.ReadPort1(controller) & 0b01000) > 0 ? 1 : 0;
				  player1Inputs._EndPlayer = (_controllerDeck.ReadPort1(controller) & 0b10000) > 0 ? 1 : 0;
				}
			}

			if (_syncSettings.Player2Present)
			{
				player2Inputs._RunSpeed = _controllerDeck.ReadPot2(controller, 0);
				player2Inputs._StrafingSpeed = _controllerDeck.ReadPot2(controller, 1);
				player2Inputs._TurningSpeed = _controllerDeck.ReadPot2(controller, 2);
				player2Inputs._WeaponSelect = _controllerDeck.ReadPot2(controller, 3);
				player2Inputs._Fire = (_controllerDeck.ReadPort2(controller) & 0b00001) > 0 ? 1 : 0;
				player2Inputs._Action = (_controllerDeck.ReadPort2(controller) & 0b00010) > 0 ? 1 : 0;
				player2Inputs._AltWeapon = (_controllerDeck.ReadPort2(controller) & 0b00100) > 0 ? 1 : 0;

				// Raven Games
				if (_syncSettings.InputFormat == DoomControllerTypes.Heretic || _syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
					player2Inputs._FlyLook = _controllerDeck.ReadPot2(controller, 4);
					player2Inputs._ArtifactUse = _controllerDeck.ReadPot2(controller, 5);
				}
				if (_syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
					player2Inputs._Jump = (_controllerDeck.ReadPort2(controller) & 0b01000) > 0 ? 1 : 0;
					player2Inputs._EndPlayer = (_controllerDeck.ReadPort2(controller) & 0b10000) > 0 ? 1 : 0;
				}
			}

			if (_syncSettings.Player3Present)
			{
				player3Inputs._RunSpeed = _controllerDeck.ReadPot3(controller, 0);
				player3Inputs._StrafingSpeed = _controllerDeck.ReadPot3(controller, 1);
				player3Inputs._TurningSpeed = _controllerDeck.ReadPot3(controller, 2);
				player3Inputs._WeaponSelect = _controllerDeck.ReadPot3(controller, 3);
				player3Inputs._Fire = (_controllerDeck.ReadPort3(controller) & 0b00001) > 0 ? 1 : 0;
				player3Inputs._Action = (_controllerDeck.ReadPort3(controller) & 0b00010) > 0 ? 1 : 0;
				player3Inputs._AltWeapon = (_controllerDeck.ReadPort3(controller) & 0b00100) > 0 ? 1 : 0;

				// Raven Games
				if (_syncSettings.InputFormat == DoomControllerTypes.Heretic || _syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
					player3Inputs._FlyLook = _controllerDeck.ReadPot3(controller, 4);
					player3Inputs._ArtifactUse = _controllerDeck.ReadPot3(controller, 5);
				}
				if (_syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
					player3Inputs._Jump = (_controllerDeck.ReadPort3(controller) & 0b01000) > 0 ? 1 : 0;
					player3Inputs._EndPlayer = (_controllerDeck.ReadPort3(controller) & 0b10000) > 0 ? 1 : 0;
				}
			}

			if (_syncSettings.Player4Present)
			{
				player4Inputs._RunSpeed = _controllerDeck.ReadPot4(controller, 0);
				player4Inputs._StrafingSpeed = _controllerDeck.ReadPot4(controller, 1);
				player4Inputs._TurningSpeed = _controllerDeck.ReadPot4(controller, 2);
				player4Inputs._WeaponSelect = _controllerDeck.ReadPot4(controller, 3);
				player4Inputs._Fire = (_controllerDeck.ReadPort4(controller) & 0b00001) > 0 ? 1 : 0;
				player4Inputs._Action = (_controllerDeck.ReadPort4(controller) & 0b00010) > 0 ? 1 : 0;
				player4Inputs._AltWeapon = (_controllerDeck.ReadPort4(controller) & 0b00100) > 0 ? 1 : 0;

				// Raven Games
				if (_syncSettings.InputFormat == DoomControllerTypes.Heretic || _syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
					player4Inputs._FlyLook = _controllerDeck.ReadPot4(controller, 4);
					player4Inputs._ArtifactUse = _controllerDeck.ReadPot4(controller, 5);
				}
				if (_syncSettings.InputFormat == DoomControllerTypes.Hexen)
				{
					player4Inputs._Jump = (_controllerDeck.ReadPort4(controller) & 0b01000) > 0 ? 1 : 0;
					player4Inputs._EndPlayer = (_controllerDeck.ReadPort4(controller) & 0b10000) > 0 ? 1 : 0;
				}
			}

			PackedRenderInfo renderInfo = new PackedRenderInfo();
			renderInfo._RenderVideo = renderVideo ? 1 : 0;
			renderInfo._RenderAudio = renderAudio ? 1 : 0;
			renderInfo._PlayerPointOfView = _settings.DisplayPlayer - 1;

			Core.dsda_frame_advance(player1Inputs, player2Inputs, player3Inputs, player4Inputs, renderInfo);

			if (renderVideo)
				UpdateVideo();

			if (renderAudio)
				UpdateAudio();

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.Doom;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			Frame = 0;
		}

		public void Dispose()
		{
			_elf.Dispose();
		}
	}
}
