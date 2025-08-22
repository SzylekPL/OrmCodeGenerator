using System;
using System.Collections.Immutable;

namespace OrmGenerator;

internal readonly struct NestableMetadataModel(string name, string @namespace, ImmutableArray<(string, DataType, string?)> properties) : IEquatable<NestableMetadataModel>
{
	public readonly string Name = name;
	public readonly string Namespace = @namespace;
	public readonly ImmutableArray<(string Name, DataType Type, string? CustomType)> Properties = properties;

	public bool Equals(NestableMetadataModel other)
	{
		if (Name != other.Name || Namespace != other.Namespace)
			return false;

		for (int i = 0; i < Properties.Length; i++)
			if (Properties[i].Name != other.Properties[i].Name || Properties[i].Type != other.Properties[i].Type || Properties[i].CustomType != other.Properties[i].CustomType)
				return false;
		return true;
	}
}