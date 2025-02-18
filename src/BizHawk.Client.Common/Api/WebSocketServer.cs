#nullable enable

using System.Net;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using BizHawk.Client.Common.Websocket.Messages;
using Newtonsoft.Json;
using BizHawk.Common.CollectionExtensions;
using Newtonsoft.Json.Serialization;

namespace BizHawk.Client.Common
{
	public sealed class WebSocketServer
	{
		private static readonly HashSet<Topic> forcedRegistrationTopics = [ Topic.Error, Topic.Registration ];
		private readonly HttpListener clientRegistrationListener;
		private CancellationToken _cancellationToken = default;
		private bool _running = false;
		private readonly Dictionary<string, WebSocket> clients = [ ];
		private readonly Dictionary<Topic, HashSet<string>> topicRegistrations = [ ];

		/// <param name="host">
		/// 	host address to register for listening to connections, defaults to <see cref="IPAddress.Loopback"/>>
		/// </param>
		/// <param name="port">port to register for listening to connections</param>
		public WebSocketServer(IPAddress? host = null, int port = 3333)
		{
			clientRegistrationListener = new();
			clientRegistrationListener.Prefixes.Add($"http://{host}:{port}/");
		}

		/// <summary>
		/// Stops the server. Alternatively, use the cancellation token passed into <see cref="Start"/>.
		/// The server can be restarted by calling <see cref="Start"/> again.
		/// </summary>
		public void Stop()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = cancellationTokenSource.Token;
			cancellationTokenSource.Cancel();
			_running = false;
		}

		/// <summary>
		/// Starts the websocket server at the configured address and registers clients.
		/// </summary>
		/// <param name="cancellationToken">optional cancellation token to stop the server</param>
		/// <returns>async task for the server loop</returns>
		public async Task Start(CancellationToken cancellationToken = default)
		{
			if (_running)
			{
				throw new InvalidOperationException("Server has already been started");
			}
			_cancellationToken = cancellationToken;
			_running = true;

			clientRegistrationListener.Start();
			await ListenForAndRegisterClients();
		}

		private async Task ListenForAndRegisterClients()
		{
			while (_running && !_cancellationToken.IsCancellationRequested)
			{
				var context = await clientRegistrationListener.GetContextAsync();
				if (context is null) return;

				if (!context.Request.IsWebSocketRequest)
				{
					context.Response.Abort();
					return;
				}

				var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
				if (webSocketContext is null) return;
				RegisterClient(webSocketContext.WebSocket);
			}
		}

		private void RegisterClient(WebSocket newClient)
		{
			string clientId = Guid.NewGuid().ToString();
			clients.Add(clientId, newClient);
			_ = Task.Run(() => ClientMessageReceiveLoop(clientId), _cancellationToken);
		}

		private async Task ClientMessageReceiveLoop(string clientId)
		{
			byte[] buffer = new byte[1024];
			var messageStringBuilder = new StringBuilder(2048);
			var client = clients[clientId];
			while (client.State == WebSocketState.Open && !_cancellationToken.IsCancellationRequested)
			{
				ArraySegment<byte> messageBuffer = new(buffer);
				var receiveResult = await client.ReceiveAsync(messageBuffer, _cancellationToken);
				if (receiveResult.Count == 0)
					return;

				messageStringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, receiveResult.Count));
				if (receiveResult.EndOfMessage)
				{
					string messageString = messageStringBuilder.ToString();
					messageStringBuilder = new StringBuilder(2048);

					try
					{
						var request = JsonSerde.Deserialize<RequestMessageWrapper>(messageString);
						await HandleRequest(clientId, request);
					}
					catch (Exception e)
					{
						// TODO proper logging
						Console.WriteLine("Error deserializing message {0} produced error {1}", messageString, e);
						await SendClientGenericError(clientId);
					}
				}
			}
		}

		private async Task HandleRequest(string clientId, RequestMessageWrapper request)
		{
			try
			{
				switch (request.Topic)
				{
					case Topic.Error:
						// clients arent allowed to publish to this topic
						await SendClientGenericError(clientId);
						break;

					case Topic.Registration:
						await HandleRegistrationRequest(clientId, request.Registration!.Value);
						break;

					case Topic.Echo:
						await HandleEchoRequest(clientId, request.Echo!.Value);
						break;
				}

			}
			catch (Exception e)
			{
				// this could happen if, for instance, the client sent a registration request to the echo topic, such
				// that we tried to access the wrong field of the wrapper
				// TODO proper logging
				Console.WriteLine("Error handling message {0}", e);
				await SendClientGenericError(clientId);
			}
		}

		private async Task HandleRegistrationRequest(string clientId, RegistrationRequestMessage request)
		{
			foreach (Topic topic in Enum.GetValues(typeof(Topic)))
			{
				if (forcedRegistrationTopics.Contains(topic))
				{
					// we dont need to keep track of topics that clients must be registered for.
					continue;
				}
				else if (request.Topics.Contains(topic))
				{
					_ = topicRegistrations.GetValueOrPut(topic, (_) => [ ]).Add(clientId);
				}
				else
				{
					_ = topicRegistrations.GetValueOrDefault(topic, [ ])?.Remove(clientId);
				}
			}

			var registeredTopics = request.Topics;
			registeredTopics.AddRange(forcedRegistrationTopics);
			var responseMessage = new ResponseMessageWrapper(new RegistrationResponseMessage(registeredTopics));
			await SendClientMessage(clientId, responseMessage);
		}

		private async Task HandleEchoRequest(string clientId, EchoRequestMessage request)
		{
			if (topicRegistrations.GetValueOrDefault(Topic.Echo, [ ])?.Contains(clientId) ?? false)
			{
				await SendClientMessage(clientId, new ResponseMessageWrapper(new EchoResponseMessage(request.Message)));
			}
		}

		// clients always get error topics
		private async Task SendClientGenericError(string clientId) => await SendClientMessage(
			clientId, new ResponseMessageWrapper(new ErrorMessage(ErrorType.UnknownRequest))
		); 

		private async Task SendClientMessage(string clientId, object message)
		{
			await clients[clientId].SendAsync(
				JsonSerde.Serialize(message),
				WebSocketMessageType.Text,
				endOfMessage: true,
				_cancellationToken
			);
		}

		private static class JsonSerde
		{

			private static readonly JsonSerializerSettings serializerSettings = new()
			{
				NullValueHandling = NullValueHandling.Ignore,
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
			};

			public static ArraySegment<byte> Serialize(object message) => new(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings)));

			public static T? Deserialize<T>(string message) => JsonConvert.DeserializeObject<T>(message, serializerSettings);
		}
	}
}
