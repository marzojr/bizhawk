#nullable enable

using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BizHawk.Client.Common
{
	public struct ClientWebSocketWrapper
	{
		private ClientWebSocket? _w;

		/// <summary>calls <see cref="ClientWebSocket.State"/> getter (unless closed/disposed, then <see cref="WebSocketState.Closed"/> is always returned)</summary>
		public WebSocketState State => _w?.State ?? WebSocketState.Closed;

		public ClientWebSocketWrapper(
			Uri uri,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
		{
			_w = new ClientWebSocket();
			_w.ConnectAsync(uri, cancellationToken).Wait(cancellationToken);
		}

		/// <summary>calls <see cref="ClientWebSocket.CloseAsync"/></summary>
		/// <remarks>also calls <see cref="ClientWebSocket.Dispose"/> (wrapper property <see cref="State"/> will continue to work, method calls will throw <see cref="ObjectDisposedException"/>)</remarks>
		public Task Close(
			WebSocketCloseStatus closeStatus,
			string statusDescription,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			var task = _w.CloseAsync(closeStatus, statusDescription, cancellationToken);
			_w.Dispose();
			_w = null;
			return task;
		}

		/// <summary>calls <see cref="ClientWebSocket.ReceiveAsync(System.ArraySegment{byte}, CancellationToken)"/></summary>
		public Task<WebSocketReceiveResult> Receive(
			ArraySegment<byte> buffer,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
				=> _w?.ReceiveAsync(buffer, cancellationToken)
					?? throw new ObjectDisposedException(nameof(_w));

		/// <summary>calls <see cref="ClientWebSocket.ReceiveAsync(System.ArraySegment{byte}, CancellationToken)"/></summary>
		public string Receive(
			int bufferCap,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			var buffer = new byte[bufferCap];
			var result = Receive(new ArraySegment<byte>(buffer), cancellationToken).Result;
			return Encoding.UTF8.GetString(buffer, 0, result.Count);
		}

		/// <summary>calls <see cref="ClientWebSocket.SendAsync(System.ArraySegment{byte}, WebSocketMessageType, bool, CancellationToken)"/></summary>
		public Task Send(
			ArraySegment<byte> buffer,
			WebSocketMessageType messageType,
			bool endOfMessage,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
				=> _w?.SendAsync(buffer, messageType, endOfMessage, cancellationToken)
					?? throw new ObjectDisposedException(nameof(_w));

		/// <summary>calls <see cref="ClientWebSocket.SendAsync(System.ArraySegment{byte}, WebSocketMessageType, bool, CancellationToken)"/></summary>
		public Task Send(
			string message,
			bool endOfMessage,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			return Send(
				new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
				WebSocketMessageType.Text,
				endOfMessage,
				cancellationToken
			);
		}
	}
}
