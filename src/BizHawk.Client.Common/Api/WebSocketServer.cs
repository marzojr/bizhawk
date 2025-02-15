#nullable enable

namespace BizHawk.Client.Common
{
	public sealed class WebSocketServer
	{
		public ClientWebSocketWrapper Open(Uri uri) => new(uri);
	}
}
