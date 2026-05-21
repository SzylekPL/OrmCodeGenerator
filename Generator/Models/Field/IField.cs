namespace OrmGenerator.Models.Property;

internal interface IField
{
	public string Name { get; }
	public bool Equals(in IField other);
}