#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BizHawk.Client.Common.Websocket.Messages
{
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

		/// <summary>
		/// Topic for valid inputs to send to the emulator, such as button press.
		/// </summary>
		/// <see cref="GetInputOptionsRequestMessage"/>
		/// <see cref="GetInputOptionsResponseMessage"/>
		GetInputOptions = 3,

		/// <summary>
		/// Topic for performing an input such as a button press. 
		/// </summary>
		/// <see cref="InputRequestMessage"/>
		/// <see cref="InputResponseMessage"/>
		Input = 4,

		/// <summary>
		/// Topic for emulator commands like speedup, reset, etc.
		/// </summary>
		/// <see cref="EmulatorCommandRequestMessage"/>
		/// <see cref="EmulatorCommandResponseMessage"/>
		EmulatorCommand = 5,

		/// <summary>
		/// Topic for custom data to send arbitrary JSON data between clients and services not in
		/// the emulator central code (such as through Lua scripts).
		/// </summary>
		Custom = 6
	}
}