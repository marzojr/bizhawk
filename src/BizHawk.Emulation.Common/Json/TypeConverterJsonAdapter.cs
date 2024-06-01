using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// System.Text.Json does not respect `[TypeConverter]`s by default, so it's required to use a JsonConverter that acts as an adapter
// see also https://github.com/dotnet/runtime/issues/38812 or https://github.com/dotnet/runtime/issues/1761
namespace BizHawk.Emulation.Common.Json
{
	/// <summary>
	/// Adapter between <see cref="TypeConverter"/> and <see cref="JsonConverter"/>.
	/// </summary>
	public class TypeConverterJsonAdapter<T> : JsonConverter<T>
	{
		/// <inheritdoc/>
		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var converter = TypeDescriptor.GetConverter(typeToConvert);
			return (T)converter.ConvertFromString(reader.GetString())!;
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, T objectToWrite, JsonSerializerOptions options)
		{
			var converter = TypeDescriptor.GetConverter(objectToWrite!);
			writer.WriteStringValue(converter.ConvertToString(objectToWrite!));
		}

		/// <inheritdoc/>
		public override bool CanConvert(Type typeToConvert) => typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();
	}

	/// <inheritdoc />
	public class TypeConverterJsonAdapter : TypeConverterJsonAdapter<object>;

	/// <summary>
	/// A factory used to create various <see cref="TypeConverterJsonAdapter{T}"/> instances.
	/// </summary>
	public class TypeConverterJsonAdapterFactory : JsonConverterFactory
	{
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert) => typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();

		/// <inheritdoc />
		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			var converterType = typeof(TypeConverterJsonAdapter<>).MakeGenericType(typeToConvert);
			return (JsonConverter)Activator.CreateInstance(converterType);
		}
	}
}
