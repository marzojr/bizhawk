using System.IO;
using System.Text.Json;

using BizHawk.Common.StringExtensions;

//this file contains some cumbersome self-"serialization" in order to gain a modicum of control over what the serialized output looks like
//I don't want them to look like crufty json

namespace BizHawk.Client.Common
{
	public interface IOpenAdvanced
	{
		string TypeName { get; }
		string DisplayName { get; }

		/// <summary>
		/// returns a sole path to use for opening a rom (not sure if this is a good idea)
		/// </summary>
		string SimplePath { get; }
		
		void Deserialize(string str);
		void Serialize(TextWriter tw);
	}

	public interface IOpenAdvancedLibretro
	{
		string CorePath { get; set;  }
	}

	public static class OpenAdvancedTypes
	{
		public const string OpenRom = "OpenRom";
		public const string Libretro = "Libretro";
		public const string LibretroNoGame = "LibretroNoGame";
		public const string MAME = "MAME";
	}


	public static class OpenAdvancedSerializer
	{
		public static IOpenAdvanced ParseWithLegacy(string text)
		{
			return text.StartsWith('*')
				? Deserialize(text.Substring(1))
				: new OpenAdvanced_OpenRom(text);
		}

		private static IOpenAdvanced Deserialize(string text)
		{
			int idx = text.IndexOf('*');
			string type = text.Substring(0, idx);
			string token = text.Substring(idx + 1);

			var ioa = type switch
			{
				OpenAdvancedTypes.OpenRom => (IOpenAdvanced)new OpenAdvanced_OpenRom(),
				OpenAdvancedTypes.Libretro => new OpenAdvanced_Libretro(),
				OpenAdvancedTypes.LibretroNoGame => new OpenAdvanced_LibretroNoGame(),
				OpenAdvancedTypes.MAME => new OpenAdvanced_MAME(),
				_ => null
			};

			if (ioa == null)
			{
				throw new InvalidOperationException($"{nameof(IOpenAdvanced)} deserialization error");
			}

			ioa.Deserialize(token);
			return ioa;
		}

		public static string Serialize(IOpenAdvanced ioa)
		{
			var sw = new StringWriter();
			sw.Write("{0}*", ioa.TypeName);
			ioa.Serialize(sw);
			return sw.ToString();
		}
	}

	public class OpenAdvanced_Libretro : IOpenAdvanced, IOpenAdvancedLibretro
	{
		public struct Token
		{
			public string Path, CorePath;
		}

		public Token token;

		public string TypeName => "Libretro";
		public string DisplayName => $"{Path.GetFileNameWithoutExtension(token.CorePath)}: {token.Path}";
		public string SimplePath => token.Path;

		public void Deserialize(string str)
		{
			token = JsonSerializer.Deserialize<Token>(str);
		}
		
		public void Serialize(TextWriter tw)
		{
			tw.Write(JsonSerializer.Serialize(token));
		}

		public string CorePath
		{
			get => token.CorePath;
			set => token.CorePath = value;
		}
	}

	public class OpenAdvanced_LibretroNoGame : IOpenAdvanced, IOpenAdvancedLibretro
	{
		// you might think ideally we'd fetch the libretro core name from the core info inside it
		// but that would involve spinning up excess libretro core instances, which probably isn't good for stability, no matter how much we wish otherwise, not to mention slow.
		// moreover it's kind of complicated here,
		// and finally, I think the DisplayName should really be file-based in all cases, since the user is going to be loading cores by filename and
		// this is related to the recent roms filename management.
		// so, leave it.
		public OpenAdvanced_LibretroNoGame()
		{
		}

		public OpenAdvanced_LibretroNoGame(string corePath)
		{
			CorePath = corePath;
		}

		public string TypeName => "LibretroNoGame";
		public string DisplayName => Path.GetFileName(CorePath); // assume we like the filename of the core
		public string SimplePath => ""; // effectively a signal to not use a game

		public void Deserialize(string str)
		{
			CorePath = str;
		}

		public void Serialize(TextWriter tw)
		{
			tw.Write(CorePath);
		}

		public string CorePath { get; set; }
	}

	public class OpenAdvanced_OpenRom : IOpenAdvanced
	{
		public string Path;

		public string TypeName => "OpenRom";
		public string DisplayName => Path;
		public string SimplePath => Path;

		public OpenAdvanced_OpenRom() {}

		public OpenAdvanced_OpenRom(string path)
			: this()
			=> Path = path;

		public void Deserialize(string str)
		{
			Path = str;
		}

		public void Serialize(TextWriter tw)
		{
			tw.Write(Path);
		}
	}

	public class OpenAdvanced_MAME : IOpenAdvanced
	{
		public string Path;

		public string TypeName => "MAME";
		public string DisplayName => $"{TypeName}: {Path}";
		public string SimplePath => Path;

		public void Deserialize(string str)
		{
			Path = str;
		}

		public void Serialize(TextWriter tw)
		{
			tw.Write(Path);
		}
	}
}
