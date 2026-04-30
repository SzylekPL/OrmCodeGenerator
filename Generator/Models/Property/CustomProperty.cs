namespace OrmGenerator.Models.Property;

internal readonly struct CustomProperty(string name, string type) : IProperty
{
	public readonly string Name = name;
	public readonly string Type = type;

	public bool Equals(in IProperty other) => other is CustomProperty p && Name == p.Name && Type == p.Type;
	string IProperty.Name => Name;
}
