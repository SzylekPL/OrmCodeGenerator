using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

		context.RegisterSyntaxNodeAction(static ctx =>
		{
			SyntaxNode node = ctx.Node;
			INamedTypeSymbol type = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(node)!;

			if (GetModelAttribute(type) is not AttributeData attribute)
				return;

			switch (node.Kind())
			{
				case SyntaxKind.RecordStructDeclaration:
					ctx.ReportDiagnostic(Diagnostic.Create(_structNotAllowedRule, node.GetLocation(), type.Name));
					return;
				case SyntaxKind.ClassDeclaration:
					if (!((ModelOptions)attribute.ConstructorArguments[0].Value!).HasFlag(ModelOptions.UsePrimaryConstructor))
						return;
					goto case SyntaxKind.RecordDeclaration;
				case SyntaxKind.RecordDeclaration:
					if (!node.ChildNodes().Any(static n => n.IsKind(SyntaxKind.ParameterList)))
						ctx.ReportDiagnostic(Diagnostic.Create(_ctorNotSuitableRule, node.GetLocation(), type.Name));
					return;
			}

		}, SyntaxKind.RecordDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.RecordStructDeclaration);

		context.RegisterSyntaxNodeAction(static ctx =>
		{
			if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node.Parent!) is not INamedTypeSymbol type)
				return;
			if (GetModelAttribute(type) is not AttributeData attribute)
				return;

			if (!((ModelOptions)attribute.ConstructorArguments[0].Value!).HasFlag(ModelOptions.UsePrimaryConstructor) && !type.IsRecord)
				return;

			ParameterListSyntax paramList = (ParameterListSyntax)ctx.Node;

			if (paramList.Parameters.Count == 0)
			{
				ctx.ReportDiagnostic(Diagnostic.Create(_ctorNotSuitableRule, type.Locations[0], type.Name));
				return;
			}

			foreach (ParameterSyntax param in paramList.Parameters)
			{
				IParameterSymbol paramSymbol = ctx.SemanticModel.GetDeclaredSymbol(param, ctx.CancellationToken)!;

				if (!Constants.DefaultDbDataTypes.Contains(paramSymbol.Type.Name) && !HasModelAttribute(paramSymbol.Type))
					ctx.ReportDiagnostic(Diagnostic.Create(_notMarkedRule, param.GetLocation(), paramSymbol.Type.Name, paramSymbol.Name));
			}

		}, SyntaxKind.ParameterList);

		context.RegisterSymbolAction(static ctx =>
		{
			IPropertySymbol prop = (IPropertySymbol)ctx.Symbol;

			if (!HasModelAttribute(prop.ContainingType))
				return;

			if (Constants.DefaultDbDataTypes.Contains(prop.Type.Name) || HasModelAttribute(prop.Type))
				return;

			ctx.ReportDiagnostic(Diagnostic.Create(_notMarkedRule, prop.Locations[0], prop.Type.Name, prop.Name));

		}, SymbolKind.Property);
	}

	private static AttributeData? GetModelAttribute(ITypeSymbol type) => type
		.GetAttributes()
		.FirstOrDefault(static attr => attr.AttributeClass?.Name == "OrmModelAttribute");
	private static bool HasModelAttribute(ITypeSymbol type) => type
		.GetAttributes()
		.Any(static a => a.AttributeClass?.Name == "OrmModelAttribute");
}