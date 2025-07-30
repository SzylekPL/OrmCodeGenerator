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
					public static abstract List<TSelf> GetListFromQuery(DbCommand command);
					public static abstract Task<List<TSelf>> GetListFromQueryAsync(DbCommand command, CancellationToken token = default);
				}
				""");
			ctx.AddSource("DbCommandExtensions.g.cs",
				"""
				using System.Data.Common;
				
				public static class DbCommandExtensions
				{
					public static List<T> GetListOf<T>(this DbCommand command) where T: OrmGenerator.IOrmModel<T> => T.GetListFromQuery(command);
					public static async Task<List<T>> GetListOfAsync<T>(this DbCommand command, CancellationToken token = default) where T: OrmGenerator.IOrmModel<T> => await T.GetListFromQueryAsync(command, token);
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
		//context.RegisterSourceOutput(provider, (spc, model) => spc.AddSource("debug", model.Properties.Aggregate(new StringBuilder(), (builder, data) => builder.AppendLine($"// {data.Name}\t{data.Type}")).ToString()));
		context.RegisterSourceOutput(provider, (spc, model) =>
		{
			StringBuilder builder = new();
			builder.AppendLine(
			$$"""
			using System.Data.Common;

			namespace {{model.Namespace}};
			partial class {{model.Name}} : OrmGenerator.IOrmModel<{{model.Name}}>
			{
				public static List<{{model.Name}}> GetListFromQuery(DbCommand command)
				{
					List<{{model.Name}}> result = [];
					using DbDataReader reader = command.ExecuteReader();

					while(reader.Read())
						result.Add(new()
						{
			""");

			GenerateTypeCasts(model, builder);

			builder.AppendLine(
			$$"""
						});
					return result;
				}
				public static async Task<List<{{model.Name}}>> GetListFromQueryAsync(DbCommand command, CancellationToken token = default)
				{
					List<{{model.Name}}> result = [];
					using DbDataReader reader = await command.ExecuteReaderAsync(token);

					while(await reader.ReadAsync(token))
						result.Add(new()
						{
			""");
			GenerateTypeCasts(model, builder);

			builder.AppendLine(
			$$"""
						});
					return result;
				}
				public static IEnumerable<{{model.Name}}> GetFromQuery(DbCommand command)
				{
					using DbDataReader reader = command.ExecuteReader();

					while(reader.Read())
						yield return new()
						{
			""");

			GenerateTypeCasts(model, builder);

			builder.AppendLine(
			$$"""
						};
				}
				public static async IAsyncEnumerable<{{model.Name}}> GetFromQueryAsync(DbCommand command, CancellationToken token = default)
				{
					using DbDataReader reader = await command.ExecuteReaderAsync(token);

					while(await reader.ReadAsync(token))
						yield return new()
						{
			""");
			GenerateTypeCasts(model, builder);

			builder.AppendLine(
			"""
						};
				}
			}
			""");

			spc.AddSource($"{model.Namespace}.{model.Name}.g.cs", builder.ToString());
		});
	}

	private static StringBuilder GenerateTypeCasts(MetadataModel model, StringBuilder builder)
	{
		for (int i = 0; i < model.Properties.Length; i++)
		{
			(string Name, DataType Type) = model.Properties[i];
			builder.AppendLine($"				{Name} = reader.Get{(Type == DataType.Single ? "Float" : Type.ToString())}({i}),");
		}
		return builder;
	}
}
