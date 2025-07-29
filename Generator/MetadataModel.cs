using System.Collections.Immutable;

namespace OrmGenerator;

internal readonly struct MetadataModel(string name, string @namespace, ImmutableArray<(string, DataType)> properties)
{
	public readonly string Name = name;
	public readonly string Namespace = @namespace;
	public readonly ImmutableArray<(string Name, DataType Type)> Properties = properties;
}