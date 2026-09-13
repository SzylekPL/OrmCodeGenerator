using DbSourceMapper.Utility;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace DbSourceMapper.Models.Generic;

internal sealed class GenericParamModel : IEquatable<GenericParamModel>
{
	public string FullReaderName { get; }
	public string FullCommandName { get; }
	public ImmutableHashSet<string> AvailableTypes { get; }

	public GenericParamModel(ITypeSymbol genericParam)
	{
		FullCommandName = string.Join(".", genericParam.AllAncestorsAndSelf.Reverse().Select(s => s.Name));

		ITypeSymbol readerType = ((IMethodSymbol)genericParam
			.GetMembers("ExecuteReader")
			.First(static m => m is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false, Parameters: [] }))
			.ReturnType;
		FullReaderName = string.Join(".", readerType.AllAncestorsAndSelf.Reverse().Select(s => s.Name));

		AvailableTypes = readerType
			.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name.StartsWith("Get") 
				&& m is { IsGenericMethod: false, Parameters: [{ Name: "ordinal", Type.Name: "Int32" }] } 
				&& m.ReturnType.Name.Contains(m.Name[4..]))
			.Select(static m => m.ReturnType.Name)
			.ToImmutableHashSet();
	}

	public bool Equals(GenericParamModel other)
	{
		if (FullReaderName != other.FullReaderName)
			return false;
		if (FullCommandName != other.FullCommandName)
			return false;
		if (!AvailableTypes.SetEquals(other.AvailableTypes))
			return false;
		return true;
	}

	internal void Deconstruct(out string fullCommandName, out string fullReaderName, out ImmutableHashSet<string> availableTypes)
	{
		fullCommandName = FullCommandName;
		fullReaderName = FullReaderName;
		availableTypes = AvailableTypes;
	}
}
