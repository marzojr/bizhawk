﻿using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Jellyfish.Virtu
{
	public sealed class Speaker : MachineComponent
	{
		public Speaker() { }
		public Speaker(Machine machine)
			: base(machine)
		{
			_flushOutputEvent = FlushOutputEvent; // cache delegates; avoids garbage
		}

		private const int CyclesPerFlush = 23;

		private Action _flushOutputEvent;

		private bool _isHigh;
		private int _highCycles;
		private int _totalCycles;
		private long _lastCycles;

		[JsonIgnore] // only relevant if trying to savestate mid-frame
		private readonly short[] _buffer = new short[4096];

		[JsonIgnore] // only relevant if trying to savestate mid-frame
		private int _position;

		#region Api

		public void Clear()
		{
			_position = 0;
		}

		public void GetSamples(out short[] samples, out int nSamp)
		{
			samples = _buffer;
			nSamp = _position / 2;
			_position = 0;
		}

		#endregion

		internal override void Initialize()
		{
			Machine.Events.AddEvent(CyclesPerFlush * Machine.Cpu.Multiplier, _flushOutputEvent);
		}

		internal override void Reset()
		{
			_isHigh = false;
			_highCycles = _totalCycles = 0;
		}

		internal void ToggleOutput()
		{
			UpdateCycles();
			_isHigh ^= true;
		}

		private void FlushOutputEvent()
		{
			UpdateCycles();
			// TODO: better than simple decimation here!!
			Output(_highCycles * short.MaxValue / _totalCycles);
			_highCycles = _totalCycles = 0;

			Machine.Events.AddEvent(CyclesPerFlush * Machine.Cpu.Multiplier, _flushOutputEvent);
		}

		private void UpdateCycles()
		{
			int delta = (int)(Machine.Cpu.Cycles - _lastCycles);
			if (_isHigh)
			{
				_highCycles += delta;
			}
			_totalCycles += delta;
			_lastCycles = Machine.Cpu.Cycles;
		}

		private void Output(int data) // machine thread
		{
			data = (int)(data * 0.2);
			if (_position < _buffer.Length - 2)
			{
				_buffer[_position++] = (short)data;
				_buffer[_position++] = (short)data;
			}
		}


		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			_position = 0;
		}
	}
}
