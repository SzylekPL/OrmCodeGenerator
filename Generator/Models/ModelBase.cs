using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DbSourceMapper.Models;

internal abstract class ModelBase : IGeneratorModel<ModelBase>
{
	private protected readonly string _name;
	private protected readonly string _namespace;
	private protected readonly bool _generateToString;
	private protected readonly bool _isRecord;
	private protected readonly ImmutableArray<Field> _fields;
	private protected readonly ImmutableArray<DbProviderData> _paramData;

	private protected ModelBase(string name, string @namespace, bool generateToString, bool isRecord, ImmutableArray<Field> fields, ImmutableArray<DbProviderData> paramData)
	{
		_name = name;
		_namespace = @namespace;
		_generateToString = generateToString;
		_isRecord = isRecord;
		_fields = fields;
		_paramData = paramData;
	}

	public abstract bool Equals(ModelBase other);
	private protected bool DataEquals(ModelBase other)
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
	public static ModelBase Create(in GeneratorAttributeSyntaxContext context, CancellationToken token)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)context.TargetSymbol;
		ModelOptions options = context.GetAttributeConstructorArgument<ModelOptions>(0);

		string @namespace = type.ContainingNamespace.ToDisplayString();
		ImmutableArray<DbProviderData> paramData = DbProviderData.CreateFromAllAttributes(context);

		token.ThrowIfCancellationRequested();

		return type.IsRecord || options.HasFlag(ModelOptions.UsePrimaryConstructor)
			? ConstructorModel.Create(context, type, @namespace, options, paramData)
			: PropertyModel.Create(type, @namespace, options, paramData);
	}
	public string FileName => $"{_namespace}.{_name}.g.cs";
	public abstract void RegisterModelOutput(SourceProductionContext context);
}