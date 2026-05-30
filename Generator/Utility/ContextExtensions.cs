using Microsoft.CodeAnalysis;
using System.Linq;

namespace OrmGenerator.Utility;

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
}
