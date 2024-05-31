using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BizHawk.Emulation.Common
{
	// heavily inspired by https://stackoverflow.com/questions/66280645/how-can-i-serialize-a-double-2d-array-to-json-using-system-text-json
	public class Array2DJsonConverter<T> : JsonConverter<T[,]?>
	{
		public override void Write(Utf8JsonWriter writer, T[,]? array, JsonSerializerOptions options)
		{
			if (array is null)
			{
				writer.WriteNullValue();
				return;
			}

			int rowsFirstIndex = array.GetLowerBound(0);
			int rowsLastIndex = array.GetUpperBound(0);
			var arrayConverter = (JsonConverter<T[]>)options.GetConverter(typeof(T[]));

			writer.WriteStartArray();
			for (int i = rowsFirstIndex; i <= rowsLastIndex; i++)
			{
				arrayConverter.Write(writer, array.SliceRow(i).ToArray(), options);
			}
			writer.WriteEndArray();
		}

		public override T[,]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is JsonTokenType.Null) return null;
			if (reader.TokenType is not JsonTokenType.StartArray)
				throw new JsonException($"Unexpected token when reading bytes: expected {nameof(JsonTokenType.StartArray)}, got {reader.TokenType}");

			var listConverter = (JsonConverter<T[]>)options.GetConverter(typeof(T[]));
			List<T[]> fullList = new();
			while (reader.Read())
				switch (reader.TokenType)
				{
					// handle both base64 string and array formats here and trust the converter to handle it
					case JsonTokenType.String:
					case JsonTokenType.StartArray:
						var result = listConverter.Read(ref reader, typeof(T[]), options)!;
						fullList.Add(result);
						continue;
					case JsonTokenType.EndArray:
						return fullList.To2D();
					case JsonTokenType.Comment:
						continue;
					default:
						throw new JsonException($"Unexpected token when reading bytes: {reader.TokenType}");
				}

			throw new JsonException("Unexpected end when reading bytes");
		}
	}

	internal static class ArrayExtensions
	{
		public static T[,] To2D<T>(this IList<T[]> source)
		{
			int firstDimension = source.Count;
			int secondDimension = source.FirstOrDefault()?.Length ?? 0;

			// sanity check; the input must consist of arrays of the same size
			if (source.Any(row => row.Length != secondDimension))
				throw new InvalidOperationException();

			var result = new T[firstDimension, secondDimension];
			for (int i = 0; i < firstDimension; i++)
			for (int j = 0; j < source[i].Length; j++)
				result[i, j] = source[i][j];

			return result;
		}

		public static IEnumerable<T> SliceRow<T>(this T[,] array, int row)
		{
			for (int i = array.GetLowerBound(1); i <= array.GetUpperBound(1); i++)
			{
				yield return array[row, i];
			}
		}
	}
}
