using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace OrmGenerator;

internal readonly struct MetadataModel(string name, string @namespace, ImmutableArray<(string, DataType)> properties) : IEquatable<MetadataModel>
{
	public readonly string Name = name;
	public readonly string Namespace = @namespace;
	public readonly ImmutableArray<(string Name, DataType Type)> Properties = properties;

	public bool Equals(MetadataModel other)
	{
		if (Name != other.Name || Namespace != other.Namespace)
			return false;

		for (int i = 0; i < Properties.Length; i++)
			if (Properties[i].Name != other.Properties[i].Name || Properties[i].Type != other.Properties[i].Type)
				return false;
		return true;
	}
}

