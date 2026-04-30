using OrmGenerator.Utility;

namespace OrmGenerator.Models.Property;

internal readonly struct StandardProperty(string name, DbDataType type) : IProperty
{
	public readonly string Name = name;
	public readonly DbDataType Type = type;

	public bool Equals(in IProperty other) => other is StandardProperty p && Name == p.Name && Type == p.Type;
	string IProperty.Name => Name;
}
