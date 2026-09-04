using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using OrmGenerator.Utility;
using System.Collections.Immutable;
using System.Linq;
using static Shared.ProjectDiagnostics;

namespace OrmGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MainAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DefinedDiagnostics;

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSymbolAction(static ctx =>
		{
			INamedTypeSymbol type = (INamedTypeSymbol)ctx.Symbol;

			AttributeData? attribute = type
				.GetAttributes()
				.FirstOrDefault(static attr => attr.AttributeClass?.Name == "OrmModelAttribute");

			if (attribute is null) return;

			ModelOptions options = (ModelOptions)attribute.ConstructorArguments[0].Value!; //null will default to ModelOptions.None

			if (type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor))
			{
				ParameterListSyntax? paramList = (ParameterListSyntax?)type
					.DeclaringSyntaxReferences
					.SelectMany(r => r.GetSyntax(ctx.CancellationToken).ChildNodes())
					.FirstOrDefault(n => n is ParameterListSyntax);

				if (paramList is not { Parameters.Count: > 0 })
				{
					ctx.ReportDiagnostic(Diagnostic.Create(_ctorNotSuitableRule, type.Locations[0], type.Name));
					return;
				}

				//todo: diagnostics report when trying to use non-models in mapping
			}
		}, SymbolKind.NamedType);

		context.RegisterSymbolAction(static ctx =>
		{
			IPropertySymbol prop = (IPropertySymbol)ctx.Symbol;

			if (!prop.ContainingType.GetAttributes().Any(static a => a.AttributeClass?.Name is "OrmModelAttribute"))
				return;

			if (prop.Type.GetAttributes().Any(static a => a.AttributeClass?.Name is "OrmModelAttribute"))
				return;

			ctx.ReportDiagnostic(Diagnostic.Create(_notMarkedRule, prop.Locations[0], prop.Type.Name, prop.Name));

		}, SymbolKind.Property);
	}
}