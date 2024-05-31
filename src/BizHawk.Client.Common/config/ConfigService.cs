using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class FloatConverter : JsonConverter<float>
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

	public static class ConfigService
	{
		private static readonly JsonSerializerOptions NonIndentedSerializerOptions = new()
		{
			IncludeFields = true,
			AllowTrailingCommas = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			Converters =
			{
				new FloatConverter(), // this serializes floats with minimum required precision, e.g. 1.8000000012 -> 1.8
				new ByteArrayAsNormalArrayJsonConverter() // this preserves the old behaviour of e,g, 0x1234ABCD --> [18,52,171,205]; omitting it will use base64 e.g. "EjSrzQ=="
			}
		};

		private static readonly JsonSerializerOptions IndentedSerializerOptions = new(NonIndentedSerializerOptions)
		{
			WriteIndented = true
		};

		internal static JsonSerializerOptions SerializerOptions => NonIndentedSerializerOptions;

		public static bool IsFromSameVersion(string filepath, out string msg)
		{
			const string MSGFMT_NEWER = "Your config file ({0}) is from a newer version of EmuHawk, {2} (this is {1}). It may fail to load.";
			const string MSGFMT_OLDER = "Your config file ({0}) is from an older version of EmuHawk, {2} (this is {1}). It may fail to load.";
			const string MSGFMT_PRE_2_3_3 = "Your config file ({0}) is corrupted, or is from an older version of EmuHawk, predating 2.3.3 (this is {1}). It may fail to load.";
			const string MSGFMT_PRE_2_5 = "Your config file ({0}) is corrupted, or is from an older version of EmuHawk, predating 2.5 (this is {1}). It may fail to load.";

			if (!File.Exists(filepath))
			{
				msg = null;
				return true;
			}
			string cfgVersionStr = null;
			try
			{
				cfgVersionStr = JsonNode.Parse(File.ReadAllText(filepath))["LastWrittenFrom"]?.GetValue<string>();
			}
			catch (Exception)
			{
				// ignored
			}
			if (cfgVersionStr == VersionInfo.MainVersion)
			{
				msg = null;
				return true;
			}
			string fmt;
			if (cfgVersionStr == null)
			{
				fmt = MSGFMT_PRE_2_3_3;
			}
			else
			{
				var cfgVersion = VersionInfo.VersionStrToInt(cfgVersionStr);
				if (cfgVersion < 0x02050000U)
				{
					fmt = MSGFMT_PRE_2_5;
				}
				else
				{
					var thisVersion = VersionInfo.VersionStrToInt(VersionInfo.MainVersion);
					fmt = cfgVersion < thisVersion ? MSGFMT_OLDER : MSGFMT_NEWER;
				}
			}
			msg = string.Format(fmt, Path.GetFileName(filepath), VersionInfo.MainVersion, cfgVersionStr);
			return false;
		}

		/// <exception cref="InvalidOperationException">internal error</exception>
		public static T Load<T>(string filepath) where T : new()
		{
			T config = default;

			try
			{
				var file = new FileInfo(filepath);
				if (file.Exists)
				{
					using var reader = file.OpenRead();
					config = JsonSerializer.Deserialize<T>(reader, IndentedSerializerOptions);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Config Error", ex);
			}

			return config ?? new T();
		}

		public static void Save(string filepath, object config)
		{
			try
			{
				using var writer = File.Create(filepath);
				JsonSerializer.Serialize(writer, config, IndentedSerializerOptions);
			}
			catch
			{
				/* Eat it */
			}
		}

		// movie 1.0 header stuff
		private class TypeNameEncapsulator
		{
			public object o;
		}

		public static T LoadWithType<T>(string serialized)
		{
			var tne = JsonSerializer.Deserialize<TypeNameEncapsulator>(serialized, SerializerOptions);

			if (tne?.o is JsonElement jsonElement)
				return jsonElement.Deserialize<T>(SerializerOptions);

			return default;
		}

		public static object LoadWithType(string serialized, Type deserializedType)
		{
			var tne = JsonSerializer.Deserialize<TypeNameEncapsulator>(serialized, SerializerOptions);

			if (tne?.o is JsonElement jsonElement)
				return jsonElement.Deserialize(deserializedType, SerializerOptions);

			return null;
		}

		public static string SaveWithType(object o)
		{
			var tne = new TypeNameEncapsulator { o = o };
			return JsonSerializer.Serialize(tne, SerializerOptions);
		}
	}
}
