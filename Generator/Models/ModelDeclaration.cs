using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrmGenerator.Models.Property;
using OrmGenerator.Utility;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace OrmGenerator.Models;

internal abstract class ModelDeclaration(string name, string @namespace, bool generateToString) : IEquatable<ModelDeclaration>
{
	private protected readonly string _name = name;
	private protected readonly string _namespace = @namespace;
	private protected readonly bool _generateToString = generateToString;

	public static ModelDeclaration? Create(in GeneratorAttributeSyntaxContext context)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
		ModelOptions options = context.GetAttributeConstructorArgument<ModelOptions>(0);

		return type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor)
			? CreateRecord(context, type, options)
			: CreateClass(type, options);
	}

	private static ModelDeclaration? CreateRecord(in GeneratorAttributeSyntaxContext context, INamedTypeSymbol type, ModelOptions options)
	{
		SemanticModel semanticModel = context.SemanticModel;
		ImmutableArray<IParameterSymbol> allParameters = ((RecordDeclarationSyntax)context.TargetNode)
			?.ParameterList
			?.Parameters
			.Select(p => (IParameterSymbol)semanticModel.GetDeclaredSymbol(p)!)
			.ToImmutableArray() ?? [];

		//todo: record handling
		bool primitiveOnly = allParameters.All(p => DbDataType.Values.Contains(p.Type.Name));
		if (options.HasFlag(ModelOptions.DisableNesting) || primitiveOnly)
		{
			if (options.HasFlag(ModelOptions.DisableNesting) != primitiveOnly)
				return null;
			ImmutableArray<StandardProperty> prop = allParameters
				.Select(static p => new StandardProperty(p.Name, (DbDataType)Enum.Parse(typeof(DbDataType), p.Type.Name)))
				.ToImmutableArray();
			return new RecordModelDeclaration(type.Name, type.ContainingNamespace.Name, prop, options.HasFlag(ModelOptions.GenerateToString));
		}

		ImmutableArray<IProperty> props = allParameters
				.Select(static p => (IProperty)(Enum.TryParse(p.Type.Name, out DbDataType type)
					? new StandardProperty(p.Name, type)
					: new CustomProperty(p.Name, p.Type.Name)))
				.ToImmutableArray();
		return new NestableRecordModelDeclaration(type.Name, type.ContainingNamespace.Name, props, options.HasFlag(ModelOptions.GenerateToString));
	}

	private static ModelDeclaration? CreateClass(INamedTypeSymbol @class, ModelOptions options)
	{
		ImmutableArray<IPropertySymbol> allProperties = @class
					.GetMembers()
					.OfType<IPropertySymbol>()
					.Where(static p => p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal && p.SetMethod is not null)
					.ToImmutableArray();

		//todo: record handling
		if (options.HasFlag(ModelOptions.DisableNesting) || allProperties.All(p => DbDataType.Values.Contains(p.Type.Name)))
		{
			ImmutableArray<StandardProperty> prop = allProperties
				.Select(static p => new StandardProperty(p.Name, (DbDataType)Enum.Parse(typeof(DbDataType), p.Type.Name)))
				.ToImmutableArray();
			return new ClassModelDeclaration(@class.Name, @class.ContainingNamespace.Name, prop, options.HasFlag(ModelOptions.GenerateToString));
		}

		ImmutableArray<IProperty> props = allProperties
				.Select(static p => (IProperty)(Enum.TryParse(p.Type.Name, out DbDataType type)
					? new StandardProperty(p.Name, type)
					: new CustomProperty(p.Name, p.Type.Name)))
				.ToImmutableArray();
		return new NestableClassModelDeclaration(@class.Name, @class.ContainingNamespace.Name, props, options.HasFlag(ModelOptions.GenerateToString));
	}

	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract bool Equals(ModelDeclaration other);
	public abstract string SourceCode { get; }
}