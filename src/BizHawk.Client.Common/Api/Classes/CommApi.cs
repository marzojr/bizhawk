#nullable enable

namespace BizHawk.Client.Common
{
	public sealed class CommApi : ICommApi
	{
		private static readonly WebSocketClient _wsClient = new();

		private readonly (HttpCommunication? HTTP, MemoryMappedFiles MMF, SocketServer? Sockets, WebSocketServer? WebSocketServer) _networkingHelpers;

		public HttpCommunication? HTTP => _networkingHelpers.HTTP;

		public MemoryMappedFiles MMF => _networkingHelpers.MMF;

		public SocketServer? Sockets => _networkingHelpers.Sockets;

		public WebSocketClient WebSockets => _wsClient;

		public WebSocketServer? WebSocketServer => _networkingHelpers.WebSocketServer;

		public CommApi(IMainFormForApi mainForm) => _networkingHelpers = mainForm.NetworkingHelpers;

		public string? HttpTest() => HTTP == null ? null : string.Join("\n", HttpTestGet(), HTTP.SendScreenshot(), "done testing");

		public string? HttpTestGet() => HTTP?.Get(HTTP.GetUrl)?.Result;
	}
}
