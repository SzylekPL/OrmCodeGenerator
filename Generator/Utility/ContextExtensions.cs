using Microsoft.CodeAnalysis;
using System.Collections.Generic;
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
	extension(INamespaceOrTypeSymbol symbol)
	{
		public IEnumerable<INamespaceSymbol> AllAncestors
		{
			get
			{
				INamespaceSymbol? current = symbol.ContainingNamespace;

				if(current is null || current.IsGlobalNamespace)
					yield break;

				while (current is not null && !current.IsGlobalNamespace)
				{
					yield return current;
					current = current.ContainingNamespace;
				}
			}
		}
	}
}
