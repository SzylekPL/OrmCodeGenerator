using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using OrmGenerator.Utility;
using Shared;
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
			if (!options.HasFlag(ModelOptions.DisableNesting))
				return;

			if (type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor))
			{
				//todo: check if this even works for records
				IMethodSymbol? ctor = type.InstanceConstructors
					.FirstOrDefault(static c => c.IsImplicitlyDeclared && c.Parameters.Length != 0);

				if (ctor is null)
				{
					ctx.ReportDiagnostic(Diagnostic.Create(_ctorNotSuitableRule, type.Locations[0], type.Name));
					return;
				}

				foreach (IParameterSymbol param in ctor.Parameters)
					if (!DbDataType.Values.Contains(param.Type.Name))
						ctx.ReportDiagnostic(Diagnostic.Create(_notNestableRule, type.Locations[0], type.Name));
			}
			foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
				if (!DbDataType.Values.Contains(property.Type.Name))
					ctx.ReportDiagnostic(Diagnostic.Create(_notNestableRule, type.Locations[0], type.Name));

		}, SymbolKind.NamedType);
	}
}