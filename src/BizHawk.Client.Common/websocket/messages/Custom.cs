#nullable enable

namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Request message for custom subtopics coming from e.g. lua scripts.
	/// 
	/// Register for <see cref="Topic.Custom"/>
	/// </summary>
	/// <seealso cref="CustomResponseMessage"/>
	public struct CustomRequestMessage
	{

		/// <summary>
		/// Optional request ID, which the websocket server will forward on to custom handlers
		/// </summary>
		public string? RequestId { get; set; }

		/// <summary>
		/// Sub-topic defined by the custom handler.
		/// </summary>
		public string SubTopic { get; set; }
		
		/// <summary>
		/// Message body. Should be a valid string representation of whatever data are needed by
		/// the custom handler, such as a JSON-encoded object (which would be double-encoded when serialized).
		/// </summary>
		public string Message { get; set; }

		public CustomRequestMessage() { }

		public CustomRequestMessage(string? requestId, string subTopic, string message)
		{
			RequestId = requestId;
			SubTopic = subTopic;
			Message = message;
		}
	}

	/// <summary>
	/// Response message for custom subtopics coming from e.g. lua scripts.
	/// 
	/// Register for <see cref="Topic.Custom"/>
	/// </summary>
	/// <seealso cref="CustomRequestMessage"/>
	public struct CustomResponseMessage
	{

		/// <summary>
		/// Optional request ID, which a custom handler may use to identify responses to clients
		/// </summary>
		public string? RequestId { get; set; }

		/// <summary>
		/// Sub-topic defined by the custom handler.
		/// </summary>
		public string SubTopic { get; set; }
		
		/// <summary>
		/// Message body. Should be a valid string representation of whatever data are needed by
		/// the client, such as a JSON-encoded object (which would be double-encoded when serialized).
		/// </summary>
		public string Message { get; set; }

		public CustomResponseMessage() { }

		public CustomResponseMessage(string? requestId, string subTopic, string message)
		{
			RequestId = requestId;
			SubTopic = subTopic;
			Message = message;
		}
	}
}