using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using BizHawk.Client.Common;
using BizHawk.Common;

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
	}
}
