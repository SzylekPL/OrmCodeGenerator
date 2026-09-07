using Microsoft.CodeAnalysis;
using System.Linq;

namespace DbSourceMapper.Utility;

internal static class ContextExtensions
{
	extension<T>(in GeneratorAttributeSyntaxContext context)
	{
		public T? GetAttributeConstructorArgument(int index) => (T?)context
		.Attributes[0]
		.ConstructorArguments[index]
		.Value;

		public T? GetAttributeNamedArgument(string name) => (T?)context
		.Attributes[0]
		.NamedArguments
		.FirstOrDefault(p => p.Key == name)
		.Value
		.Value;
	}
	extension<T>(IncrementalGeneratorInitializationContext context) where T : IGeneratorModel<T>
	{
		public void RegisterModelSourceOutput(IncrementalValuesProvider<T> provider)
		{
			context.RegisterSourceOutput<T>(provider, static (ctx, model) =>
			{
				model.RegisterModelOutput(ctx);
			});
		}
	}
}
