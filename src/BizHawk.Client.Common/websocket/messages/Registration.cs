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
		/// The set of topics to register for. Previous topics will be unregistered. Providing
		/// an empty set of topics is the same as unregistering for all topics.
		/// </summary>
		public HashSet<Topic> Topics { get; set; }

		public RegistrationRequestMessage() { }

		public RegistrationRequestMessage(HashSet<Topic> topics)
		{
			Topics = topics;
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
		/// The set of topics the client is registered for. Generally should match what was
		/// in the request, plus some topics the client will always receive.
		/// </summary>
		public HashSet<Topic> Topics { get; set; }

		public RegistrationResponseMessage() { }

		public RegistrationResponseMessage(HashSet<Topic> topics)
		{
			Topics = topics;
		}
	}
}