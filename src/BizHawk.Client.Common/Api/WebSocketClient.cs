#nullable enable

namespace BizHawk.Client.Common
{
	public sealed class WebSocketClient
	{
		public ClientWebSocketWrapper Open(Uri uri) => new(uri);
	}
}
