using System;
using System.Collections.Immutable;

namespace OrmGenerator.Models;

internal readonly struct MetadataModel(string name, string @namespace, ImmutableArray<(string, DbDataType)> properties, bool generateToString) : IEquatable<MetadataModel>
{
	public readonly string Name = name;
	public readonly string Namespace = @namespace;
	public readonly bool GenerateToString = generateToString;
	public readonly ImmutableArray<(string Name, DbDataType Type)> Properties = properties;

	public bool Equals(MetadataModel other)
	{
		if (Name != other.Name || Namespace != other.Namespace || Properties.Length != other.Properties.Length)
			return false;

		for (int i = 0; i < Properties.Length; i++)
			if (Properties[i].Name != other.Properties[i].Name || Properties[i].Type != other.Properties[i].Type)
				return false;
		return true;
	}

	public void Deconstruct(out string name, out string @namespace, out bool generateToString, out ImmutableArray<(string Name, DbDataType Type)> properties)
	{
		name = Name;
		@namespace = Namespace;
		generateToString = GenerateToString;
		properties = Properties;
	}
}

