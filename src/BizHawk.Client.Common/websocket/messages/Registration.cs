using System.Collections.Generic;

namespace BizHawk.Client.Common.Websocket.Messages
{

	/// <summary>
	/// Defines the set of topics a client wishes to register to listen to.
	/// 
	/// Register for <see cref="Topic.Registration"/>
	/// </summary>
	public struct RegistrationRequestMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// The set of topics to register for. Previous topics will be unregistered. Providing
		/// an empty set of topics is the same as unregistering for all topics.
		/// </summary>
		public HashSet<Topic> Topics { get; set; }

		/// <summary>
		/// Set of subtopics within the <see cref="Topic.Custom"/> to subscribe to. Providing an empty
		/// set of subtopics is the same as unregistering for all subtopics. You must also be registered
		/// for the <see cref="Topic.Custom"/> topic to receive these.
		/// </summary>
		public HashSet<string> CustomTopics { get; set; }

		public RegistrationRequestMessage() { }

		public RegistrationRequestMessage(string requestId, HashSet<Topic> topics, HashSet<string> customTopics)
		{
			RequestId = requestId;
			Topics = topics;
			CustomTopics = customTopics;
		}
	}

	/// <summary>
	/// Response message after client has sent a <see cref="RegistrationRequestMessage"/>
	/// 
	/// Register for <see cref="Topic.Registration"/>
	/// </summary>
	public struct RegistrationResponseMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// The set of topics the client is registered for. Generally should match what was
		/// in the request, plus some topics the client will always receive.
		/// </summary>
		public HashSet<Topic> Topics { get; set; }

		/// <summary>
		/// Set of subtopics within the <see cref="Topic.Custom"/> to the client is registered for. Generally
		/// should match what was in the request.
		/// </summary>
		public HashSet<string> CustomTopics { get; set; }

		public RegistrationResponseMessage() { }

		public RegistrationResponseMessage(string requestId, HashSet<Topic> topics, HashSet<string> customTopics)
		{
			RequestId = requestId;
			Topics = topics;
			CustomTopics = customTopics;
		}
	}
}