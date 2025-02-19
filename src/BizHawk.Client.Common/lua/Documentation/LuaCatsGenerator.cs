using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BizHawk.Common;
using NLua;

namespace BizHawk.Client.Common;

/// <summary>
/// Generates API definitions in the LuaCATS format.
/// </summary>
/// <remarks>
/// See https://luals.github.io/wiki/annotations
/// </remarks>
internal class LuaCatsGenerator
{
	private static readonly Dictionary<Type, string> TypeConversions = new()
	{
		[typeof(object)] = "any",
		[typeof(int)] = "integer",
		[typeof(uint)] = "integer",
		[typeof(short)] = "integer",
		[typeof(ushort)] = "integer",
		[typeof(long)] = "integer",
		[typeof(ulong)] = "integer",
		[typeof(float)] = "number",
		[typeof(double)] = "number",
		[typeof(string)] = "string",
		[typeof(bool)] = "boolean",
		[typeof(LuaFunction)] = "function",
		[typeof(LuaTable)] = "table",
		[typeof(System.Drawing.Color)] = "color",
	};

	private const string Preamble = @"---@meta _

---@class color : userdata

---A color in one of the following formats:
--- - Number in the format `0xAARRGGBB`
--- - String in the format `""#RRGGBB""` or `""#AARRGGBB""`
--- - A CSS3/X11 color name e.g. `""blue""`, `""palegoldenrod""`
--- - Color created with `forms.createcolor`
---@alias luacolor integer | string | color
";

	public string Generate(LuaDocumentation docs)
	{
		var sb = new StringBuilder();

		sb.AppendLine($"--Generated with BizHawk {VersionInfo.MainVersion}");

		sb.AppendLine(Preamble);

		foreach (var libraryGroup in docs.GroupBy(func => func.Library).OrderBy(group => group.Key))
		{
			string library = libraryGroup.Key;
			string libraryDescription = libraryGroup.First().LibraryDescription;

			if (!string.IsNullOrEmpty(libraryDescription))
				sb.AppendLine(FormatDescription(libraryDescription));
			sb.AppendLine($"---@class {library}");
			sb.AppendLine($"{library} = {{}}");
			sb.AppendLine();

			foreach (var func in libraryGroup.OrderBy(func => func.Name))
			{
				if (!string.IsNullOrEmpty(func.Description))
					sb.AppendLine(FormatDescription(func.Description));

				if (func.IsDeprecated)
					sb.AppendLine("---@deprecated");

				foreach (var parameter in func.Method.GetParameters())
				{
					if (IsParams(parameter))
					{
						sb.Append("---@vararg");
					}
					else
					{
						sb.Append($"---@param {parameter.Name}");
						if (parameter.IsOptional || IsNullable(parameter.ParameterType))
							sb.Append('?');
					}

					sb.Append(' ');
					sb.AppendLine(GetLuaType(parameter));
				}

				if (func.Method.ReturnType != typeof(void))
				{
					sb.Append("---@return ");
					sb.AppendLine(GetLuaType(func.Method.ReturnType));
				}

				sb.Append($"function {library}.{func.Name}(");

				foreach (var parameter in func.Method.GetParameters())
				{
					if (parameter.Position > 0)
						sb.Append(", ");
					sb.Append(IsParams(parameter) ? "..." : parameter.Name);
				}

				sb.AppendLine(") end");
				sb.AppendLine();
			}
			sb.AppendLine();
		}
		return sb.ToString();
	}

	private static string FormatDescription(string description)
	{
		// prefix every line with ---
		description = Regex.Replace(description, "^", "---", RegexOptions.Multiline);
		// replace {{wiki markup}} with `markdown`
		description = Regex.Replace(description, @"{{(.+?)}}", "`$1`");
		// replace wiki image markup with markdown
		description = Regex.Replace(description, @"\[(?<url>.+?)\|alt=(?<alt>.+?)\]", "![${alt}](${url})");
		return description;
	}

	private static string GetLuaType(ParameterInfo parameter)
	{
		if (parameter.ParameterType == typeof(object) && parameter.GetCustomAttribute<LuaColorParamAttribute>() is not null)
			return "luacolor"; // see Preamble

		// no [] array modifier for varargs
		if (parameter.ParameterType.IsArray && IsParams(parameter))
			return GetLuaType(parameter.ParameterType.GetElementType());

		// technically any string parameter can be passed a number, but let's just focus on the ones where it's commonly used
		// like `gui.text` and `forms.settext` instead of polluting the entire API surface
		if (parameter.ParameterType == typeof(string) && parameter.Name is "message" or "caption")
			return "string | number";

		return GetLuaType(parameter.ParameterType);
	}

	private static string GetLuaType(Type type)
	{
		if (type.IsArray)
			return GetLuaType(type.GetElementType()) + "[]";

		if (IsNullable(type))
			type = type.GetGenericArguments()[0];

		if (TypeConversions.TryGetValue(type, out string luaType))
			return luaType;
		else
			throw new NotSupportedException($"Unknown type {type.FullName} used in API. Generator must be updated to handle this.");
	}

	private static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

	private static bool IsParams(ParameterInfo parameter) => parameter.GetCustomAttribute<ParamArrayAttribute>() is not null;
}
