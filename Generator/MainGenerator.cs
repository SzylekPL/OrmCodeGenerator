using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrmGenerator.Models;
using System.Diagnostics;

namespace OrmGenerator;

[Generator]
public sealed partial class MainGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("OrmModelAttribute.g.cs", _markerContent);
			ctx.AddSource("IOrmModel.g.cs", _interfaceContent);
			ctx.AddSource("DbCommandExtensions.g.cs", _extensionsContent);
		});

		IncrementalValuesProvider<ModelDeclaration> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"OrmGenerator.OrmModelAttribute",
				predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
				transform: static (ctx, _) => ModelDeclaration.Create(ctx)
			)
			.Where(static m => m is not null)!;
		context.RegisterSourceOutput(provider, static (spc, model) =>
		{
			Debug.WriteLine(model.FileName);
			Debug.WriteLine(model.SourceCode);
			spc.AddSource(model.FileName, model.SourceCode);
		});
	}
}