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
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("DbSourceModelAttribute.cs", _markerContent);
			ctx.AddSource("IOrmModel.cs", _interfaceContent);
			ctx.AddSource("DbCommandExtensions.cs", _extensionsContent);
			ctx.AddEmbeddedAttributeDefinition();
		});
		//IncrementalValueProvider<ImmutableDictionary<string, ImmutableHashSet<string>>> configProvider = context
		//	.AdditionalTextsProvider
		//	.Where(static a => a.Path.EndsWith(".dsm.txt"))
		//	.Select(static (t, token) => (
		//		Path.GetFileNameWithoutExtension(t.Path),
		//		t.GetText(token)!
		//			.Lines
		//			.Select(static l => l.Text?.ToString())
		//			.OfType<string>()
		//			.ToImmutableHashSet()))
		//	.Where(static p => p.Item2 is not null)
		//	.Collect()!
		//	.Select(static (a, token) => a.ToImmutableDictionary(
		//		static t => t.Item1,
		//		static t => t.Item2)
		//	);

		IncrementalValuesProvider<ModelDeclaration> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"DbSourceMapper.DbSourceModelAttribute",
				predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax && node is not StructDeclarationSyntax,
				transform: static (ctx, token) => ModelDeclaration.Create(ctx, token)
			);

		context.RegisterModelSourceOutput(provider);

		IncrementalValuesProvider<GenericModelDeclaration> genericProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"DbSourceMapper.DbSourceModelAttribute`1",
				predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax && node is not StructDeclarationSyntax,
				transform: static (ctx, token) => GenericModelDeclaration.Create(ctx, token)
			);

		context.RegisterModelSourceOutput(genericProvider);
	}
}