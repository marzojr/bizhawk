namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Send an input to the current emulator. For valid inputs, <see cref="GetInputOptionsRequestMessage"/>
	/// </summary>
	/// <seealso cref="InputResponseMessage"/>
	public struct InputRequestMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// Whether to clear existing inputs before processing <see cref="Inputs"/> .
		/// </summary>
		public bool ClearInputs { get; set; }

		/// <summary>
		/// Inputs, such as button presses or toggles.
		/// </summary>
		public ButtonInput[] Inputs { get; set; }

		public InputRequestMessage() { }
	}

	public struct ButtonInput
	{
		/// <summary>
		/// Name of the input, e.g. one of the inputs returned in <see cref="GetInputOptionsResponseMessage"/>
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Number of frames to hold the button.
		/// Examples:
		/// <br/>
		/// HoldFrames == 0 : Stop holding the button. If not currently held, this is a no-op.
		/// <br/>
		/// HoldFrames == null : Hold indefinitely. If already held, this is a no-op.
		/// <br/>
		/// HoldFrames == -1 : Toggle hold indefinitely. So, if not held, same as passing null, and if held, same as passing 0.
		/// <br/>
		/// HoldFrames == 3 : Hold for 3 frames and then stop holding.
		/// </summary>
		public int? HoldFrames { get; set; }
	}

	/// <summary>
	/// Result message after sending an input to the <see cref="Topic.Input"/> topic.
	/// </summary>
	/// <seealso cref="InputRequestMessage"/>
	public struct InputResponseMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// Whether the input was successfully applied.
		/// </summary>
		public bool Success { get; set; }

		public InputResponseMessage() { }

		public InputResponseMessage(string requestId, bool success)
		{
			RequestId = requestId;
			Success = success;
		}
	}
}