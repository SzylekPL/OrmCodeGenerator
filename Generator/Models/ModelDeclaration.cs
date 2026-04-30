using Microsoft.CodeAnalysis;
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

	private static bool GetBoolAttributeProperty(in GeneratorAttributeSyntaxContext context, string property) => (bool)(context
		.Attributes[0]
		.NamedArguments
		.FirstOrDefault(p => p.Key == property)
		.Value
		.Value ?? false);
	public static ModelDeclaration Create(in GeneratorAttributeSyntaxContext context)
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
	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract bool Equals(ModelDeclaration other);
	public abstract string SourceCode { get; }
}