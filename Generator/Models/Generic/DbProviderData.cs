using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace DbSourceMapper.Models.Generic;

internal sealed class DbProviderData : IEquatable<DbProviderData>
{
	public string FullReaderName { get; }
	public string FullCommandName { get; }
	public ImmutableDictionary<string, string> AvailableTypes { get; }
	private DbProviderData(string fullCommandName, string fullReaderName, ImmutableDictionary<string, string> availableTypes)
	{
		FullReaderName = fullReaderName;
		FullCommandName = fullCommandName;
		AvailableTypes = availableTypes;
	}

	public static ImmutableArray<DbProviderData> CreateFromAllAttributes(GeneratorAttributeSyntaxContext context)
	{
		int length = context.Attributes.Length;
		DbProviderData[] result = new DbProviderData[length];

		for (int i = 0; i < length; i++)
		{
			INamedTypeSymbol attributeClass = context.Attributes[i].AttributeClass!;
			result[i] = attributeClass.IsGenericType
				? FromGeneric(attributeClass.TypeArguments[0])
				: FromBase(context.SemanticModel.Compilation);
		}
		return ImmutableCollectionsMarshal.AsImmutableArray(result);
	}
	private static DbProviderData FromGeneric(ITypeSymbol genericParam)
	{
		ITypeSymbol readerType = ((IMethodSymbol)genericParam
			.GetMembers("ExecuteReader")
			.First(static m => m is IMethodSymbol
			{
				ReturnType.BaseType.Name: "DbDataReader",
				DeclaredAccessibility: Accessibility.Public,
				IsGenericMethod: false,
				Parameters: []
			}))
			.ReturnType;

		ImmutableDictionary<string, string> availableTypes = readerType
			.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name.StartsWith("Get")
				&& m is { IsGenericMethod: false, Parameters: [{ Name: "ordinal", Type.Name: "Int32" }] }
				&& m.ReturnType.Name.Contains(m.Name[4..]))
			.ToImmutableDictionary(
				static m => m.ReturnType.Name,
				static m => m.Name);
		return new(genericParam.ToDisplayString(), readerType.ToDisplayString(), availableTypes);
	}

	private static DbProviderData FromBase(Compilation compilation)
	{
		ImmutableDictionary<string, string> availableTypes = ((IMethodSymbol)compilation
			.GetTypeByMetadataName("System.Data.Common.DbCommand")! //if this type is missing, the entire generator will fail anyway
			.GetMembers("ExecuteReader")
			.First(static m => m is IMethodSymbol
			{
				ReturnType.Name: "DbDataReader",
				DeclaredAccessibility: Accessibility.Public,
				IsGenericMethod: false,
				Parameters: []
			}))
			.ReturnType
			.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name.StartsWith("Get")
				&& m is
				{
					IsGenericMethod: false,
					Parameters: [{ Name: "ordinal", Type.Name: "Int32" }],
					Name: not ("GetDbDataReader" or "GetData")
				}
				&& m.ReturnType.Name.Contains(m.Name[4..]))
			.ToImmutableDictionary(
				static m => m.ReturnType.Name,
				static m => m.Name);
		return new("System.Data.Common.DbCommand", "System.Data.Common.DbDataReader", availableTypes);
	}

	public bool Equals(DbProviderData other)
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
