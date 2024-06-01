using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BizHawk.Emulation.Common.Json
{
	/// <remarks>based on <see href="https://stackoverflow.com/a/15228384">this SO answer</see></remarks>
	public sealed class ByteArrayAsNormalArrayJsonConverter : JsonConverter<byte[]?>
	{
		public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is JsonTokenType.Null) return null;
			if (reader.TokenType is not JsonTokenType.StartArray) throw new JsonException($"Unexpected token when reading bytes: expected {nameof(JsonTokenType.StartArray)}, got {reader.TokenType}");

			List<byte> list = new();
			while (reader.Read()) switch (reader.TokenType)
			{
				case JsonTokenType.Number:
					list.Add(reader.GetByte());
					continue;
				case JsonTokenType.EndArray:
					return list.ToArray();
				case JsonTokenType.Comment:
					continue;
				default:
					throw new JsonException($"Unexpected token when reading bytes: {reader.TokenType}");
			}
			throw new JsonException("Unexpected end when reading bytes");
		}

		public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
		{
			if (value is null)
			{
				writer.WriteNullValue();
				return;
			}

			writer.WriteStartArray();
			foreach (byte b in value) writer.WriteNumberValue(b);
			writer.WriteEndArray();
		}
	}
}
