namespace OrmGenerator.Models;

internal readonly struct Field(string name, string type)
{
	public readonly string Name = name;
	public readonly string Type = type;

	public bool Equals(in Field other) => Name == other.Name && Type == other.Type;
}
