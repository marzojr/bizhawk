#nullable enable

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BizHawk.Client.Common.Websocket
{
	public static class JsonSerde
	{

		private static readonly JsonSerializerSettings serializerSettings = new()
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
		};

		public static ArraySegment<byte> Serialize(object message) => 
			new(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings)));

		public static string SerializeToString(object message) =>
			JsonConvert.SerializeObject(message, serializerSettings);

		public static T? Deserialize<T>(string message) => 
			JsonConvert.DeserializeObject<T>(message, serializerSettings);
	}
}