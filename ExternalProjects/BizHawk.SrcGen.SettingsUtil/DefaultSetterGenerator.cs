namespace BizHawk.SrcGen.SettingsUtil;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class DefaultSetterGenerator : ISourceGenerator
{
	public class SyntaxReceiver : ISyntaxContextReceiver
	{
		public readonly List<(TypeDeclarationSyntax, SemanticModel)> TypeDeclarations = new();

		public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
		{
			if (context.Node is TypeDeclarationSyntax cds)
			{
				TypeDeclarations.Add((cds, context.SemanticModel));
			}
		}
	}

	public void Initialize(GeneratorInitializationContext context)
		=> context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

	private static void CreateDefaultSetter(StringBuilder source, INamespaceOrTypeSymbol symbol)
	{
		var props = symbol
			.GetMembers()
			.Where(m => m.Kind == SymbolKind.Property)
			.ToImmutableArray();

		source.Append($@"
		public static void SetDefaultValues({symbol} settings)
		{{");

		foreach (var prop in props)
		{
			var defaultValueAttribute = prop
				.GetAttributes()
				.FirstOrDefault(
					a => a.AttributeClass?.Name == "DefaultValueAttribute");

			var ctorArgs = defaultValueAttribute?.ConstructorArguments;
			if (!ctorArgs.HasValue)
			{
				continue;
			}

			switch (ctorArgs.Value.Length)
			{
				case 1:
					// this single arg is just the value assigned to the default value
					var arg = ctorArgs.Value[0];
					// a bit lame, but it'll work
					// TODO: do we even want to handle arrays? we don't even have arrays in default values...
					var converionStr = arg.Kind == TypedConstantKind.Array
						? $"new {arg.Type}[] " // new T[]
						: ""; // do we need a cast (i.e. (T)) here? probably not?
					source.Append($@"
			settings.{prop.Name} = {converionStr}{arg.ToCSharpString()};");
					break;
				case 2:
					// first arg is the type, the second arg is a string which converts it
					source.Append($@"
			settings.{prop.Name} = ({ctorArgs.Value[0].Value})System.ComponentModel.TypeDescriptor
									.GetConverter({ctorArgs.Value[0].ToCSharpString()})
									.ConvertFromInvariantString({ctorArgs.Value[1].ToCSharpString()});");
					break;
			}
		}

		source.Append(@"
		}
");
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.SyntaxContextReceiver is not SyntaxReceiver syntaxReceiver)
		{
			return;
		}

		// Generated source code
		var source = new StringBuilder(@"
namespace BizHawk.Common
{
	public static partial class SettingsUtil
	{");

		foreach (var (tds, semanticModel) in syntaxReceiver.TypeDeclarations)
		{
			if (tds.AttributeLists.SelectMany(e => e.Attributes)
			    .Any(e => e.Name.NormalizeWhitespace().ToFullString() == "CoreSettings"))
			{
				var symbol = semanticModel.GetDeclaredSymbol(tds, context.CancellationToken);
				if (symbol is not null) // probably never happens?
				{
					CreateDefaultSetter(source, symbol);
				}
			}
		}

		source.Append(@"
	}
}");

		// Add the source code to the compilation
		context.AddSource("DefaultSetters.g.cs", source.ToString());
	}
}
