namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Test message for clients to use to confirm messages are passed back and forth.
	/// 
	/// Register for <see cref="Topic.Echo"/>
	/// </summary>
	/// <seealso cref="EchoResponseMessage"/>
	public struct EchoRequestMessage
	{

		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }
		
		/// <summary>
		/// Message body. Will be the same in the response.
		/// </summary>
		public string Message { get; set; }

		public EchoRequestMessage() { }

		public EchoRequestMessage(string requestId, string message)
		{
			RequestId = requestId;
			Message = message;
		}
	}

	/// <summary>
	/// Test message for clients to use to confirm messages are passed back and forth.
	/// 
	/// Register for <see cref="Topic.Echo"/>
	/// </summary>
	/// <seealso cref="EchoRequestMessage"/>
	public struct EchoResponseMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }
		
		/// <summary>
		/// Message body. Same as was in the request.
		/// </summary>
		public string Message { get; set; }

		public EchoResponseMessage() { }

		public EchoResponseMessage(string requestId, string message)
		{
			RequestId = requestId;
			Message = message;
		}
	}
}