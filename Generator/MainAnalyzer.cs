using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Shared;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Shared.ProjectDiagnostics;

namespace DbSourceMapper;

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
			ImmutableArray<AttributeData> modelAttributes = GetModelAttributes(type);

			if (modelAttributes is [])
				return;

			if (node.IsKind(SyntaxKind.ClassDeclaration) && !modelAttributes.Any(HasPrimaryConstructorFlag))
				return;

			if (!node.ChildNodes().Any(static n => n.IsKind(SyntaxKind.ParameterList)))
				ctx.ReportDiagnostic(Diagnostic.Create(_ctorNotSuitableRule, node.GetLocation(), type.Name));

		}, SyntaxKind.RecordDeclaration, SyntaxKind.ClassDeclaration);

		context.RegisterCompilationStartAction(static ctx =>
		{
			//todo: use this
			ConcurrentDictionary<string, HashSet<string>> cachedTypes = [];

			ctx.RegisterSymbolAction(c =>
			{
				INamedTypeSymbol type = (INamedTypeSymbol)c.Symbol;

				ImmutableArray<AttributeData> attributes = GetModelAttributes(type);

				if (attributes is [])
					return;

				IEnumerable<(string Name, HashSet<string> parameters)> types = attributes
					.Select(static a => a.AttributeClass!.TypeParameters)
					.Where(static t => t is not [])
					.Select(static t =>
					{
						ITypeParameterSymbol arg = t[0];
						HashSet<string> parameters = new(((IMethodSymbol)arg.GetMembers("GetReader")
							.First(static m => m is IMethodSymbol { ReturnType.BaseType.Name: "DbDataReader", Parameters: [] }))
							.ReturnType
							.GetMembers()
							.Where(static m => m is IMethodSymbol { IsGenericMethod: false, Parameters: [{ Type.Name: "Int32", Name: "ordinal" }] } ms && ms.Name.StartsWith("Get"))
							.Select(static m => ((IMethodSymbol)m).ReturnType.Name));

						return (arg.Name, parameters);
					});

				foreach ((string Name, HashSet<string> parameters) in types)
					cachedTypes.TryAdd(Name, parameters);

			}, SymbolKind.NamedType);
		});

		context.RegisterSyntaxNodeAction(static ctx =>
		{
			if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node.Parent!) is not INamedTypeSymbol type)
				return;
			ImmutableArray<AttributeData> modelAttributes = GetModelAttributes(type);
			if (modelAttributes is [])
				return;

			if (!modelAttributes.Any(HasPrimaryConstructorFlag) && !type.IsRecord)
				return;

			ParameterListSyntax paramList = (ParameterListSyntax)ctx.Node;

			if (paramList.Parameters is [])
			{
				ctx.ReportDiagnostic(Diagnostic.Create(_ctorNotSuitableRule, type.Locations[0], type.Name));
				return;
			}

			foreach (ParameterSyntax param in paramList.Parameters)
			{
				IParameterSymbol paramSymbol = ctx.SemanticModel.GetDeclaredSymbol(param, ctx.CancellationToken)!;

				if (!Constants.DefaultDbDataTypes.Contains(paramSymbol.Type.Name) && !HasModelAttribute(paramSymbol.Type))
					ctx.ReportDiagnostic(Diagnostic.Create(_typeNotSupportedRule, param.GetLocation(), paramSymbol.Type.Name, paramSymbol.Name));
			}

		}, SyntaxKind.ParameterList);

		context.RegisterSymbolAction(static ctx =>
		{
			IPropertySymbol prop = (IPropertySymbol)ctx.Symbol;

			Accessibility accessibility = prop.DeclaredAccessibility;
			if (prop.SetMethod is null || accessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
				return;

			if (!HasModelAttribute(prop.ContainingType))
				return;

			if (Constants.DefaultDbDataTypes.Contains(prop.Type.Name) || HasModelAttribute(prop.Type))
				return;

			ctx.ReportDiagnostic(Diagnostic.Create(_typeNotSupportedRule, prop.Locations[0], prop.Type.Name, prop.Name));

		}, SymbolKind.Property);
	}

	private static bool HasPrimaryConstructorFlag(AttributeData attribute) => ((ModelOptions)attribute
		.ConstructorArguments[0]
		.Value!)
		.HasFlag(ModelOptions.UsePrimaryConstructor);
	private static ImmutableArray<AttributeData> GetModelAttributes(ITypeSymbol type) => type
		.GetAttributes()
		.Where(static attr => attr.AttributeClass?.Name is "DbSourceModelAttribute" or "DbSourceModelAttribute`1")
		.ToImmutableArray();
	private static bool HasModelAttribute(ITypeSymbol type) => type
		.GetAttributes()
		.Any(static a => a.AttributeClass?.Name is "DbSourceModelAttribute" or "DbSourceModelAttribute`1");
}