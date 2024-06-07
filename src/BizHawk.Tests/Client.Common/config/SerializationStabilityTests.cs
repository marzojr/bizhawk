using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common.Json;

namespace BizHawk.Tests.Client.Common.config
{
	[TestClass]
	public sealed class SerializationStabilityTests
	{
		private const string PATHENTRY_SER = @"{""Type"":""Movies"",""Path"":""./Movies"",""System"":""Global_NULL""}";

		private const string RECENT_SER = @"{""recentlist"":[],""MAX_RECENT_FILES"":8,""AutoLoad"":false,""Frozen"":false}";

		private const string ZWINDER_SER = @"{""CurrentUseCompression"":false,""CurrentBufferSize"":256,""CurrentTargetFrameLength"":500,""CurrentStoreType"":0,""RecentUseCompression"":false,""RecentBufferSize"":128,""RecentTargetFrameLength"":2000,""RecentStoreType"":0,""GapsUseCompression"":false,""GapsBufferSize"":64,""GapsTargetFrameLength"":125,""GapsStoreType"":0,""AncientStateInterval"":5000,""AncientStoreType"":0}";

#if NET5_0_OR_GREATER
		private static readonly IReadOnlySet<Type> KnownGoodFromStdlib = new HashSet<Type>
#else
		private static readonly ICollection<Type> KnownGoodFromStdlib = new HashSet<Type>
#endif
		{
			typeof(bool),
			typeof(DateTime),
			typeof(Dictionary<,>),
			typeof(int),
			typeof(JsonElement),
			typeof(List<>),
			typeof(Nullable<>),
			typeof(object),
			typeof(Point),
			typeof(Queue<>),
			typeof(float),
			typeof(Size),
			typeof(string),
		};

		private static readonly IReadOnlyDictionary<Type, string> KnownGoodFromBizHawk = new Dictionary<Type, string>
		{
			[typeof(AnalogBind)] = @"{""Value"":""X1 LeftThumbX Axis"",""Mult"":0.8,""Deadzone"":0.1}",
			[typeof(CheatConfig)] = $@"{{""DisableOnLoad"":false,""LoadFileByGame"":true,""AutoSaveOnClose"":true,""Recent"":{RECENT_SER}}}",
			[typeof(FeedbackBind)] = @"{""Channels"":""Left+Right"",""GamepadPrefix"":""X1 "",""Prescale"":1}",
			[typeof(MessagePosition)] = @"{""X"":0,""Y"":0,""Anchor"":0}",
			[typeof(MovieConfig)] = $@"{{""MovieEndAction"":3,""EnableBackupMovies"":true,""MoviesOnDisk"":false,""MovieCompressionLevel"":2,""VBAStyleMovieLoadState"":false,""DefaultTasStateManagerSettings"":{ZWINDER_SER}}}",
			[typeof(PathEntry)] = PATHENTRY_SER,
			[typeof(PathEntryCollection)] = $@"{{""Paths"":[{PATHENTRY_SER}],""UseRecentForRoms"":false,""LastRomPath"":"".""}}",
			[typeof(RecentFiles)] = RECENT_SER,
			[typeof(RewindConfig)] = @"{""UseCompression"":false,""UseDelta"":false,""Enabled"":true,""AllowSlowStates"":false,""BufferSize"":512,""UseFixedRewindInterval"":false,""TargetFrameLength"":600,""TargetRewindInterval"":5,""AllowOutOfOrderStates"":true,""BackingStore"":0}",
			[typeof(SaveStateConfig)] = @"{""Type"":0,""CompressionLevelNormal"":1,""CompressionLevelRewind"":0,""MakeBackups"":true,""SaveScreenshot"":true,""BigScreenshotSize"":131072,""NoLowResLargeScreenshots"":false}",
			[typeof(ToolDialogSettings)] = @"{""_wndx"":52,""_wndy"":44,""Width"":796,""Height"":455,""SaveWindowPosition"":true,""TopMost"":false,""FloatingWindow"":true,""AutoLoad"":false}",
			[typeof(ZwinderStateManagerSettings)] = ZWINDER_SER,
		};

