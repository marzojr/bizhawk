using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BizHawk.Emulation.Common.Json
{
	public class FloatConverter : JsonConverter<float>
	{
		public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetSingle();

		public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
		{
#if NETCOREAPP
			writer.WriteNumberValue(value);
#else
			// gotta love the fact .net framework can't even format floats correctly by default
			// can't use G7 here because it may be too low accuracy, and can't use G8 because it may be too high, see 1.0000003f or 0.8f
			writer.WriteRawValue(value.ToString("R", NumberFormatInfo.InvariantInfo));
#endif
		}
	}
}
