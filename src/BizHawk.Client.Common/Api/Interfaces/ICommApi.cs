﻿#nullable enable

namespace BizHawk.Client.Common
{
	public interface ICommApi : IExternalApi
	{
		HttpCommunication? HTTP { get; }

		MemoryMappedFiles MMF { get; }

		SocketServer? Sockets { get; }

		WebSocketServer? WebSocketServer { get; }

		string? HttpTest();

		string? HttpTestGet();
	}
}
