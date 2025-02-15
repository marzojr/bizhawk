using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Client.Common.Websocket;
using BizHawk.Client.Common.Websocket.Messages;
using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for communicating with other programs")]
	public sealed class CommLuaLibrary : LuaLibraryBase
	{
		public CommLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "comm";

		//TO DO: not fully working yet!
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		public static string GetLuaFunctionsList()
		{
			var list = new StringBuilder();
			foreach (var function in typeof(CommLuaLibrary).GetMethods())
			{
				list.AppendLine(function.ToString());
			}
			return list.ToString();
		}

		[LuaMethod("socketServerIsConnected", "socketServerIsConnected")]
		public bool SocketServerIsConnected()
			=> APIs.Comm.Sockets.Connected;

		[LuaMethod("socketServerScreenShot", "sends a screenshot to the Socket server")]
		public string SocketServerScreenShot()
		{
			CheckSocketServer();
			return APIs.Comm.Sockets?.SendScreenshot();
		}

		[LuaMethod("socketServerScreenShotResponse", "sends a screenshot to the Socket server and retrieves the response")]
		public string SocketServerScreenShotResponse()
		{
			CheckSocketServer();
			return APIs.Comm.Sockets?.SendScreenshot(1000);
		}

		[LuaMethod("socketServerSend", "sends a string to the Socket server")]
		public int SocketServerSend(string SendString)
		{
			if (!CheckSocketServer())
			{
				return -1;
			}
			return APIs.Comm.Sockets.SendString(SendString);
		}

		[LuaMethod("socketServerSendBytes", "sends bytes to the Socket server")]
		public int SocketServerSendBytes(LuaTable byteArray)
		{
			if (!CheckSocketServer()) return -1;
			return APIs.Comm.Sockets.SendBytes(_th.EnumerateValues<long>(byteArray).Select(l => (byte) l).ToArray());
		}

		[LuaMethod("socketServerResponse", "Receives a message from the Socket server. Since BizHawk 2.6.2, all responses must be of the form $\"{msg.Length:D} {msg}\" i.e. prefixed with the length in base-10 and a space.")]
		public string SocketServerResponse()
		{
			CheckSocketServer();
			return APIs.Comm.Sockets?.ReceiveString();
		}

		[LuaMethod("socketServerSuccessful", "returns the status of the last Socket server action")]
		public bool SocketServerSuccessful()
		{
			return CheckSocketServer() && APIs.Comm.Sockets.Successful;
		}

		[LuaMethod("socketServerSetTimeout", "sets the timeout in milliseconds for receiving messages")]
		public void SocketServerSetTimeout(int timeout)
		{
			CheckSocketServer();
			APIs.Comm.Sockets?.SetTimeout(timeout);
		}

		[LuaMethod("socketServerSetIp", "sets the IP address of the Lua socket server")]
		public void SocketServerSetIp(string ip)
		{
			CheckSocketServer();
			APIs.Comm.Sockets.IP = ip;
		}

		[LuaMethod("socketServerSetPort", "sets the port of the Lua socket server")]
		public void SocketServerSetPort(ushort port)
		{
			CheckSocketServer();
			APIs.Comm.Sockets.Port = port;
		}

		[LuaMethod("socketServerGetIp", "returns the IP address of the Lua socket server")]
		public string SocketServerGetIp()
		{
			return APIs.Comm.Sockets?.IP;
		}

		[LuaMethod("socketServerGetPort", "returns the port of the Lua socket server")]
		public int? SocketServerGetPort()
		{
			return APIs.Comm.Sockets?.Port;
		}

		[LuaMethod("socketServerGetInfo", "returns the IP and port of the Lua socket server")]
		public string SocketServerGetInfo()
		{
			return CheckSocketServer()
				? APIs.Comm.Sockets.GetInfo()
				: "";
		}

		private bool CheckSocketServer()
		{
			if (APIs.Comm.Sockets == null)
			{
				Log("Socket server was not initialized, please initialize it via the command line");
				return false;
			}

			return true;
		}

		// All MemoryMappedFile related methods
		[LuaMethod("mmfSetFilename", "Sets the filename for the screenshots")]
		public void MmfSetFilename(string filename)
			=> APIs.Comm.MMF.Filename = filename;

		[LuaMethod("mmfGetFilename", "Gets the filename for the screenshots")]
		public string MmfGetFilename()
			=> APIs.Comm.MMF.Filename;

		[LuaMethod("mmfScreenshot", "Saves screenshot to memory mapped file")]
		public int MmfScreenshot()
			=> APIs.Comm.MMF.ScreenShotToFile();

		[LuaMethod("mmfWrite", "Writes a string to a memory mapped file")]
		public int MmfWrite(string mmf_filename, string outputString)
			=> APIs.Comm.MMF.WriteToFile(mmf_filename, outputString);

		[LuaMethod("mmfWriteBytes", "Write bytes to a memory mapped file")]
		public int MmfWriteBytes(string mmf_filename, LuaTable byteArray)
			=> APIs.Comm.MMF.WriteToFile(mmf_filename, _th.EnumerateValues<long>(byteArray).Select(l => (byte) l).ToArray());

		[LuaMethod("mmfCopyFromMemory", "Copy a section of the memory to a memory mapped file")]
		public int MmfCopyFromMemory(
			string mmf_filename,
			long addr,
			int length,
			string domain)
				=> APIs.Comm.MMF.WriteToFile(mmf_filename, APIs.Memory.ReadByteRange(addr, length, domain).ToArray());

		[LuaMethod("mmfCopyToMemory", "Copy a memory mapped file to a section of the memory")]
		public void MmfCopyToMemory(
			string mmf_filename,
			long addr,
			int length,
			string domain)
				=> APIs.Memory.WriteByteRange(addr, new List<byte>(APIs.Comm.MMF.ReadBytesFromFile(mmf_filename, length)), domain);

		[LuaMethod("mmfRead", "Reads a string from a memory mapped file")]
		public string MmfRead(string mmf_filename, int expectedSize)
			=> APIs.Comm.MMF.ReadFromFile(mmf_filename, expectedSize);

		[LuaMethod("mmfReadBytes", "Reads bytes from a memory mapped file")]
		public LuaTable MmfReadBytes(string mmf_filename, int expectedSize)
			=> _th.ListToTable(APIs.Comm.MMF.ReadBytesFromFile(mmf_filename, expectedSize), indexFrom: 0);

		// All HTTP related methods
		[LuaMethod("httpTest", "tests HTTP connections")]
		public string HttpTest()
		{
			_ = APIs.Comm.HTTP!; // to match previous behaviour
			return APIs.Comm.HttpTest();
		}

		[LuaMethod("httpTestGet", "tests the HTTP GET connection")]
		public string HttpTestGet()
		{
			CheckHttp();
			return APIs.Comm.HttpTestGet();
		}

		[LuaMethod("httpGet", "makes a HTTP GET request")]
		public string HttpGet(string url)
		{
			CheckHttp();
			return APIs.Comm.HTTP?.ExecGet(url);
		}

		[LuaMethod("httpPost", "makes a HTTP POST request")]
		public string HttpPost(string url, string payload)
		{
			CheckHttp();
			return APIs.Comm.HTTP?.ExecPostAsForm(url: url, payload: payload);
		}

		[LuaMethod("httpPostScreenshot", "HTTP POST screenshot")]
		public string HttpPostScreenshot()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.SendScreenshot();
		}

		[LuaMethod("httpSetTimeout", "Sets HTTP timeout in milliseconds")]
		public void HttpSetTimeout(int timeout)
		{
			CheckHttp();
			APIs.Comm.HTTP?.SetTimeout(timeout);
		}

		[LuaMethod("httpSetPostUrl", "Sets HTTP POST URL")]
		public void HttpSetPostUrl(string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.PostUrl = url;
		}

		[LuaMethod("httpSetGetUrl", "Sets HTTP GET URL")]
		public void HttpSetGetUrl(string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.GetUrl = url;
		}

		[LuaMethod("httpGetPostUrl", "Gets HTTP POST URL")]
		public string HttpGetPostUrl()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.PostUrl;
		}

		[LuaMethod("httpGetGetUrl", "Gets HTTP GET URL")]
		public string HttpGetGetUrl()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.GetUrl;
		}

		[LuaMethodExample("comm.wss_topic( \"my-custom-topic\" );")]
		[LuaMethod("wss_topic", "Registers a custom topic for broadcast messages, which allows clients to register.")]
		public void RegisterCustomWebSocketServerTopic(string customTopic)
		{
			if (WssEnabled())
			{
				APIs.Comm.WebSocketServer!.RegisterCustomBroadcastTopic(customTopic);
			}
		}

		[LuaMethodExample("comm.wss_send( \"{\\\"topic\\\": \\\"Echo\\\", \\\"Echo\\\": {\\\"requestId\\\": \\\"abcd\\\", \\\"message\\\": \\\"hello, world\\\"}}\" );")]
		[LuaMethod("wss_send", "Broadcasts a message over the websocket server to registered clients. Message contents must be a valid [ResponseMessageWrapper] JSON string with camelCase properties.")]
		public async Task WebSocketServerSend(string messageJson)
		{
			if (WssEnabled())
			{
				ResponseMessageWrapper? deserializedMessage = null;
				try
				{
					deserializedMessage = JsonSerde.Deserialize<ResponseMessageWrapper>(messageJson);
				}
				catch
				{
					Log("Invalid message, must be of type ResponseMessageWrapper");
				}
				if (deserializedMessage != null)
				{
					await APIs.Comm.WebSocketServer!.BroadcastMessage(deserializedMessage.Value);
				}
			}
		}

		[LuaMethodExample("local my_handler = comm.wss_custom_handler(\"my-topic\", \r\n\tfunction(message)\r\n\tconsole.log(message);\r\n\tend);")]
		[LuaMethod("wss_custom_handler", "Registers a custom handler that is called when there's a request on the provided custom topic. The message will always be a JSON string encoded [RequestMessageWrapper], and the response must be a JSON string encode [ResponseMessageWrapper].")]
		public void RegisterWebSocketServerCustomHandler(string customTopic, LuaFunction luaf)
		{
			if (WssEnabled())
			{
				Task<ResponseMessageWrapper?> wrappedHandler(RequestMessageWrapper request)
				{
					string responseString = "";
					// TODO I have no idea what i'm doing here. If it works then it's magic.
					// I ripped it from [LuaWinForm.DoLuaEvent]
					LuaSandbox.Sandbox(null, () =>
					{
						object[] response = luaf.Call(JsonSerde.SerializeToString(request));
						if (response.Length > 0)
						{
							responseString = (string) response[0];
						}
					});

					if (responseString != "")
					{
						var result = JsonSerde.Deserialize<ResponseMessageWrapper>(responseString);
						return Task.FromResult<ResponseMessageWrapper?>(result);
					}
					else
					{
						return Task.FromResult<ResponseMessageWrapper?>(null);
					}
				}
				APIs.Comm.WebSocketServer!.RegisterCustomHandler(customTopic, wrappedHandler);
			}
		}

		private void CheckHttp()
		{
			if (APIs.Comm.HTTP == null)
			{
				Log("HTTP was not initialized, please initialize it via the command line");
			}
		}

		private bool WssEnabled() {
			bool wssEnabled = APIs.Comm.WebSocketServer != null;
			if (!wssEnabled)
			{
				Log("WebSocket Server was not initialized, please initialize it via the command line");
			}
			return wssEnabled;
		}
	}
}