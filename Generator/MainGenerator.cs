using DbSourceMapper.Models;
using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DbSourceMapper;

[Generator]
public sealed partial class MainGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("DbSourceModelAttribute.cs", _markerContent);
			ctx.AddSource("IOrmModel.cs", _interfaceContent);
			ctx.AddSource("DbCommandExtensions.cs", _extensionsContent);
			ctx.AddEmbeddedAttributeDefinition();
		});

		IncrementalValuesProvider<ModelDeclaration> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"DbSourceMapper.DbSourceModelAttribute",
				predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax && node is not StructDeclarationSyntax,
				transform: static (ctx, token) => ModelDeclaration.Create(ctx,token)
			);

		context.RegisterModelSourceOutput(provider);

	}
}