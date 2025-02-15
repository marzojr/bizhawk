using System.Collections.Generic;

namespace BizHawk.Client.Common.Websocket.Messages
{

	/// <summary>
	/// Requests the available options for controlling the current emulator.
	/// 
	/// Register for <see cref="Topic.GetInputOptions"/>
	/// </summary>
	public struct GetInputOptionsRequestMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		public GetInputOptionsRequestMessage() { }

		public GetInputOptionsRequestMessage(string requestId) {
			RequestId = requestId;
		}
	}

	/// <summary>
	/// Response message after client has sent a <see cref="GetInputOptionsRequestMessage"/>
	/// 
	/// Register for <see cref="Topic.GetInputOptions"/>
	/// </summary>
	public struct GetInputOptionsResponseMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// The set of controls that can be sent in a TBD message to control the current emulator.
		/// </summary>
		public HashSet<string> Inputs { get; set; }

		public GetInputOptionsResponseMessage() { }

		public GetInputOptionsResponseMessage(string requestId, HashSet<string> inputs)
		{
			RequestId = requestId;
			Inputs = inputs;
		}
	}
}