		[TestMethod]
		public void AssertAllTypesKnownSerializable()
		{
			static void CheckMemberAndTypeParams(Type t, string groupDesc)
			{
				if (t.IsEnum) return;
				if (t.IsConstructedGenericType)
				{
					CheckMemberAndTypeParams(t.GetGenericTypeDefinition(), groupDesc);
					foreach (var typeParam in t.GenericTypeArguments) CheckMemberAndTypeParams(typeParam, groupDesc);
					return;
				}
				Assert.IsTrue(KnownGoodFromStdlib.Contains(t) || KnownGoodFromBizHawk.ContainsKey(t), $"type {t.FullName}, present in {groupDesc}, may not be serializable");
			}
			static void CheckAll<T>(string? groupDesc = null)
			{
				var t = typeof(T);
				groupDesc ??= t.Name;
				foreach (var mi in t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (mi.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;
					if (mi is PropertyInfo pi) CheckMemberAndTypeParams(pi.PropertyType, groupDesc);
					else if (mi is FieldInfo fi) CheckMemberAndTypeParams(fi.FieldType, groupDesc);
				}
			}
			CheckAll<Config>();
		}

		[TestMethod]
		public void TestRoundTripSerialization()
		{
			static object? Deser(string s, Type type) => JsonSerializer.Deserialize(s, type, ConfigService.SerializerOptions);
			static string Ser(object? o) => JsonSerializer.Serialize(o, ConfigService.SerializerOptions);

			foreach (var (type, s) in KnownGoodFromBizHawk)
			{
				if (s == "TODO") continue;
				Assert.AreEqual(s, Ser(Deser(s, type)), $"{type} failed serialization round-trip");
			}
		}

		[TestMethod]
		[DataRow("0.8")]
		[DataRow("1.00000036")]
		[DataRow("1.8")]
		public void TestRoundTripSerializationFloatConverter(string floatValue)
		{
			float deserialized = JsonSerializer.Deserialize<float>(floatValue, ConfigService.SerializerOptions);
			string serialized = JsonSerializer.Serialize(deserialized, ConfigService.SerializerOptions);
			Assert.AreEqual(floatValue, serialized);
		}

		[TestMethod]
		[DataRow("[1,2,3]")]
		[DataRow("[]")]
		[DataRow("null")]
		[DataRow("[255,0,127,128,1]")]
		public void TestRoundTripSerializationByteArrayConverter(string byteArrayValue)
		{
			byte[]? deserialized = JsonSerializer.Deserialize<byte[]>(byteArrayValue, ConfigService.SerializerOptions);
			string serialized = JsonSerializer.Serialize(deserialized, ConfigService.SerializerOptions);
			Assert.AreEqual(byteArrayValue, serialized);
		}

		[TestMethod]
		public void TestSerializationTypeConverter()
		{
			var color = Color.FromArgb(200, 255, 13, 42);
			string serialized = JsonSerializer.Serialize(color, ConfigService.SerializerOptions);
			Assert.AreEqual("\"200, 255, 13, 42\"", serialized);

			var newColor = JsonSerializer.Deserialize<Color>(serialized, ConfigService.SerializerOptions);
			Assert.AreEqual(color, newColor);
		}

		private static bool Equals<T>(T[,] array1, T[,] array2)
		{
			return array1.Rank == array2.Rank
				&& Enumerable.Range(0, array1.Rank).All(dimension => array1.GetLength(dimension) == array2.GetLength(dimension))
				&& array1.Cast<T>().SequenceEqual(array2.Cast<T>());
		}

		[TestMethod]
		public void TestSerialization2DArrayConverter()
		{
			var options = new JsonSerializerOptions
			{
				Converters = { new Array2DJsonConverter<byte>() }
			};
			var optionsWithByteArrayConverter = new JsonSerializerOptions
			{
				Converters = { new Array2DJsonConverter<byte>(), new ByteArrayAsNormalArrayJsonConverter() }
			};

			byte[,] testByteArray =
			{
				{ 1, 2, 3 },
				{ 255, 0, 128 }
			};

			string serialized = JsonSerializer.Serialize(testByteArray, options);
			Assert.AreEqual("[\"AQID\",\"/wCA\"]", serialized);
			byte[,] deserialized = JsonSerializer.Deserialize<byte[,]>(serialized, options)!;
			Assert.IsTrue(Equals(testByteArray, deserialized));

			string serialized2 = JsonSerializer.Serialize(testByteArray, optionsWithByteArrayConverter);
			Assert.AreEqual("[[1,2,3],[255,0,128]]", serialized2);
			byte[,] deserialized2 = JsonSerializer.Deserialize<byte[,]>(serialized2, optionsWithByteArrayConverter)!;
			Assert.IsTrue(Equals(testByteArray, deserialized2));
		}
	}
}
