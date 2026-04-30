using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrmGenerator.Models;
using OrmGenerator.Models.Property;
using OrmGenerator.Utility;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

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
			.ForAttributeWithMetadataName<ModelDeclaration>(
				"OrmGenerator.OrmModelAttribute",
				predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
				transform: static (context, token) =>
				{
					INamedTypeSymbol @class = (INamedTypeSymbol)context.TargetSymbol;
					bool generateToString = GetBoolAttributeProperty(context, "GenerateToString");
					bool disableNesting = GetBoolAttributeProperty(context, "DisableNesting");
					ImmutableArray<IPropertySymbol> allProperties = @class
						.GetMembers()
						.OfType<IPropertySymbol>()
						.Where(static p => p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal && p.SetMethod is not null)
						.ToImmutableArray();

					//todo: record handling
					if (disableNesting || allProperties.All(p => Constants.DbDataTypes.Contains(p.Type.Name)))
					{
						ImmutableArray<StandardProperty> prop = allProperties
							.Select(static p => new StandardProperty(p.Name, (DbDataType)Enum.Parse(typeof(DbDataType), p.Type.Name)))
							.ToImmutableArray();
						return new ClassModelDeclaration(@class.Name, @class.ContainingNamespace.Name, prop, generateToString);
					}

					ImmutableArray<IProperty> props = allProperties
							.Select(static p => (IProperty)(Enum.TryParse(p.Type.Name, out DbDataType type)
								? new StandardProperty(p.Name, type)
								: new CustomProperty(p.Name, p.Type.Name)))
							.ToImmutableArray();
					return new NestableClassModelDeclaration(@class.Name, @class.ContainingNamespace.Name, props, generateToString);
				}
			);
		context.RegisterSourceOutput(provider, static (spc, model) => 
			spc.AddSource(model.FileName, model.SourceCode)
		);
	}

	private static bool GetBoolAttributeProperty(in GeneratorAttributeSyntaxContext context, string property) => (bool)(context
		.Attributes[0]
		.NamedArguments
		.FirstOrDefault(p => p.Key == property)
		.Value
		.Value ?? false);
}