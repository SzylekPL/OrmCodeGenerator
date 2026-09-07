using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shared;
using System.Collections.Immutable;
using System.Linq;

namespace DbSourceMapper.Models;

internal abstract class ModelDeclaration(string name, string @namespace, bool generateToString, bool isRecord, ImmutableArray<Field> fields)
	: IGeneratorModel<ModelDeclaration>
{
	private protected readonly string _name = name;
	private protected readonly string _namespace = @namespace;
	private protected readonly bool _generateToString = generateToString;
	private protected readonly bool _isRecord = isRecord;
	private protected readonly ImmutableArray<Field> _fields = fields;


	public static ModelDeclaration Create(in GeneratorAttributeSyntaxContext context)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
		ModelOptions options = context.GetAttributeConstructorArgument<ModelOptions>(0);
		string @namespace = string.Join(".", type.AllAncestors.Reverse().Select(s => s.Name));

		return type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor)
			? CreateFromConstructor(context, type, @namespace, options)
			: CreateFromProperties(type, @namespace, options);
	}

	private static ModelDeclaration CreateFromConstructor(in GeneratorAttributeSyntaxContext context, INamedTypeSymbol type, string @namespace, ModelOptions options)
	{
		SemanticModel semanticModel = context.SemanticModel;

		ParameterListSyntax paramList = (ParameterListSyntax)context.TargetNode
			.ChildNodes()
			.FirstOrDefault(static n => n is ParameterListSyntax);

		ImmutableArray<Field> @params = paramList
			.Parameters
			.Select(p => (IParameterSymbol)semanticModel.GetDeclaredSymbol(p)!)
			.Select(static p => new Field(
				p.Name,
				Constants.DefaultDbDataTypes.Contains(p.Type.Name) ? string.Intern(p.Type.Name) : p.Type.Name)
			)
			.ToImmutableArray();
		return new ConstructorModel(type.Name, @namespace, options.HasFlag(ModelOptions.GenerateToString), type.IsRecord, @params);
	}

	private static ModelDeclaration CreateFromProperties(INamedTypeSymbol type, string @namespace, ModelOptions options)
	{
		ImmutableArray<Field> props = type
			.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(static p => p.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal && p.SetMethod is not null)
			.Select(static p => new Field(
				p.Name,
				Constants.DefaultDbDataTypes.Contains(p.Type.Name) ? string.Intern(p.Type.Name) : p.Type.Name)
			)
			.ToImmutableArray();
		return new PropertyModel(type.Name, @namespace, options.HasFlag(ModelOptions.GenerateToString), type.IsRecord, props);
	}

	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract bool Equals(ModelDeclaration other);
	public abstract void RegisterModelOutput(SourceProductionContext context);
}