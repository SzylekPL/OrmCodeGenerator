using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DbSourceMapper.Models;

internal abstract class ModelDeclaration(string name, string @namespace, bool generateToString, bool isRecord, ImmutableArray<Field> fields)
	: IGeneratorModel<ModelDeclaration>
{
	private protected readonly string _name = name;
	private protected readonly string _namespace = @namespace;
	private protected readonly bool _generateToString = generateToString;
	private protected readonly bool _isRecord = isRecord;
	private protected readonly ImmutableArray<Field> _fields = fields;

	public abstract bool Equals(ModelDeclaration other);
	private protected bool DataEquals(ModelDeclaration other)
	{
		if (_name != other._name)
			return false;
		if (_namespace != other._namespace)
			return false;
		if (_generateToString != other._generateToString)
			return false;
		if (_isRecord != other._isRecord)
			return false;
		if (!_fields.SequenceEqual(other._fields))
			return false;
		return true;
	}
	public static ModelDeclaration Create(in GeneratorAttributeSyntaxContext context, CancellationToken token)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
		ModelOptions options = context.GetAttributeConstructorArgument<ModelOptions>(0);
		string @namespace = string.Join(".", type.AllAncestors.Reverse().Select(s => s.Name));

		token.ThrowIfCancellationRequested();

		return type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor)
			? ConstructorModel.Create(context, type, @namespace, options)
			: PropertyModel.Create(type, @namespace, options);
	}
	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract void RegisterModelOutput(SourceProductionContext context);
}