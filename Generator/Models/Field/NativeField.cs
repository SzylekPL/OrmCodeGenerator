using OrmGenerator.Models.Property;
using Shared;

namespace OrmGenerator.Models.Field;

internal readonly struct NativeField(string name, DbDataType type) : IField
{
	public readonly string Name = name;
	public readonly DbDataType Type = type;

	public bool Equals(in IField other) => other is NativeField p && Name == p.Name && Type == p.Type;
	string IField.Name => Name;
}
