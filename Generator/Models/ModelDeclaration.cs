using System;
using System.Reflection;

namespace OrmGenerator.Models;

internal abstract class ModelDeclaration(string name, string @namespace, bool generateToString) : IEquatable<ModelDeclaration>
{
	private protected readonly string _name = name;
	private protected readonly string _namespace = @namespace;
	private protected readonly bool _generateToString = generateToString;
	public abstract bool Equals(ModelDeclaration other);
	public abstract string SourceCode { get; }
	public string FileName => $"{_namespace}.{_name}.g.cs";
}
