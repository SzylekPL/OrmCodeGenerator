using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrmGenerator.Models;
using OrmGenerator.Utility;

namespace OrmGenerator;

[Generator]
public sealed partial class MainGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("OrmModelAttribute.cs", _markerContent);
			ctx.AddSource("IOrmModel.cs", _interfaceContent);
			ctx.AddSource("DbCommandExtensions.cs", _extensionsContent);
			ctx.AddEmbeddedAttributeDefinition();
		});

		IncrementalValuesProvider<ModelDeclaration> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"OrmGenerator.OrmModelAttribute",
				predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax && node is not StructDeclarationSyntax,
				transform: static (ctx, _) => ModelDeclaration.Create(ctx)
			)
			.Where(static m => m is not null);

		context.RegisterModelSourceOutput(provider);
		
	}
}