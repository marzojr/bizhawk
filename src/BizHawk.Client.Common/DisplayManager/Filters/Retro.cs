// https://github.com/Themaister/RetroArch/wiki/GLSL-shaders
// https://github.com/Themaister/Emulator-Shader-Pack/blob/master/Cg/README
// https://github.com/libretro/common-shaders/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

using BizHawk.Client.Common.FilterManager;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common.Filters
{
	public class RetroShaderChain : IDisposable
	{
		private static readonly Regex RxInclude = new(@"^(\s)?\#include(\s)+(""|<)(.*)?(""|>)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

		private static string ResolveIncludes(string content, string baseDirectory)
		{
			while (true)
			{
				var match = RxInclude.Match(content);
				if (match.Value == string.Empty)
				{
					return content;
				}

				string fname = match.Groups[4].Value;
				fname = Path.Combine(baseDirectory,fname);
				string includedContent = ResolveIncludes(File.ReadAllText(fname),Path.GetDirectoryName(fname));
				content = content.Substring(0, match.Index) + includedContent + content.Substring(match.Index + match.Length);
			}
		}

		public RetroShaderChain(IGL owner, RetroShaderPreset preset, string baseDirectory, bool debug = false)
		{
			Owner = owner;
			Passes = preset.Passes.ToArray();
			Errors = string.Empty;

			if (owner.API is not ("OPENGL" or "D3D9"))
			{
				Errors = $"Unsupported API {owner.API}";
				return;
			}

			// load up the shaders
			RetroShader[] shaders = new RetroShader[preset.Passes.Count];
			for (int i = 0; i < preset.Passes.Count; i++)
			{
				// acquire content. we look for it in any reasonable filename so that one preset can bind to multiple shaders
				string content;
				string path = Path.Combine(baseDirectory, preset.Passes[i].ShaderPath);
				if (!File.Exists(path))
				{
					if (!Path.HasExtension(path))
					{
						path += ".cg";
					}

					if (!File.Exists(path))
					{
						path = owner.API switch
						{
							"OPENGL" => Path.ChangeExtension(path, ".glsl"),
							"D3D9" => Path.ChangeExtension(path, ".hlsl"),
							_ => throw new InvalidOperationException(),
						};
					}
				}

				try
				{
					content = ResolveIncludes(File.ReadAllText(path), Path.GetDirectoryName(path));
				}
				catch (DirectoryNotFoundException e)
				{
					Errors += $"caught {nameof(DirectoryNotFoundException)}: {e.Message}\n";
					return;
				}

				catch (FileNotFoundException e)
				{
					Errors += $"could not read file {e.FileName}\n";
					return;
				}

				var shader = shaders[i] = new(Owner, content, debug);
				if (!shader.Available)
				{
					Errors += $"-------------------\r\nPass {i}:\r\n{(shader.Errors ?? string.Empty).Replace("\n","\r\n")}\n";
					return;
				}
			}

			Shaders = shaders;
			Available = true;
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			foreach (var s in Shaders)
			{
				s.Dispose();
			}

			_isDisposed = true;
		}

		/// <summary>Whether this shader chain is available (it wont be available if some resources failed to load or compile)</summary>
		public readonly bool Available;
		public readonly string Errors;
		public readonly IGL Owner;
		public readonly IReadOnlyList<RetroShader> Shaders = Array.Empty<RetroShader>();
		public readonly RetroShaderPreset.ShaderPass[] Passes;

		private bool _isDisposed;
	}

	public class RetroShaderPreset
	{
		/// <summary>
		/// Parses an instance from a stream to a CGP file
		/// </summary>
		public RetroShaderPreset(Stream stream)
		{
			string content = new StreamReader(stream).ReadToEnd();
			Dictionary<string, string> dict = new();

			// parse the key-value-pair format of the file
			content = content.Replace("\r", "");
			foreach (string splitLine in content.Split('\n'))
			{
				string line = splitLine.Trim();
				if (line.Length is 0)
				{
					continue;
				}

				if (line.StartsWith('#'))
				{
					continue; // comments
				}

				int eq = line.IndexOf('=');
				string key = line.Substring(0, eq).Trim();
				string value = line.Substring(eq + 1).Trim();
				int quote = value.IndexOf('\"');
				if (quote != -1)
				{
					value = value.Substring(quote + 1, value.IndexOf('\"', quote + 1) - (quote + 1));
				}
				else
				{
					// remove comments from end of value. exclusive from above condition, since comments after quoted strings would be snipped by the quoted string extraction
					int hash = value.IndexOf('#');
					if (hash != -1)
					{
						value = value.Substring(0, hash);
					}

					value = value.Trim();
				}
				dict[key.ToLowerInvariant()] = value;
			}

			// process the keys
			int nShaders = FetchInt(dict, "shaders", 0);
			for (int i = 0; i < nShaders; i++)
			{
				ShaderPass sp = new() { Index = i };
				Passes.Add(sp);

				sp.InputFilterLinear = FetchBool(dict, $"filter_linear{i}", false); // Should this value not be defined, the filtering option is implementation defined.
				sp.OutputFloat = FetchBool(dict, $"float_framebuffer{i}", false);
				sp.FrameCountMod = FetchInt(dict, $"frame_count_mod{i}", 1);
				sp.ShaderPath = FetchString(dict, $"shader{i}", "?"); // todo - change extension to .cg for better compatibility? just change .cg to .glsl transparently at last second?

				// If no scale type is assumed, it is assumed that it is set to "source" with scaleN set to 1.0.
				// It is possible to set scale_type_xN and scale_type_yN to specialize the scaling type in either direction. scale_typeN however overrides both of these.
				sp.ScaleTypeX = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, $"scale_type_x{i}", "Source"), true);
				sp.ScaleTypeY = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, $"scale_type_y{i}", "Source"), true);
				ScaleType st = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, $"scale_type{i}", "NotSet"), true);
				if (st != ScaleType.NotSet)
				{
					sp.ScaleTypeX = sp.ScaleTypeY = st;
				}

				// scaleN controls both scaling type in horizontal and vertical directions. If scaleN is defined, scale_xN and scale_yN have no effect.
				sp.Scale.X = FetchFloat(dict, $"scale_x{i}", 1);
				sp.Scale.Y = FetchFloat(dict, $"scale_y{i}", 1);
				if (dict.ContainsValue($"scale{i}"))
				{
					sp.Scale.X = sp.Scale.Y = FetchFloat(dict, $"scale{i}", 1);
				}

				//TODO - LUTs
			}
		}

		public List<ShaderPass> Passes { get; set; } = new();

		public enum ScaleType
		{
			NotSet, Source, Viewport, Absolute
		}

		public class ShaderPass
		{
			public int Index;
			public string ShaderPath;
			public bool InputFilterLinear;
			public bool OutputFloat;
			public int FrameCountMod;
			public ScaleType ScaleTypeX;
			public ScaleType ScaleTypeY;
			public Vector2 Scale;
		}

		private static string FetchString(IDictionary<string, string> dict, string key, string @default) => dict.TryGetValue(key, out string str) ? str : @default;

		private static int FetchInt(IDictionary<string, string> dict, string key, int @default) => dict.TryGetValue(key, out string str) ? int.Parse(str) : @default;

		private static float FetchFloat(IDictionary<string, string> dict, string key, float @default) => dict.TryGetValue(key, out string str) ? float.Parse(str, NumberFormatInfo.InvariantInfo) : @default;

		private static bool FetchBool(IDictionary<string, string> dict, string key, bool @default) => dict.TryGetValue(key, out string str) ? ParseBool(str) : @default;

		private static bool ParseBool(string value)
		{
			switch (value)
			{
				case "1":
					return true;
				case "0":
					return false;
			}

			value = value.ToLowerInvariant();
			return value switch
			{
				"true" => true,
				"false" => false,
				_ => throw new InvalidOperationException("Unparsable bool in CGP file content")
			};
		}
	}

	public class RetroShaderPass : BaseFilter
	{
		private readonly RetroShaderChain _rsc;
		private readonly RetroShaderPreset.ShaderPass _sp;
		private readonly int _rsi;
		private Size _outputSize;

		public override string ToString() => $"{nameof(RetroShaderPass)}[#{_rsi}]";

		public RetroShaderPass(RetroShaderChain rsc, int index)
		{
			_rsc = rsc;
			_rsi = index;
			_sp = _rsc.Passes[index];
		}

		public override void Initialize() => DeclareInput(SurfaceDisposition.Texture);

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			var inSize = state.SurfaceFormat.Size;

			_outputSize.Width = _sp.ScaleTypeX switch
			{
				RetroShaderPreset.ScaleType.Absolute => (int)_sp.Scale.X,
				RetroShaderPreset.ScaleType.Source => (int)(inSize.Width * _sp.Scale.X),
				_ => _outputSize.Width
			};

			_outputSize.Height = _sp.ScaleTypeY switch
			{
				RetroShaderPreset.ScaleType.Absolute => (int)_sp.Scale.Y,
				RetroShaderPreset.ScaleType.Source => (int)(inSize.Height * _sp.Scale.Y),
				_ => _outputSize.Height
			};

			DeclareOutput(new SurfaceState(new(_outputSize), SurfaceDisposition.RenderTarget));
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			_outputSize = size;
			return size;
		}

		public override Size PresizeInput(string channel, Size inSize)
		{
			var outsize = inSize;

			outsize.Width = _sp.ScaleTypeX switch
			{
				RetroShaderPreset.ScaleType.Absolute => (int)_sp.Scale.X,
				RetroShaderPreset.ScaleType.Source => (int)(inSize.Width * _sp.Scale.X),
				_ => outsize.Width
			};

			outsize.Height = _sp.ScaleTypeY switch
			{
				RetroShaderPreset.ScaleType.Absolute => (int)_sp.Scale.Y,
				RetroShaderPreset.ScaleType.Source => (int)(inSize.Height * _sp.Scale.Y),
				_ => outsize.Height
			};

			return outsize;
		}

		public override void Run()
		{
			var shader = _rsc.Shaders[_rsi];
			shader.Bind();

			// apply all parameters to this shader.. even if it was meant for other shaders. kind of lame.
			if (Parameters != null)
			{
				foreach (var (k, v) in Parameters)
				{
					if (v is float value)
					{
						shader.Pipeline[k].Set(value);
					}
				}
			}

			var input = InputTexture;
			if (_sp.InputFilterLinear)
			{
				InputTexture.SetFilterLinear();
			}
			else
			{
				InputTexture.SetFilterNearest();
			}

			_rsc.Shaders[_rsi].Run(input, input.Size, _outputSize, InputTexture.IsUpsideDown);

			// maintain invariant.. i think.
			InputTexture.SetFilterNearest();
		}
	}
}
