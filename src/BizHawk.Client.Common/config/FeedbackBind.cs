#nullable enable

using System.Text.Json.Serialization;

namespace BizHawk.Client.Common
{
	public struct FeedbackBind
	{
		/// <remarks>may be a '+'-delimited list (e.g. <c>"Left+Right"</c>), which will be passed through the input pipeline to <see cref="Controller.PrepareHapticsForHost"/></remarks>
		public string? Channels;

		/// <remarks>"X# "/"J# " (with the trailing space)</remarks>
		public string? GamepadPrefix;

		[JsonIgnore]
		public bool IsZeroed => GamepadPrefix == null;

		public float Prescale;

		public FeedbackBind(string prefix, string channels, float prescale)
		{
			GamepadPrefix = prefix;
			Channels = channels;
			Prescale = prescale;
		}
	}
}
