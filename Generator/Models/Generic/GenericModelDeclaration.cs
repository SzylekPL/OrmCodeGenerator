using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DbSourceMapper.Models.Generic;

internal abstract class GenericModelDeclaration : IGeneratorModel<GenericModelDeclaration>
{
	private protected readonly string _name;
	private protected readonly string _namespace;
	private protected readonly bool _generateToString;
	private protected readonly bool _isRecord;
	private protected readonly ImmutableArray<Field> _fields;
	private protected readonly ImmutableArray<GenericParamModel> _paramData;

	private protected GenericModelDeclaration(string name, string @namespace, bool generateToString, bool isRecord, ImmutableArray<Field> fields, ImmutableArray<GenericParamModel> paramData)
	{
		_name = name;
		_namespace = @namespace;
		_generateToString = generateToString;
		_isRecord = isRecord;
		_fields = fields;
		_paramData = paramData;
	}

	public abstract bool Equals(GenericModelDeclaration other);
	private protected bool DataEquals(GenericModelDeclaration other)
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
		if (_paramData.SequenceEqual(other._paramData))
			return false;
		return true;
	}
	public static GenericModelDeclaration Create(in GeneratorAttributeSyntaxContext context, CancellationToken token)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
		ModelOptions options = context.GetAttributeConstructorArgument<ModelOptions>(0);

		string @namespace = string.Join(".", type.AllAncestors.Reverse().Select(s => s.Name));
		ImmutableArray<GenericParamModel> paramData = context
			.Attributes
			.Select(static a => new GenericParamModel(a.AttributeClass!.TypeArguments[0]))
			.ToImmutableArray();

		token.ThrowIfCancellationRequested();

		return type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor)
			? GenericConstructorModel.Create(context, type, @namespace, options, paramData)
			: GenericPropertyModel.Create(type, @namespace, options, paramData);
	}
	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract void RegisterModelOutput(SourceProductionContext context);
}