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
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("OrmModelAttribute.g.cs",
				"""
				namespace OrmCodeGenerator;
				[AttributeUsage(AttributeTargets.Class)]
				public sealed class OrmModelAttribute : Attribute;
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
				using System.Data.Common;
				using OrmGenerator;
				
				public static class DbCommandExtensions
				{
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

					public static IEnumerable<T> GetIteratorOf<T>(this DbCommand command) where T: IOrmModel<T>
					{
						using DbDataReader reader = command.ExecuteReader();
				
						while(reader.Read())
							yield return T.GetSingleModel(reader);
					}

					public static async IAsyncEnumerable<T> GetIteratorOfAsync<T>(this DbCommand command, CancellationToken token = default) where T: IOrmModel<T>
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
					ImmutableArray<(string, DataType)> prop = @class
						.GetMembers()
						.Where(static s => s is IPropertySymbol)
						.Cast<IPropertySymbol>()
						.Select(static p => Enum.TryParse(p.Type.Name, true, out DataType dataType)
								? (p.Name, dataType)
								: throw new NotSupportedException($"Data type of property {p.Name} is {p.Type.Name}, which is not supported."))
						.ToImmutableArray();
					return new MetadataModel(@class.Name, @class.ContainingNamespace.Name, prop);
				}
			);
		context.RegisterSourceOutput(provider, (spc, model) =>
		{
			StringBuilder builder = new();

			builder.AppendLine(
			$$"""
			using System.Data.Common;
			using OrmGenerator;

			namespace {{model.Namespace}};
			partial class {{model.Name}} : IOrmModel<{{model.Name}}>
			{
				public static {{model.Name}} GetSingleModel(DbDataReader reader) => new()
				{
			""");

			for (int i = 0; i < model.Properties.Length; i++)
			{
				(string Name, DataType Type) = model.Properties[i];
				builder.AppendLine($"		{Name} = reader.Get{(Type == DataType.Single ? "Float" : Type.ToString())}({i}),");
			}

			builder.AppendLine($$"""
				};
			}
			""");

			spc.AddSource($"{model.Namespace}.{model.Name}.g.cs", builder.ToString());
		});
	}
}
