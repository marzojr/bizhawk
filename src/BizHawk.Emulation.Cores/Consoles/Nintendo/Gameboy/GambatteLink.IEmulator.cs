using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IEmulator, IBoardInfo
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => GBLinkController;

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedConts[i].Clear();
			}

			foreach (var s in GBLinkController.BoolButtons)
			{
				if (!controller.IsPressed(s)) continue;
				if (s[0] is not 'P') continue;
				var iSpace = s.IndexOf(' ');
				var playerNum = int.Parse(s.Substring(startIndex: 1, length: iSpace - 1));
				var consoleNum = 0;
				var isSGB = false;
				while (consoleNum < _numCores)
				{
					isSGB = IsSgb(consoleNum);
					var playersForConsole = isSGB ? 4 : 1;
					if (playerNum <= playersForConsole) break;
					playerNum -= playersForConsole;
					consoleNum++;
				}
				//TODO rather than this string manipulation, could construct a lookup ahead of time
				_linkedConts[consoleNum].Set(s.EndsWithOrdinal("Power")
					? "Power"
					: isSGB
						? $"P{playerNum} {s.Substring(startIndex: iSpace + 1)}"
						: s.Substring(startIndex: iSpace + 1));
			}

			bool linkDiscoSignalNew = controller.IsPressed("Toggle Link Connection");
			if (linkDiscoSignalNew && !_linkDiscoSignal)
			{
				LinkConnected ^= true;
				Console.WriteLine("Link connect status to {0}", LinkConnected);
			}

			_linkDiscoSignal = linkDiscoSignalNew;

			if (_numCores > 2)
			{
				bool linkShiftSignalNew = controller.IsPressed("Toggle Link Shift");
				if (linkShiftSignalNew && !_linkShiftSignal)
				{
					_linkShifted ^= true;
					Console.WriteLine("Link shift status to {0}", _linkShifted);
				}

				_linkShifted = linkShiftSignalNew;

				bool linkSpaceSignalNew = controller.IsPressed("Toggle Link Spacing");
				if (linkSpaceSignalNew && !_linkSpaceSignal)
				{
					_linkSpaced ^= true;
					Console.WriteLine("Link spacing status to {0}", _linkSpaceSignal);
				}

				_linkSpaceSignal = linkSpaceSignalNew;
			}

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].FrameAdvancePrep(_linkedConts[i]);
			}

			unsafe
			{
				fixed (int* fbuff = &FrameBuffer[0], svbuff = SgbVideoBuffer)
				{
					// use pitch to have both cores write to the same frame buffer, interleaved
					int Pitch = 160 * _numCores;
					int sgbPitch = 256 * _numCores;

					fixed (short* sbuff = &SoundBuffer[0])
					{
						int Step = _linkConnected ? 4 : SampPerFrame; // don't bother stepping if we aren't linked

						int[] n = new int[_numCores];
						for (int i = 0; i < _numCores; i++)
						{
							n[i] = _linkedOverflow[i];
						}

						// slowly step our way through the frame, while continually checking and resolving link cable status
						for (int target = 0; target < SampPerFrame;)
						{
							target += Step;
							if (target > SampPerFrame)
							{
								target = SampPerFrame; // don't run for slightly too long depending on step
							}

							for (int i = 0; i < _numCores; i++)
							{
								// gambatte_runfor() aborts early when a frame is produced, but we don't want that, hence the while()
								while (n[i] < target)
								{
									uint nsamp = (uint)(target - n[i]);
									if (LibGambatte.gambatte_runfor(_linkedCores[i].GambatteState, fbuff + (i * 160), Pitch, sbuff + (i * MaxSampsPerFrame) + (n[i] * 2), ref nsamp) > 0)
									{
										for (int j = 0; j < 144; j++)
										{
											Array.Copy(FrameBuffer, (i * 160) + (j * Pitch), VideoBuffer, (i * 160) + (j * Pitch), 160);
										}
										if (IsAnySgb)
										{
											// all SGB borders will be displayed when any of them has the option enabled
											if (IsSgb(i))
											{
												if (LibGambatte.gambatte_updatescreenborder(
													_linkedCores[i].GambatteState,
													svbuff + (i * 256),
													sgbPitch) is not 0)
												{
													throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_updatescreenborder)}() returned non-zero (border error???)");
												}
											}
											else
											{
												for (int j = 0; j < 144; j++)
												{
													Array.Copy(FrameBuffer, (i * 160) + (j * Pitch), SgbVideoBuffer, (i * 256 + 48) + (40 + j) * sgbPitch, 160);
												}
											}
										}
									}

									n[i] += (int)nsamp;
								}
							}

							// poll link cable statuses, but not when the cable is disconnected
							if (!_linkConnected)
							{
								continue;
							}

							switch (_numCores)
							{
								case 2:
									{
										TryLinkTransfer(P1, P2);
										break;
									}
								case 3:
									{
										if (_linkSpaced)
										{
											TryLinkTransfer(P1, P3);
										}
										else if (_linkShifted)
										{
											TryLinkTransfer(P2, P3);
										}
										else
										{
											TryLinkTransfer(P1, P2);
										}
										break;
									}
								case 4:
									{
										if (_linkSpaced)
										{
											TryLinkTransfer(P1, P3);
											TryLinkTransfer(P2, P4);
										}
										else if (_linkShifted)
										{
											TryLinkTransfer(P2, P3);
											TryLinkTransfer(P4, P1);
										}
										else
										{
											TryLinkTransfer(P1, P2);
											TryLinkTransfer(P3, P4);
										}
										break;
									}
								default:
									throw new Exception();
							}
						}

						for (int i = 0; i < _numCores; i++)
						{
							_linkedOverflow[i] = n[i] - SampPerFrame;
							if (_linkedOverflow[i] < 0)
							{
								throw new Exception("Timing problem?");
							}
						}

						if (rendersound)
						{
							PrepSound();
						}

						// copy extra samples back to beginning
						for (int i = 0; i < _numCores; i++)
						{
							for (int j = 0; j < _linkedOverflow[i] * 2; j++)
							{
								SoundBuffer[(i * MaxSampsPerFrame) + j] = SoundBuffer[(i * MaxSampsPerFrame) + j + (SampPerFrame * 2)];
							}
						}
					}
				}
			}

			IsLagFrame = true;

			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].FrameAdvancePost();
				IsLagFrame &= _linkedCores[i].IsLagFrame;
			}

			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			return true;
		}

		private void TryLinkTransfer(int one, int two)
		{
			if (LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 256) != 0) // ClockTrigger
			{
				LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 257); // ack
				int oo = LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 258); // GetOut
				int ot = LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 258);
				LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, oo & 0xff); // ShiftIn
				LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, ot & 0xff); // ShiftIn
			}

			if (LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 256) != 0) // ClockTrigger
			{
				LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 257); // ack
				int ot = LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 258); // GetOut
				int oo = LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 258);
				LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, ot & 0xff); // ShiftIn
				LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, oo & 0xff); // ShiftIn
			}

			if (LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 259) != 0) // InfraredTrigger
			{
				LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 260); // ack
				if (LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 261) != 0) // GetOut
				{
					LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 262); // ShiftInOn
				}
				else
				{
					LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 263); // ShiftInOff
				}
			}

			if (LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 259) != 0) // InfraredTrigger
			{
				LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 260); // ack
				if (LibGambatte.gambatte_linkstatus(_linkedCores[two].GambatteState, 261) != 0) // GetOut
				{
					LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 262); // ShiftInOn
				}
				else
				{
					LibGambatte.gambatte_linkstatus(_linkedCores[one].GambatteState, 263); // ShiftInOff
				}
			}
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.GBL;

		public bool DeterministicEmulation => LinkedDeterministicEmulation();

		private bool LinkedDeterministicEmulation()
		{
			bool deterministicEmulation = true;
			for (int i = 0; i < _numCores; i++)
			{
				deterministicEmulation &= _linkedCores[i].DeterministicEmulation;
			}
			return deterministicEmulation;
		}

		public string BoardName => LinkedBoardName();

		private string LinkedBoardName()
		{
			string boardName = "";
			for (int i = 0; i < _numCores; i++)
			{
				boardName += _linkedCores[i].BoardName + "|";
			}
			return boardName.Remove(boardName.Length - 1);
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			for (int i = 0; i < _numCores; i++)
			{
				_linkedOverflow[i] = 0;
			}
		}

		public void Dispose()
		{
			if (_numCores > 0)
			{
				for (int i = 0; i < _numCores; i++)
				{
					_linkedCores[i].Dispose();
					_linkedCores[i] = null;
					_linkedBlips[i].Dispose();
					_linkedBlips[i] = null;
				}

				_numCores = 0;
			}
		}
	}
}
