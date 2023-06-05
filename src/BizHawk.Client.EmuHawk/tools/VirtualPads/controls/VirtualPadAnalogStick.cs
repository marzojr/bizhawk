#nullable enable

using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadAnalogStick : UserControl, IVirtualPadControl
	{
		private readonly StickyHoldController _stickyHoldController;
		private bool _readonly;

		private bool _updatingFromAnalog;

		private bool _updatingFromPolar;

		private bool _updatingFromXY;

		private readonly Func<int, int, (short X, short Y)> PolarToRectHelper;

		private readonly Func<int, int, (uint R, uint Θ)> RectToPolarHelper;

		public VirtualPadAnalogStick(
			StickyHoldController stickyHoldController,
			EventHandler setLastFocusedNUD,
			string name,
			string secondaryName,
			AxisSpec rangeX,
			AxisSpec rangeY)
		{
			_stickyHoldController = stickyHoldController;

			RangeX = rangeX;
			RangeY = rangeY;
			if (RangeX.Min >= -128 && RangeX.Max <= 127 && RangeY.Min >= -128 && RangeY.Max <= 127)
			{
				// LUT
				//TODO ditch this on move to .NET Core
				PolarToRectHelper = (r, θ) =>
				{
					var (x, y) = PolarRectConversion.PolarToRectLookup((ushort) r, (ushort) θ);
					var x1 = (RangeX.IsReversed ? RangeX.Neutral - x : RangeX.Neutral + x).ConstrainWithin(RangeX.Range);
					var y1 = (RangeY.IsReversed ? RangeY.Neutral - y : RangeY.Neutral + y).ConstrainWithin(RangeY.Range);
					return ((short) x1, (short) y1);
				};
				RectToPolarHelper = (x, y) => PolarRectConversion.RectToPolarLookup(
					(sbyte) (RangeX.IsReversed ? RangeX.Neutral - x : x - RangeX.Neutral),
					(sbyte) (RangeY.IsReversed ? RangeY.Neutral - y : y - RangeY.Neutral)
				);
			}
			else
			{
				// float math
				const double DEG_TO_RAD_FACTOR = Math.PI / 180;
				const double RAD_TO_DEG_FACTOR = 180 / Math.PI;
				PolarToRectHelper = (r, θ) =>
				{
					var x = (short) (r * Math.Cos(θ * DEG_TO_RAD_FACTOR));
					var y = (short) (r * Math.Sin(θ * DEG_TO_RAD_FACTOR));
					var x1 = (RangeX.IsReversed ? RangeX.Neutral - x : RangeX.Neutral + x).ConstrainWithin(RangeX.Range);
					var y1 = (RangeY.IsReversed ? RangeY.Neutral - y : RangeY.Neutral + y).ConstrainWithin(RangeY.Range);
					return ((short) x1, (short) y1);
				};
				RectToPolarHelper = (x, y) =>
				{
					double x1 = RangeX.IsReversed ? RangeX.Neutral - x : x - RangeX.Neutral;
					double y1 = RangeY.IsReversed ? RangeY.Neutral - y : y - RangeY.Neutral;
					var θ = Math.Atan2(y1, x1) * RAD_TO_DEG_FACTOR;
					return ((uint) Math.Sqrt(x1 * x1 + y1 * y1), (uint) (θ < 0 ? 360.0 + θ : θ));
				};
			}

			InitializeComponent();
			AnalogStick.ClearCallback = ClearCallback;
			manualR.Maximum = Math.Max(RectToPolarHelper(RangeX.Max, RangeY.Max).R, RectToPolarHelper(RangeX.Min, RangeY.Min).R);

			void UnsetLastFocusedNUD(object? sender, EventArgs eventArgs)
				=> setLastFocusedNUD(null, EventArgs.Empty);
			ManualX.ValueChanged += ManualXY_ValueChanged;
			ManualX.GotFocus += setLastFocusedNUD;
			ManualX.LostFocus += UnsetLastFocusedNUD;
			ManualY.ValueChanged += ManualXY_ValueChanged;
			ManualY.GotFocus += setLastFocusedNUD;
			ManualY.LostFocus += UnsetLastFocusedNUD;
			manualR.ValueChanged += PolarNumeric_Changed;
			manualR.GotFocus += setLastFocusedNUD;
			manualR.LostFocus += UnsetLastFocusedNUD;
			manualTheta.ValueChanged += PolarNumeric_Changed;
			manualTheta.GotFocus += setLastFocusedNUD;
			manualTheta.LostFocus += UnsetLastFocusedNUD;
			MaxXNumeric.GotFocus += setLastFocusedNUD;
			MaxXNumeric.LostFocus += UnsetLastFocusedNUD;
			MaxYNumeric.GotFocus += setLastFocusedNUD;
			MaxYNumeric.LostFocus += UnsetLastFocusedNUD;

			AnalogStick.Init(
				stickyHoldController,
				name,
				RangeX,
				string.IsNullOrEmpty(secondaryName) ? Name.Replace('X', 'Y') : secondaryName,
				RangeY
			);

			ManualX.Minimum = RangeX.Min;
			ManualX.Maximum = RangeX.Max;
			ManualY.Minimum = RangeY.Min;
			ManualY.Maximum = RangeY.Max;
			MaxXNumeric.Minimum = 1;
			MaxXNumeric.Maximum = 100;
			MaxYNumeric.Minimum = 1;
			MaxYNumeric.Maximum = 100;

			// these trigger Change events that set the analog stick's values too
			MaxXNumeric.Value = 100;
			MaxYNumeric.Value = 100;
		}

		private readonly AxisSpec RangeX;

		private readonly AxisSpec RangeY;

		public void UpdateValues()
		{
			// Nothing to do
			// This tool already draws as necessary
		}

		public void Set(IController controller)
		{
			AnalogStick.Set(controller);
			SetNumericsFromAnalog();
		}

		public void ClearCallback()
		{
			AnalogStick.Clear(fromCallback: true);
			SetNumericsFromAnalog();
			_stickyHoldController.SetAxisHold(AnalogStick.XName, null);
			_stickyHoldController.SetAxisHold(AnalogStick.YName, null);
		}

		public void Clear() => AnalogStick.Clear();

		public bool ReadOnly
		{
			get => _readonly;
			set
			{
				if (_readonly == value) return;
				XLabel.Enabled =
					ManualX.Enabled =
					YLabel.Enabled =
					ManualY.Enabled =
					MaxLabel.Enabled =
					MaxXNumeric.Enabled =
					MaxYNumeric.Enabled =
					manualR.Enabled =
					rLabel.Enabled =
					manualTheta.Enabled =
					thetaLabel.Enabled =
						!value;
				AnalogStick.ReadOnly =
					_readonly =
						value;
				Refresh();
			}
		}

		public void Bump(int? x, int? y)
		{
			if (x.HasValue)
			{
				AnalogStick.HasValue = true;
				AnalogStick.X += x.Value;
			}

			if (y.HasValue)
			{
				AnalogStick.HasValue = true;
				AnalogStick.Y += y.Value;
			}

			SetNumericsFromAnalog();
		}

		public void SetPrevious(IController previous) => AnalogStick.SetPrevious(previous);

		private void ManualXY_ValueChanged(object? sender, EventArgs e)
		{
			if (_updatingFromAnalog || _updatingFromPolar) return;
			_updatingFromXY = true;

			var x = (int) ManualX.Value;
			var y = (int) ManualY.Value;
			var (r, θ) = RectToPolarHelper(x, y);
			SetAnalog(x, y);
			SetPolar(r, θ);

			_updatingFromXY = false;
		}

		private void MaxManualXY_ValueChanged(object sender, EventArgs e)
			=> AnalogStick.SetUserRange((int) MaxXNumeric.Value, (int) MaxYNumeric.Value);

		private void PolarNumeric_Changed(object? sender, EventArgs e)
		{
			if (_updatingFromAnalog || _updatingFromXY) return;
			_updatingFromPolar = true;

			var (x, y) = PolarToRectHelper((int) manualR.Value, (int) manualTheta.Value);
			SetAnalog(x, y);
			SetXY(x, y);

			_updatingFromPolar = false;
		}

		private void SetAnalog(int x, int y)
		{
			AnalogStick.X = x;
			AnalogStick.Y = y;
			AnalogStick.HasValue = true;
			AnalogStick.Refresh();
		}

		/// <remarks>setting <see cref="NumericUpDown.Value"/> causes a draw, so we avoid it unless necessary</remarks>
		private void SetPolar(decimal r, decimal θ)
		{
			if (manualR.Value != r) manualR.Value = r;
			if (manualTheta.Value != θ) manualTheta.Value = θ;
		}

		/// <inheritdoc cref="SetPolar"/>
		private void SetXY(decimal x, decimal y)
		{
			if (ManualX.Value != x) ManualX.Value = x;
			if (ManualY.Value != y) ManualY.Value = y;
		}

		private void SetNumericsFromAnalog()
		{
			_updatingFromAnalog = true;

			if (AnalogStick.HasValue)
			{
				var x = AnalogStick.X;
				var y = AnalogStick.Y;
				var (r, θ) = RectToPolarHelper(x, y);
				SetPolar(r, θ);
				SetXY(x, y);
			}
			else
			{
				SetPolar(0, 0);
				SetXY(0, 0);
			}

			_updatingFromAnalog = false;
		}

		private void AnalogStick_MouseDown(object sender, MouseEventArgs e)
		{
			if (!ReadOnly) SetNumericsFromAnalog();
		}

		private void AnalogStick_MouseMove(object sender, MouseEventArgs e)
		{
			if (!ReadOnly) SetNumericsFromAnalog();
		}
	}
}
