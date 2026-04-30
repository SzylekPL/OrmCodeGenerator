namespace OrmGenerator.Models.Property;

internal interface IProperty
{
	public string Name { get; }
	public bool Equals(in IProperty other);
}