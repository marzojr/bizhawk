using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class ConfigExtensions
	{
		private static JsonElement Serialize(object o)
		{
			return JsonSerializer.SerializeToElement(o, ConfigService.SerializerOptions);
		}
		private static object Deserialize(JsonElement json, Type type)
		{
			try
			{
				return json.Deserialize(type, ConfigService.SerializerOptions);
			}
			catch
			{
				// presumably some sort of config mismatch.  Anywhere we can expose this usefully?
				return null;
			}
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSettings(this Config config, Type coreType, Type settingsType)
		{
			_ = config.CoreSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j, settingsType);
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static TSetting GetCoreSettings<TCore, TSetting>(this Config config)
			where TCore : IEmulator
		{
			return (TSetting)config.GetCoreSettings(typeof(TCore), typeof(TSetting));
		}

		/// <summary>
		/// saves the core settings for a core
		/// </summary>
		/// <param name="o">null to remove settings for that core instead</param>
		public static void PutCoreSettings(this Config config, object o, Type coreType)
		{
			if (o != null)
			{
				config.CoreSettings[coreType.ToString()] = Serialize(o);
			}
			else
			{
				config.CoreSettings.Remove(coreType.ToString());
			}
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSyncSettings(this Config config, Type coreType, Type syncSettingsType)
		{
			_ = config.CoreSyncSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j, syncSettingsType);
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static TSync GetCoreSyncSettings<TCore, TSync>(this Config config)
			where TCore : IEmulator
		{
			return (TSync)config.GetCoreSyncSettings(typeof(TCore), typeof(TSync));
		}

		/// <summary>
		/// saves the core syncsettings for a core
		/// </summary>
		/// <param name="o">null to remove settings for that core instead</param>
		public static void PutCoreSyncSettings(this Config config, object o, Type coreType)
		{
			if (o != null)
			{
				config.CoreSyncSettings[coreType.ToString()] = Serialize(o);
			}
			else
			{
				config.CoreSyncSettings.Remove(coreType.ToString());
			}
		}

		public static void ReplaceKeysInBindings(this Config config, IReadOnlyDictionary<string, string> replMap)
		{
			string ReplMulti(string multiBind)
				=> multiBind.TransformFields(',', bind => bind.TransformFields('+', button => replMap.TryGetValue(button, out var repl) ? repl : button));
			foreach (var k in config.HotkeyBindings.Keys.ToList()) config.HotkeyBindings[k] = ReplMulti(config.HotkeyBindings[k]);
			foreach (var bindCollection in new[] { config.AllTrollers, config.AllTrollersAutoFire }) // analog and feedback binds can only be bound to (host) gamepads, not keyboard
			{
				foreach (var k in bindCollection.Keys.ToArray()) bindCollection[k] = bindCollection[k].ToDictionary(static kvp => kvp.Key, kvp => ReplMulti(kvp.Value));
			}
		}

		/// <param name="fileExt">file extension, including the leading period and in lowercase</param>
		/// <remarks><paramref name="systemID"/> will be <see langword="null"/> if returned value is <see langword="false"/></remarks>
		public static bool TryGetChosenSystemForFileExt(this Config config, string fileExt, out string systemID)
		{
			var b = config.PreferredPlatformsForExtensions.TryGetValue(fileExt, out var v);
			if (b && !string.IsNullOrEmpty(v))
			{
				systemID = v;
				return true;
			}
			systemID = null;
			return false;
		}
	}
}
