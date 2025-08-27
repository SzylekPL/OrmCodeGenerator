using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace OrmGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MainAnalyzer : DiagnosticAnalyzer
{
	private static readonly HashSet<string> EnumNames = new(typeof(DataType).GetEnumNames());
	private static readonly DiagnosticDescriptor Rule = new(
		id: "ORM001",
		title: "Unmarked model",
		messageFormat: "The type {0} of property {1} isn't a marked model",
		category: "Syntax",
		description: "The property is neither a standard database type, nor a model marked with OrmModelAttribute or NestableOrmModelAttribute.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterSymbolAction(static ctx =>
		{
			IPropertySymbol property = (IPropertySymbol)ctx.Symbol;

			if (EnumNames.Contains(property.Type.Name))
				return;

			bool predicate = !property
				.Type
				.GetAttributes()
				.Any(static attr => attr.AttributeClass?.Name is "OrmModelAttribute" or "NestableOrmAttribute");
			if (predicate)
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, property.Locations[0], property.Type.Name, property.Name);
				ctx.ReportDiagnostic(diagnostic);
			}
		}, SymbolKind.Property);
	}
}
