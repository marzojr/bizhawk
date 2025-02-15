using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class StickyHoldController : IController
	{
		private readonly HashSet<string> _buttonHolds = [ ];
		private readonly Dictionary<string, int> _axisHolds = [ ];

		public ControllerDefinition Definition { get; }

		public IReadOnlyCollection<string> CurrentHolds => _buttonHolds; // the callsite doesn't care about sticky axes

		public StickyHoldController(ControllerDefinition definition)
		{
			Definition = definition;
		}

		public bool IsPressed(string button)
		{
			return _buttonHolds.Contains(button);
		}

		public int AxisValue(string name)
			=> _axisHolds.TryGetValue(name, out int axisValue)
				? axisValue
#if DEBUG
				: Definition.Axes[name].Neutral; // throw if no such axis
#else
				: Definition.Axes.TryGetValue(name, out var axisSpec) ? axisSpec.Neutral : default;
#endif

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotSupportedException();
		public void SetHapticChannelStrength(string name, int strength) => throw new NotSupportedException();

		public void SetButtonHold(string button, bool enabled)
		{
			if (enabled)
			{
				_buttonHolds.Add(button);
			}
			else
			{
				_buttonHolds.Remove(button);
			}
		}

		public void SetAxisHold(string name, int? value)
		{
			if (value.HasValue)
			{
				_axisHolds[name] = value.Value;
			}
			else
			{
				_axisHolds.Remove(name);
			}
		}

		public bool IsSticky(string buttonOrAxis) => _buttonHolds.Contains(buttonOrAxis) || _axisHolds.ContainsKey(buttonOrAxis);

		public void ClearStickies()
		{
			_buttonHolds.Clear();
			_axisHolds.Clear();
		}

		private List<string> _justPressed = [ ];

		public void ToggleStickyState(string button)
		{
			if (_buttonHolds.Contains(button))
			{
				_buttonHolds.Remove(button);
			}
			else 
			{
				_buttonHolds.Add(button);
			}
			_justPressed = [ button ];
		}

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				if (_buttonHolds.Contains(button))
				{
					_buttonHolds.Remove(button);
				}
				else
				{
					_buttonHolds.Add(button);
				}
			}

			_justPressed = buttons;
		}
	}

	public class StickyAutofireController : IController
	{
		// TODO: Change the AutoHold controller to be one of these, with an 'Off' value of 0?
		// Probably would have slightly lower performance, but it seems weird to have such a similar class that is only used once.
		private int _onFrames;
		private int _offFrames;
		private bool _respectLag;

		private readonly Mutex _boolPatternsMutex = new Mutex();
		private readonly Dictionary<string, AutoPatternBool> _boolPatterns = [ ];
		private readonly Mutex _axisPatternsMutex = new Mutex();
		private readonly Dictionary<string, AutoPatternAxis> _axisPatterns = [ ];

		public ControllerDefinition Definition { get; }

		public IReadOnlyCollection<string> CurrentAutofires => _boolPatterns.Keys; // the callsite doesn't care about sticky axes

		public StickyAutofireController(ControllerDefinition definition, int onFrames = 1, int offFrames = 1, bool respectLag = true)
		{
			Definition = definition;
			UpdateDefaultPatternSettings(onFrames, offFrames, respectLag);
		}

		public bool IsPressed(string button)
		{
			return _boolPatterns.TryGetValue(button, out var pattern) && pattern.PeekNextValue();
		}

		public int AxisValue(string name)
			=> _axisPatterns.TryGetValue(name, out var pattern)
				? pattern.PeekNextValue()
#if DEBUG
				: Definition.Axes[name].Neutral; // throw if no such axis
#else
				: Definition.Axes.TryGetValue(name, out var axisSpec) ? axisSpec.Neutral : default;
#endif

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => throw new NotSupportedException();
		public void SetHapticChannelStrength(string name, int strength) => throw new NotSupportedException();

		public void UpdateDefaultPatternSettings(int onFrames, int offFrames, bool respectLag)
		{
			_onFrames = Math.Max(onFrames, 1);
			_offFrames = Math.Max(offFrames, 1);
			_respectLag = respectLag;
		}

		public void SetButtonAutofire(string button, bool enabled, AutoPatternBool pattern = null)
		{
			_boolPatternsMutex.WaitOne();
			if (enabled)
			{
				pattern ??= new AutoPatternBool(_onFrames, _offFrames, _respectLag);
				_boolPatterns[button] = pattern;
			}
			else
			{
				_boolPatterns.Remove(button);
			}
			_boolPatternsMutex.ReleaseMutex();
		}

		public void SetAxisAutofire(string name, int? value, AutoPatternAxis pattern = null)
		{
			_axisPatternsMutex.WaitOne();
			if (value.HasValue)
			{
				pattern ??= new AutoPatternAxis(value.Value, _onFrames, Definition.Axes[name].Neutral, _offFrames, _respectLag);
				_axisPatterns[name] = pattern;
			}
			else
			{
				_axisPatterns.Remove(name);
			}
			_axisPatternsMutex.ReleaseMutex();
		}

		public bool IsSticky(string buttonOrAxis) => _boolPatterns.ContainsKey(buttonOrAxis) || _axisPatterns.ContainsKey(buttonOrAxis);

		public void ClearStickies()
		{
			_boolPatternsMutex.WaitOne();
			_boolPatterns.Clear();
			_boolPatternsMutex.ReleaseMutex();

			_axisPatternsMutex.WaitOne();
			_axisPatterns.Clear();
			_axisPatternsMutex.ReleaseMutex();
		}

		public void IncrementLoops(bool lagged)
		{
			_boolPatternsMutex.WaitOne();
			foreach (var v in _boolPatterns.Values) v.GetNextValue(lagged);
			_boolPatternsMutex.ReleaseMutex();

			_axisPatternsMutex.WaitOne();
			foreach (var v in _axisPatterns.Values) v.GetNextValue(lagged);
			_axisPatternsMutex.ReleaseMutex();
		}

		private List<string> _justPressed = [ ];

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (var button in buttons.Where(button => !_justPressed.Contains(button)))
			{
				SetButtonAutofire(button, !_boolPatterns.ContainsKey(button));
			}

			_justPressed = buttons;
		}
	}
}
