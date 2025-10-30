using Microsoft.CodeAnalysis;
using System;

namespace OrmGenerator.Utility;
public static class GeneratorExtensions
{
	public static void RegisterMarkerAttributes(this IncrementalGeneratorInitializationContext context, params (string Namespace, string Name, AttributeTargets Targets)[] attributesData)
	{
		context.RegisterPostInitializationOutput(ctx =>
		{
			foreach ((string Namespace, string Name, AttributeTargets Targets) attribute in attributesData)
			{
				(string Namespace, string Name, AttributeTargets Targets) = attribute;

				ctx.AddSource($"{Name}.g.cs",
					$$"""
					namespace {{Namespace}};
					[AttributeUsage((AttributeTargets){{(int)Targets}})]
					public sealed class {{Name}} : Attribute;
					""");
			}
		});
	}
}
