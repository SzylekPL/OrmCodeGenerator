namespace OrmGenerator.Models.Property;

internal readonly struct CustomField(string name, string type) : IField
{
	public readonly string Name = name;
	public readonly string Type = type;

	public bool Equals(in IField other) => other is CustomField p && Name == p.Name && Type == p.Type;
	string IField.Name => Name;
}
