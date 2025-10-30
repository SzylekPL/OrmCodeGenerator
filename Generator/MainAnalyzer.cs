using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using OrmGenerator.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace OrmGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MainAnalyzer : DiagnosticAnalyzer
{
	private static readonly HashSet<string> DbDataTypes = new(Enum.GetNames(typeof(DbDataType)));
	private static readonly DiagnosticDescriptor NotMarkedRule = new(
		id: "ORM001",
		title: "Model must be marked",
		messageFormat: "The type {0} of property {1} must be a marked model",
		category: "Syntax",
		description: "The property is neither a standard database type, nor a model marked with OrmModelAttribute or NestableOrmModelAttribute.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	private static readonly DiagnosticDescriptor NonBasicRule = new(
		id: "ORM002",
		title: "Nestable not necessary",
		messageFormat: "The model of type {0} doesn't contain other models and shouldn't be nestable",
		category: "Syntax",
		description: "The type doesn't contain other models in it's definition. Consider changing the attribute from NestableOrmModelAttribute to OrmModelAttribute.",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);
	private static readonly DiagnosticDescriptor NonNestableRule = new(
		id: "ORM003",
		title: "Nestable required",
		messageFormat: "The model of type {0} isn't nestable and mustn't contain other models",
		category: "Syntax",
		description: "The type isn't nestable, yet it contains other models in it's definition. Change the attribute from OrmModelAttribute to NestableOrmModelAttribute or remove unsupported properties.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [NotMarkedRule, NonBasicRule, NonNestableRule];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSymbolAction(static ctx =>
		{
			IPropertySymbol property = (IPropertySymbol)ctx.Symbol;

			if (DbDataTypes.Contains(property.Type.Name))
				return;

			if (property
				.Type
				.GetAttributes()
				.Any(static attr => attr.AttributeClass?.Name is "OrmModelAttribute" or "NestableOrmModelAttribute"))

				return;

			Diagnostic diagnostic = Diagnostic.Create(NotMarkedRule, property.Locations[0], property.Type.Name, property.Name);
			ctx.ReportDiagnostic(diagnostic);

		}, SymbolKind.Property);

		context.RegisterSymbolAction(static ctx =>
		{
			INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Symbol;

			ImmutableArray<AttributeData> attributeData = namedType.GetAttributes();
			if (attributeData.Any(static attr => attr.AttributeClass?.Name == "NestableOrmModelAttribute"))
			{
				foreach (IPropertySymbol property in namedType.GetMembers().OfType<IPropertySymbol>())
					if (!DbDataTypes.Contains(property.Type.Name))
						return;

				Diagnostic diagnostic = Diagnostic.Create(NonBasicRule, namedType.Locations[0], namedType.Name);
				ctx.ReportDiagnostic(diagnostic);
			}
			else if (attributeData.Any(static attr => attr.AttributeClass?.Name == "OrmModelAttribute"))
			{
				foreach (IPropertySymbol property in namedType.GetMembers().OfType<IPropertySymbol>())
					if (!DbDataTypes.Contains(property.Type.Name))
					{
						Diagnostic diagnostic = Diagnostic.Create(NonNestableRule, namedType.Locations[0], namedType.Name);
						ctx.ReportDiagnostic(diagnostic);
						return;
					}
			}

		}, SymbolKind.NamedType);
	}
}
