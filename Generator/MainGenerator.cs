using Microsoft.CodeAnalysis;
using OrmGenerator.Models;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace OrmGenerator;

[Generator]
public class MainGenerator : IIncrementalGenerator
{
	private const string _baseMarker = "OrmGenerator.OrmModelAttribute";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		//context.RegisterMarkerAttributes(
		//	("OrmCodeGenerator", "OrmModelAttribute", AttributeTargets.Class),
		//	("OrmCodeGenerator", "NestableOrmModelAttribute", AttributeTargets.Class)
		//);
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("OrmModelAttribute.g.cs",
				"""
				using System;
				namespace OrmGenerator;

				[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
				internal class OrmModelAttribute : Attribute
				{
					public bool GenerateToString { get; set; } = false;
				}

				internal sealed class NestableOrmModelAttribute : OrmModelAttribute;
				""");
			ctx.AddSource("IOrmModel.g.cs",
				"""
				using System.Data.Common;
				namespace OrmGenerator;

				public interface IOrmModel<TSelf>
				{
					internal static abstract TSelf GetSingleModel(DbDataReader reader);
				}
				""");

			ctx.AddSource("DbCommandExtensions.g.cs",
				"""
				using System.Data;
				using System.Data.Common;
				namespace OrmGenerator;
				
				public static class DbCommandExtensions
				{
					public static T? GetSingle<T>(this DbCommand command) where T: IOrmModel<T>?
					{
						using DbDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);
						return reader.Read() 
							? T.GetSingleModel(reader) 
							: default;
					}

					public static async Task<T?> GetSingleAsync<T>(this DbCommand command, CancellationToken token = default) where T: IOrmModel<T>?
					{
						using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
						return await reader.ReadAsync(token) 
							? T.GetSingleModel(reader) 
							: default;
					}

					public static List<T> GetListOf<T>(this DbCommand command) where T: IOrmModel<T>
					{
						List<T> result = [];
						using DbDataReader reader = command.ExecuteReader();
				
						while(reader.Read())
							result.Add(T.GetSingleModel(reader));
						return result;
					}

					public static async Task<List<T>> GetListOfAsync<T>(this DbCommand command, CancellationToken token = default) where T: IOrmModel<T>
					{
						List<T> result = [];
						using DbDataReader reader = await command.ExecuteReaderAsync(token);
				
						while(await reader.ReadAsync(token))
							result.Add(T.GetSingleModel(reader));
						return result;
					}

					public static IEnumerable<T> GetEnumerableOf<T>(this DbCommand command) where T: IOrmModel<T>
					{
						using DbDataReader reader = command.ExecuteReader();
				
						while(reader.Read())
							yield return T.GetSingleModel(reader);
					}

					public static async IAsyncEnumerable<T> GetAsyncEnumerableOf<T>(this DbCommand command, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default) where T: IOrmModel<T>
					{
						using DbDataReader reader = await command.ExecuteReaderAsync(token);
				
						while(await reader.ReadAsync(token))
							yield return T.GetSingleModel(reader);
					}
				}
				""");
		});

		IncrementalValuesProvider<MetadataModel> provider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				_baseMarker,
				predicate: static (node, _) => true,
				transform: static (context, token) =>
				{
					INamedTypeSymbol @class = (INamedTypeSymbol)context.TargetSymbol;
					bool generateToString = GetToStringInfo(context);
					ImmutableArray<(string, DbDataType)> prop = @class
						.GetMembers()
						.OfType<IPropertySymbol>()
						.Where(static p => p.DeclaredAccessibility != Accessibility.Private && p.SetMethod is not null)
						.Select(static p => Enum.TryParse(p.Type.Name, true, out DbDataType dataType)
								? (p.Name, dataType)
								: (p.Name, DbDataType.Unknown))
						.ToImmutableArray();
					return new MetadataModel(@class.Name, @class.ContainingNamespace.Name, prop, generateToString);
				}
			);
		context.RegisterSourceOutput(provider, static (spc, model) =>
		{
			StringBuilder builder = new();
			(string name,
			string @namespace,
			bool generateToString,
			ImmutableArray<(string Name, DbDataType Type)> properties) = model;

			builder.AppendLine(
			$$"""
			using System.Data.Common;
			using OrmGenerator;

			namespace {{@namespace}};
			partial class {{name}} : IOrmModel<{{name}}>
			{
				public static {{name}} GetSingleModel(DbDataReader reader, ref int index) => new()
				{
			""");

			foreach ((string Name, DbDataType _) prop in properties)
			{
				(string Name, DbDataType Type) = prop;
				builder.AppendLine($"		{Name} = reader.Get{(Type == DbDataType.Single ? "Float" : Type.ToString())}(index++),");
			}

			builder.AppendLine($$"""
				};
				public static {{name}} GetSingleModel(DbDataReader reader)
				{
					int a = 0;
					return GetSingleModel(reader, ref a);
				}
			""");
			if (model.GenerateToString)
			{
				builder.AppendLine($$""""
						public override string ToString() =>
							$"""
					"""");
				foreach ((string Name, DbDataType _) prop in properties)
					builder.AppendLine($"		{prop.Name}: {{{prop.Name}}}");
				builder.AppendLine("	\"\"\";");
			}
			builder.AppendLine("}");

			spc.AddSource($"{@namespace}.{name}.g.cs", builder.ToString());
		});

		IncrementalValuesProvider<NestableMetadataModel> nestableProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"OrmGenerator.NestableOrmModelAttribute",
				predicate: static (node, _) => true,
				transform: static (context, token) =>
				{
					INamedTypeSymbol @class = (INamedTypeSymbol)context.TargetSymbol;
					bool generateToString = GetToStringInfo(context);
					ImmutableArray<(string, DbDataType, string?)> prop = @class
						.GetMembers()
						.OfType<IPropertySymbol>()
						.Where(static p => p.DeclaredAccessibility != Accessibility.Private && p.SetMethod is not null)
						.Select(static p =>
						{
							(string, DbDataType, string?) result;
							ImmutableArray<AttributeData> attributeData = p.Type.GetAttributes();
							result = Enum.TryParse(p.Type.Name, true, out DbDataType type)
									? (p.Name, type, null)
									: (p.Name, DbDataType.Unknown, p.Type.Name);
							return result;
						})
						.ToImmutableArray();
					return new NestableMetadataModel(@class.Name, @class.ContainingNamespace.Name, prop, generateToString);
				}
			);
		context.RegisterSourceOutput(nestableProvider, static (spc, model) =>
		{
			StringBuilder builder = new();
			(string name,
			string @namespace,
			bool generateToString,
			ImmutableArray<(string Name, DbDataType Type, string? CustomType)> properties) = model;

			builder.AppendLine(
			$$"""
			using System.Data.Common;
			using OrmGenerator;

			namespace {{@namespace}};
			partial class {{name}} : IOrmModel<{{name}}>
			{
				public static {{name}} GetSingleModel(DbDataReader reader, ref int index) => new()
				{
			""");

			foreach ((string Name, DbDataType Type, string? CustomType) prop in properties)
			{
				(string Name, DbDataType Type, string? CustomType) = prop;
				if (Type == DbDataType.Unknown)
					builder.AppendLine($"		{Name} = {CustomType}.GetSingleModel(reader, ref index),");
				else
					builder.AppendLine($"		{Name} = reader.Get{(Type == DbDataType.Single ? "Float" : Type.ToString())}(index++),");
			}

			builder.AppendLine($$"""
				};
				public static {{name}} GetSingleModel(DbDataReader reader)
				{
					int a = 0;
					return GetSingleModel(reader, ref a);
				}			
			""");
			if (model.GenerateToString)
			{
				builder.AppendLine($$""""
						public override string ToString() =>
							$"""
					"""");
				foreach ((string Name, DbDataType _, string? __) prop in properties)
					builder.AppendLine($"		{prop.Name}: {{{prop.Name}}}");
				builder.AppendLine("	\"\"\";");
			}
			builder.AppendLine("}");

			spc.AddSource($"{@namespace}.{name}.g.cs", builder.ToString());
		});
	}

	private static bool GetToStringInfo(GeneratorAttributeSyntaxContext context) => (bool)(context
		.Attributes[0]
		.NamedArguments
		.FirstOrDefault()
		.Value
		.Value ?? false);
	
}
