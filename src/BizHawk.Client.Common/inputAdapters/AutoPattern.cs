namespace BizHawk.Client.Common
{
	public class AutoPatternBool
	{
		public AutoPatternBool()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AutoPatternBool"/> class.
		/// A simple on/off pattern.
		/// <paramref name="on"/> number of frames to return true in each iteration
		/// <paramref name="off"/> number of frames to return false in each iteration
		/// <paramref name="loop"/> where to start in the on/off pattern on each iteration after the first iteration
		/// <paramref name="iterations"/> number of iterations to run before always returning false, set to -1 for indefinite
		/// </summary>
		public AutoPatternBool(int on, int off, bool skipLag = true, int offset = 0, int loop = 0, int iterations = -1)
		{
			SkipsLag = skipLag;
			_index = offset;
			Pattern = new bool[on + off];
			Loop = loop;
			Iterations = iterations;
			for (int i = 0; i < on; i++)
			{
				Pattern[i] = true;
			}
		}

		public AutoPatternBool(bool[] pattern, bool skipLag = true, int offset = 0, int loop = 0, int iterations = -1)
		{
			SkipsLag = skipLag;
			Pattern = pattern;
			_index = offset;
			Loop = loop;
			Iterations = iterations;
		}

		private int _index;

		private int _iteration;

		public int Iterations { get; }

		public bool SkipsLag { get; } = true;
		public bool[] Pattern { get; } = { true };
		public int Loop { get; }

		/// <returns>
		/// true if this will always return false after some 
		/// number of iterations of the full pattern
		/// </returns>
		public bool IsFinite() => Iterations != -1;

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		public bool GetNextValue(bool isLag = false)
		{
			if (IsFinite() && (_iteration >= Iterations))
			{
				return false;
			}

			bool ret = Pattern[_index];
			if (!isLag || !SkipsLag)
			{
				_index++;
				if (_index == Pattern.Length)
				{
					_index = Loop;
					_iteration++;
				}
			}

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		public bool PeekNextValue()
		{
			if (IsFinite() && (_iteration >= Iterations)) {
				return false;
			}
			return Pattern[_index];
		}

		public void Reset()
		{
			_index = 0;
			_iteration = 0;
		}
	}

	public class AutoPatternAxis
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AutoPatternAxis"/> class.
		/// Defaults to 0.
		/// </summary>
		public AutoPatternAxis()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AutoPatternAxis"/> class.
		/// Simple on/off pattern, using the given values as on/off.
		/// </summary>
		public AutoPatternAxis(int valueOn, int on, int valueOff, int off, bool skipLag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skipLag;
			_index = offset;
			Loop = loop;
			Pattern = new int[on + off];
			for (int i = 0; i < on; i++)
			{
				Pattern[i] = valueOn;
			}

			for (int i = on; i < Pattern.Length; i++)
			{
				Pattern[i] = valueOff;
			}
		}

		public AutoPatternAxis(int[] pattern, bool skipLag = true, int offset = 0, int loop = 0)
		{
			SkipsLag = skipLag;
			Pattern = pattern;
			_index = offset;
			Loop = loop;
		}

		private int _index;

		public bool SkipsLag { get; } = true;
		public int[] Pattern { get; } = { 0 };
		public int Loop { get; }

		/// <summary>
		/// Gets the next value and increments index.
		/// </summary>
		public int GetNextValue(bool isLag = false)
		{
			int ret = Pattern[_index];
			if (!isLag || !SkipsLag)
			{
				_index++;
				if (_index == Pattern.Length)
				{
					_index = Loop;
				}
			}

			return ret;
		}

		/// <summary>
		/// Gets the next value without incrementing index.
		/// </summary>
		public int PeekNextValue()
		{
			return Pattern[_index];
		}

		public void Reset()
		{
			_index = 0;
		}
	}
}
