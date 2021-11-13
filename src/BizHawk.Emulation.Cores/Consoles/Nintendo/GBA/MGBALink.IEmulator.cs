using System;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => GBALinkController;

		private static ControllerDefinition GBALinkController { get; set; }

		private ControllerDefinition CreateControllerDefinition()
		{
			var ret = new ControllerDefinition { Name = $"GBA Link {_numCores}x Controller" };
			for (int i = 0; i < _numCores; i++)
			{
				ret.BoolButtons.AddRange(
					new[] { "Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "L", "R", "Power" }
						.Select(s => $"P{i + 1} {s}"));
				ret.AddXYZTriple($"P{i + 1} " + "Tilt {0}", (-32767).RangeTo(32767), 0);
				ret.AddAxis($"P{i + 1} " + "Light Sensor", 0.RangeTo(255), 0);
			}
			return ret;
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedConts[i].Clear();
			}

			foreach (var s in GBALinkController.BoolButtons)
			{
				if (controller.IsPressed(s))
				{
					for (int i = 0; i < _numCores; i++)
					{
						if (s.Contains($"P{i + 1} "))
						{
							_linkedConts[i].Set(s.Replace($"P{i + 1} ", ""));
						}
					}
				}
			}

			foreach (var s in GBALinkController.Axes)
			{
				for (int i = 0; i < _numCores; i++)
				{
					if (s.Key.Contains($"P{i + 1} "))
					{
						_linkedConts[i].SetAxisValue(s.Key.Replace($"P{i + 1} ", ""), controller.AxisValue(s.Key));
					}
				}
			}

			// todo: actually step
			// todo: link!
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].FrameAdvance(_linkedConts[i], render, rendersound);
			}

			unsafe
			{
				for (int i = 0; i < _numCores; i++)
				{
					fixed (int* fb = &_linkedCores[i].GetVideoBuffer()[0], vb = &_videobuff[i * 240])
					{
						for (int j = 0; j < 160; j++)
						{
							for (int k = 0; k < 240; k++)
							{
								vb[j * BufferWidth + k] = fb[j * 240 + k];
							}
						}
					}
				}

				_linkedCores[0].GetSamplesSync(out short[] lsamples, out int lnsamp);
				fixed (short* ls = &lsamples[0], sb = &_soundbuff[0])
				{
					for (int i = 0; i < lnsamp; i++)
					{
						int lsamp = (lsamples[i * 2] + lsamples[i * 2 + 1]) / 2;
						sb[i * 2] = (short)lsamp;
					}
				}

				_linkedCores[1].GetSamplesSync(out short[] rsamples, out int rnsamp);
				fixed (short* rs = &rsamples[0], sb = &_soundbuff[0])
				{
					for (int i = 0; i < rnsamp; i++)
					{
						int rsamp = (rsamples[i * 2] + rsamples[i * 2 + 1]) / 2;
						sb[i * 2 + 1] = (short)rsamp;
					}
				}

				if (rendersound)
				{
					_nsamp = Math.Max(lnsamp, rnsamp);
				}
				else
				{
					_nsamp = 0;
				}
			}

			IsLagFrame = false;
			for (int i = 0; i < _numCores; i++)
			{
				if (_linkedCores[i].IsLagFrame)
					IsLagFrame = true;
			}

			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => "GBALink";

		public bool DeterministicEmulation => LinkedDeterministicEmulation();

		private bool LinkedDeterministicEmulation()
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (_linkedCores[i].DeterministicEmulation)
					return true;
			}
			return false;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (_numCores > 0)
			{
				for (int i = 0; i < _numCores; i++)
				{
					_linkedCores[i].Dispose();
					_linkedCores[i] = null;
				}

				_numCores = 0;
			}
		}
	}
}