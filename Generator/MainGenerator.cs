using DbSourceMapper.Models;
using DbSourceMapper.Models.Generic;
using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DbSourceMapper;
[Generator]
public sealed partial class MainGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		/*
		TODO:
		- analyzer support for custom provider models
		*/
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("DbSourceModelAttribute.cs", _markerContent);
			ctx.AddSource("IDbSourceModel.cs", _interfaceContent);
			ctx.AddSource("DbCommandExtensions.cs", _extensionsContent);
			ctx.AddEmbeddedAttributeDefinition();
		});

		IncrementalValuesProvider<ModelDeclaration> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"DbSourceMapper.DbSourceModelAttribute",
				predicate: static (node, _) => node is (ClassDeclarationSyntax or RecordDeclarationSyntax) and not StructDeclarationSyntax,
				transform: static (ctx, token) => ModelDeclaration.Create(ctx, token)
			);

		context.RegisterModelSourceOutput(provider);

		IncrementalValuesProvider<GenericModelDeclaration> genericProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"DbSourceMapper.DbSourceModelAttribute`1",
				predicate: static (node, _) => node is (ClassDeclarationSyntax or RecordDeclarationSyntax) and not StructDeclarationSyntax,
				transform: static (ctx, token) => GenericModelDeclaration.Create(ctx, token)
			);

		context.RegisterModelSourceOutput(genericProvider);
	}
}