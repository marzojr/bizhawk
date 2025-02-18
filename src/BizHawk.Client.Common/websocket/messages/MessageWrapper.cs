#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BizHawk.Client.Common.Websocket.Messages
{
	public struct RequestMessageWrapper
	{
		public Topic Topic { get; set; }

		public RegistrationRequestMessage? Registration { get; set; }

		public EchoRequestMessage? Echo { get; set; }

		public RequestMessageWrapper() { }

		public RequestMessageWrapper(RegistrationRequestMessage message)
		{
			Topic = Topic.Registration;
			Registration = message;
		}

		public RequestMessageWrapper(EchoRequestMessage message)
		{
			Topic = Topic.Echo;
			Echo = message;
		}
	}

	public struct ResponseMessageWrapper
	{
		public Topic Topic { get; set; }

		public ErrorMessage? Error { get; set; }

		public RegistrationResponseMessage? Registration { get; set; }

		public EchoResponseMessage? Echo { get; set; }

		public ResponseMessageWrapper() { }

		public ResponseMessageWrapper(ErrorMessage message)
		{
			Topic = Topic.Error;
			Error = message;
		}

		public ResponseMessageWrapper(RegistrationResponseMessage message)
		{
			Topic = Topic.Registration;
			Registration = message;
		}

		public ResponseMessageWrapper(EchoResponseMessage message)
		{
			Topic = Topic.Echo;
			Echo = message;
		}
	}

	[JsonConverter(typeof(StringEnumConverter))]
	public enum Topic : ushort
	{
		/// <summary>
		/// Topic for general errors. Some errors may be client-specific, such as when
		/// a request was made that could not be assigned to a topic.
		/// </summary>
		Error = 0,

		/// <summary>
		/// Topic for registration requests.
		/// </summary>
		/// <see cref="RegistrationRequestMessage"/>
		/// <see cref="RegistrationResponseMessage"/>
		Registration = 1,

		/// <summary>
		/// This is a client-specific topic, i.e. only the client who sent the message
		/// will receive the echoed response.
		/// </summary>
		/// <see cref="EchoRequestMessage"/>
		/// <see cref="EchoResponseMessage"/>
		Echo = 2,
	}
}