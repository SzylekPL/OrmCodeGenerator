using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace DbSourceMapper.Models.Generic;

internal sealed class GenericParamModel : IEquatable<GenericParamModel>
{
	public string FullReaderName { get; }
	public string FullCommandName { get; }
	public ImmutableDictionary<string, string> AvailableTypes { get; }

	public GenericParamModel(ITypeSymbol genericParam)
	{
		FullCommandName = genericParam.ToDisplayString(); //string.Join(".", genericParam.AllAncestorsAndSelf.Reverse().Select(s => s.Name));

		ITypeSymbol readerType = ((IMethodSymbol)genericParam
			.GetMembers("ExecuteReader")
			.First(static m => m is IMethodSymbol { ReturnType.BaseType.Name: "DbDataReader", DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false, Parameters: [] }))
			.ReturnType;
		FullReaderName = readerType.ToDisplayString();

		AvailableTypes = readerType
			.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name.StartsWith("Get")
				&& m is { IsGenericMethod: false, Parameters: [{ Name: "ordinal", Type.Name: "Int32" }] }
				&& m.ReturnType.Name.Contains(m.Name[4..]))
			.ToImmutableDictionary(
				static m => m.ReturnType.Name,
				static m => m.Name);
	}

	public bool Equals(GenericParamModel other)
	{
		if (FullReaderName != other.FullReaderName)
			return false;
		if (FullCommandName != other.FullCommandName)
			return false;
		if (!AvailableTypes.SequenceEqual(other.AvailableTypes))
			return false;
		return true;
	}

	internal void Deconstruct(out string fullCommandName, out string fullReaderName, out ImmutableDictionary<string, string> availableTypes)
	{
		fullCommandName = FullCommandName;
		fullReaderName = FullReaderName;
		availableTypes = AvailableTypes;
	}
}
