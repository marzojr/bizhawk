﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class LuaDocumentation : List<LibraryFunction>
	{
		public string ToTASVideosWikiMarkup()
		{
			var sb = new StringBuilder();

			sb
				.AppendLine("[module:ListParents]")
				.AppendLine()
				.AppendLine("This page documents the the behavior and parameters of Lua functions available for the [BizHawk] emulator.")
				.AppendLine("__This is an autogenerated page, do not edit__")
				.AppendLine()
				.AppendLine();

			sb.AppendLine(@"All type names represent the standard .NET types of the same name. Except for func which represents a lua function and table which represents a lua table. For more information on .NET types can be found in MSDN documentation.

__Types and notation__
* ? (question mark)
** A question mark next to a value indicates that it is a Nullable type (only applies to types that are not normally nullable)
* [[]] (brackets)
** Brackets around a parameter indicate that the parameter is optional. optional parameters have an equals sign followed by the value that will be used if no value is supplied.
** Brackets after a parameter type indicate it is an array
* null
** null is equivalent to the lua nil
* Color
** This is a .NET System.Drawing.Color struct. The value passed from lua is any value acceptable in the Color constructor. This means either a string with the color name such as ""red"", or a 0xAARRGGBB integer value.  Unless specified, this is not a nullable value
* object
** A System.Object, literally anything qualifies for this parameter. However, the context of particular function may suggest a narrower range of useful values.
* luaf
** A Lua function. Note that these are always parameters, and never return values of a call
* table
** A standard Lua table
");

			foreach (var library in this.Select(lf => new { Name = lf.Library, Description = lf.LibraryDescription }).Distinct())
			{
				sb
					.AppendFormat("%%TAB {0}%%", library.Name)
					.AppendLine()
					.AppendLine();
				if (!string.IsNullOrWhiteSpace(library.Description))
				{
					sb
						.Append(library.Description)
						.AppendLine()
						.AppendLine();
				}

				foreach (var func in this.Where(lf => lf.Library == library.Name))
				{
					sb
						.AppendFormat("__{0}.{1}__%%%", func.Library, func.Name)
						.AppendLine().AppendLine()
						.AppendFormat("* {0} {1}.{2}{3}", func.ReturnType, func.Library, func.Name, func.ParameterList.Replace("[", "[[").Replace("]", "]]"))
						.AppendLine().AppendLine()
						.AppendFormat("* {0}", func.Description)
						.AppendLine().AppendLine();
				}
			}

			sb.Append("%%TAB_END%%");

			return sb.ToString();
		}

		private class SublimeCompletions
		{
			public SublimeCompletions()
			{
				Scope = "source.lua - string";
			}

			[JsonProperty(PropertyName = "scope")]
			public string Scope { get; set; }

			[JsonProperty(PropertyName = "completions")]
			public List<Completion> Completions { get; set; } = new List<Completion>();

			public class Completion
			{
				[JsonProperty(PropertyName = "trigger")]
				public string Trigger { get; set; }

				[JsonProperty(PropertyName = "contents")]
				public string Contents { get; set; }
			}
		}

		public string ToSublime2CompletionList()
		{
			var sc = new SublimeCompletions();

			foreach (var f in this.OrderBy(lf => lf.Library).ThenBy(lf => lf.Name))
			{
				var completion = new SublimeCompletions.Completion
				{
					Trigger = f.Library + "." + f.Name
				};

				var sb = new StringBuilder();

				if (f.ParameterList.Any())
				{
					sb
						.Append($"{f.Library}.{f.Name}(");

					var parameters = f.Method.GetParameters()
						.ToList();

					for (int i = 0; i < parameters.Count; i++)
					{
						sb
							.Append("${")
							.Append(i + 1)
							.Append(":");

						if (parameters[i].IsOptional)
						{
							sb.Append($"[{parameters[i].Name}]");
						}
						else
						{
							sb.Append(parameters[i].Name);
						}

						sb.Append("}");

						if (i < parameters.Count - 1)
						{
							sb.Append(",");
						}
					}

					sb.Append(")");
				}
				else
				{
					sb.Append($"{f.Library}.{f.Name}()");
				}

				completion.Contents = sb.ToString();
				sc.Completions.Add(completion);
			}

			return JsonConvert.SerializeObject(sc);
		}

		public string ToNotepadPlusPlusAutoComplete()
		{
			return ""; // TODO
		}
	}

	public class LibraryFunction
	{
		private readonly LuaMethodAttribute _luaAttributes;
		private readonly LuaMethodExampleAttribute _luaExampleAttribute;
		private readonly MethodInfo _method;

		public LibraryFunction(string library, string libraryDescription, MethodInfo method)
		{
			_luaAttributes = method.GetCustomAttribute<LuaMethodAttribute>(false);
			_luaExampleAttribute = method.GetCustomAttribute<LuaMethodExampleAttribute>(false);
			_method = method;

			Library = library;
			LibraryDescription = libraryDescription;
		}

		public string Library { get; }
		public string LibraryDescription { get; }

		public MethodInfo Method => _method;

		public string Name => _luaAttributes.Name;

		public string Description => _luaAttributes.Description;

		public string Example => _luaExampleAttribute?.Example;

		private string _paramterList = null;

		public string ParameterList
		{
			get
			{
				if (_paramterList == null)
				{
					var parameters = _method.GetParameters();

					var list = new StringBuilder();
					list.Append('(');
					for (var i = 0; i < parameters.Length; i++)
					{
						var p = TypeCleanup(parameters[i].ToString());
						if (parameters[i].IsOptional)
						{
							var def = parameters[i].DefaultValue != null ? parameters[i].DefaultValue.ToString() : "null";
							list.AppendFormat("[{0} = {1}]", p, def);
						}
						else
						{
							list.Append(p);
						}

						if (i < parameters.Length - 1)
						{
							list.Append(", ");
						}
					}

					list.Append(')');
					_paramterList = list.ToString();
				}

				return _paramterList;
			}
		}

		private string TypeCleanup(string str)
		{
			return str
				.Replace("System", "")
				.Replace(" ", "")
				.Replace(".", "")
				.Replace("LuaInterface", "")
				.Replace("Object[]", "object[] ")
				.Replace("Object", "object ")
				.Replace("Nullable`1[Boolean]", "bool? ")
				.Replace("Boolean[]", "bool[] ")
				.Replace("Boolean", "bool ")
				.Replace("String", "string ")
				.Replace("LuaTable", "table ")
				.Replace("LuaFunction", "func ")
				.Replace("Nullable`1[Int32]", "int? ")
				.Replace("Nullable`1[UInt32]", "uint? ")
				.Replace("Byte", "byte ")
				.Replace("Int16", "short ")
				.Replace("Int32", "int ")
				.Replace("Int64", "long ")
				.Replace("Ushort", "ushort ")
				.Replace("Ulong", "ulong ")
				.Replace("UInt32", "uint ")
				.Replace("UInt64", "ulong ")
				.Replace("Double", "double ")
				.Replace("Uint", "uint ")
				.Replace("Nullable`1[DrawingColor]", "Color? ")
				.Replace("DrawingColor", "Color ")
				.ToLower();
		}

		public string ReturnType
		{
			get
			{
				var returnType = _method.ReturnType.ToString();
				return TypeCleanup(returnType).Trim();
			}
		}
	}
}
