using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrmGenerator.Models.Property;
using OrmGenerator.Utility;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace OrmGenerator.Models;

internal abstract class ModelDeclaration(string name, string @namespace, bool generateToString) : IEquatable<ModelDeclaration>
{
	private protected readonly string _name = name;
	private protected readonly string _namespace = @namespace;
	private protected readonly bool _generateToString = generateToString;

	public static ModelDeclaration Create(in GeneratorAttributeSyntaxContext context)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
		bool generateToString = GetBoolAttributeProperty(context, "GenerateToString");
		bool disableNesting = GetBoolAttributeProperty(context, "DisableNesting");


		if (type.IsRecord)
		{
			return CreateRecord(context, type, generateToString, disableNesting);
		}
		else
		{
			return CreateClass(type, generateToString, disableNesting);
		}
	}

	private static ModelDeclaration CreateRecord(in GeneratorAttributeSyntaxContext context,  INamedTypeSymbol type, bool generateToString, bool disableNesting)
	{
		SemanticModel semanticModel = context.SemanticModel;
			ImmutableArray<IParameterSymbol> allParameters = ((RecordDeclarationSyntax)context.TargetNode)
			?.ParameterList
			?.Parameters
			.Select(p => (IParameterSymbol)semanticModel.GetDeclaredSymbol(p)!)
			.ToImmutableArray() ?? [];

		//todo: record handling
		if (disableNesting || allParameters.All(p => Constants.DbDataTypes.Contains(p.Type.Name)))
		{
			ImmutableArray<StandardProperty> prop = allParameters
				.Select(static p => new StandardProperty(p.Name, (DbDataType)Enum.Parse(typeof(DbDataType), p.Type.Name)))
				.ToImmutableArray();
			return new RecordModelDeclaration(type.Name, type.ContainingNamespace.Name, prop, generateToString);
		}

		ImmutableArray<IProperty> props = allParameters
				.Select(static p => (IProperty)(Enum.TryParse(p.Type.Name, out DbDataType type)
					? new StandardProperty(p.Name, type)
					: new CustomProperty(p.Name, p.Type.Name)))
				.ToImmutableArray();
		return new NestableRecordModelDeclaration(type.Name, type.ContainingNamespace.Name, props, generateToString);
	}

	private static ModelDeclaration CreateClass(INamedTypeSymbol @class, bool generateToString, bool disableNesting)
	{
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

	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract bool Equals(ModelDeclaration other);
	public abstract string SourceCode { get; }

	private static bool GetBoolAttributeProperty(in GeneratorAttributeSyntaxContext context, string property) => (bool)(context
	.Attributes[0]
	.NamedArguments
	.FirstOrDefault(p => p.Key == property)
	.Value
	.Value ?? false);
}