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
		/// Message body. Will be the same in the response.
		/// </summary>
		public string Message { get; set; }

		public EchoRequestMessage() { }

		public EchoRequestMessage(string message)
		{
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
		/// Message body. Same as was in the request.
		/// </summary>
		public string Message { get; set; }

		public EchoResponseMessage() { }

		public EchoResponseMessage(string message)
		{
			Message = message;
		}
	}
}