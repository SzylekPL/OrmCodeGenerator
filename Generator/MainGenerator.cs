using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace OrmGenerator;

[Generator]
public class MainGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterMarkerAttributes(
			("OrmCodeGenerator", "OrmModelAttribute", AttributeTargets.Class),
			("OrmCodeGenerator", "NestableOrmModelAttribute", AttributeTargets.Class)
		);
		context.RegisterPostInitializationOutput(static ctx =>
		{
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
				using OrmGenerator;
				
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
				"OrmCodeGenerator.OrmModelAttribute",
				predicate: static (node, _) => true,
				transform: static (context, token) =>
				{
					INamedTypeSymbol @class = (INamedTypeSymbol)context.TargetSymbol;
					ImmutableArray<(string, DbDataType)> prop = @class
						.GetMembers()
						.Where(static s => s is IPropertySymbol)
						.Cast<IPropertySymbol>()
						.Select(static p => Enum.TryParse(p.Type.Name, true, out DbDataType dataType)
								? (p.Name, dataType)
								: (p.Name, DbDataType.Unknown))
						.ToImmutableArray();
					return new MetadataModel(@class.Name, @class.ContainingNamespace.Name, prop);
				}
			);
		context.RegisterSourceOutput(provider, static (spc, model) =>
		{
			StringBuilder builder = new();

			builder.AppendLine(
			$$"""
			using System.Data.Common;
			using OrmGenerator;

			namespace {{model.Namespace}};
			partial class {{model.Name}} : IOrmModel<{{model.Name}}>
			{
				public static {{model.Name}} GetSingleModel(DbDataReader reader, ref int index) => new()
				{
			""");

			foreach ((string Name, DbDataType Type) prop in model.Properties)
			{
				(string Name, DbDataType Type) = prop;
				builder.AppendLine($"		{Name} = reader.Get{(Type == DbDataType.Single ? "Float" : Type.ToString())}(index++),");
			}

			builder.AppendLine($$"""
				};
				public static {{model.Name}} GetSingleModel(DbDataReader reader)
				{
					int a = 0;
					return GetSingleModel(reader, ref a);
				}
			}
			""");

			spc.AddSource($"{model.Namespace}.{model.Name}.g.cs", builder.ToString());
		});

		IncrementalValuesProvider<NestableMetadataModel> nestableProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"OrmCodeGenerator.NestableOrmModelAttribute",
				predicate: static (node, _) => true,
				transform: static (context, token) =>
				{
					INamedTypeSymbol @class = (INamedTypeSymbol)context.TargetSymbol;
					ImmutableArray<(string, DbDataType, string?)> prop = @class
						.GetMembers()
						.OfType<IPropertySymbol>()
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
					return new NestableMetadataModel(@class.Name, @class.ContainingNamespace.Name, prop);
				}
			);
		context.RegisterSourceOutput(nestableProvider, static (spc, model) =>
		{
			StringBuilder builder = new();

			builder.AppendLine(
			$$"""
			using System.Data.Common;
			using OrmGenerator;

			namespace {{model.Namespace}};
			partial class {{model.Name}} : IOrmModel<{{model.Name}}>
			{
				public static {{model.Name}} GetSingleModel(DbDataReader reader, ref int index) => new()
				{
			""");

			foreach ((string Name, DbDataType Type, string? CustomType) prop in model.Properties)
			{
				(string Name, DbDataType Type, string? CustomType) = prop;
				if (Type == DbDataType.Unknown)
					builder.AppendLine($"		{Name} = {CustomType}.GetSingleModel(reader, ref index),");
				else
					builder.AppendLine($"		{Name} = reader.Get{(Type == DbDataType.Single ? "Float" : Type.ToString())}(index++),");
			}

			builder.AppendLine($$"""
				};
				public static {{model.Name}} GetSingleModel(DbDataReader reader)
				{
					int a = 0;
					return GetSingleModel(reader, ref a);
				}			
			}
			""");

			spc.AddSource($"{model.Namespace}.{model.Name}.g.cs", builder.ToString());
		});
	}
}
