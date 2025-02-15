#nullable enable

namespace BizHawk.Client.Common.Websocket.Messages
{
	public struct RequestMessageWrapper
	{
		public Topic Topic { get; set; }

		public RegistrationRequestMessage? Registration { get; set; }

		public EchoRequestMessage? Echo { get; set; }

		public GetInputOptionsRequestMessage? GetInputOptions { get; set; }

		public InputRequestMessage? Input { get; set; }

		public EmulatorCommandRequestMessage? EmulatorCommand { get; set; }

		public CustomRequestMessage? Custom { get; set; }

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

		public RequestMessageWrapper(GetInputOptionsRequestMessage message)
		{
			Topic = Topic.GetInputOptions;
			GetInputOptions = message;
		}

		public RequestMessageWrapper(InputRequestMessage message)
		{
			Topic = Topic.Input;
			Input = message;
		}

		public RequestMessageWrapper(EmulatorCommandRequestMessage message)
		{
			Topic = Topic.EmulatorCommand;
			EmulatorCommand = message;
		}

		public RequestMessageWrapper(CustomRequestMessage message)
		{
			Topic = Topic.Custom;
			Custom = message;
		}
	}

	public struct ResponseMessageWrapper
	{
		public Topic Topic { get; set; }

		public ErrorMessage? Error { get; set; }

		public RegistrationResponseMessage? Registration { get; set; }

		public EchoResponseMessage? Echo { get; set; }

		public GetInputOptionsResponseMessage? GetInputOptions { get; set; }

		public InputResponseMessage? Input { get; set; }

		public EmulatorCommandResponseMessage? EmulatorCommand { get; set; }

		public CustomResponseMessage? Custom { get; set; }

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

		public ResponseMessageWrapper(GetInputOptionsResponseMessage message)
		{
			Topic = Topic.GetInputOptions;
			GetInputOptions = message;
		}

		public ResponseMessageWrapper(InputResponseMessage message)
		{
			Topic = Topic.Input;
			Input = message;
		}

		public ResponseMessageWrapper(EmulatorCommandResponseMessage message)
		{
			Topic = Topic.EmulatorCommand;
			EmulatorCommand = message;
		}

		public ResponseMessageWrapper(CustomResponseMessage message)
		{
			Topic = Topic.Custom;
			Custom = message;
		}
	}
}