using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shared;

internal static class ProjectDiagnostics
{
	/// <summary>
	/// DSM0001
	/// </summary>
	public static readonly DiagnosticDescriptor _notMarkedRule = new(
		id: "DSM0001",
		title: "Model not marked",
		messageFormat: "The type {0} of property {1} must be a marked model",
		category: "Syntax",
		description: "The property is neither a standard database type, nor a model marked with OrmModelAttribute.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	/// <summary>
	/// DSM0002
	/// </summary>
	public static readonly DiagnosticDescriptor _ctorNotSuitableRule = new(
		id: "DSM0002",
		title: "Primary constructor not suitable",
		messageFormat: "The model of type {0} doesn't have a suitable primary constructor",
		category: "Syntax",
		description: "The model is configured to use primary constructor for mapping, but it doesn't exist or is parameterless.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	/// <summary>
	/// DSM0003
	/// </summary>
	public static readonly DiagnosticDescriptor _structNotAllowedRule = new(
		id: "DSM0003",
		title: "Struct type not supported",
		messageFormat: "The model of type {0} is defined as a struct, which is not supported by the generator",
		category: "Syntax",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	public static readonly ImmutableArray<DiagnosticDescriptor> DefinedDiagnostics = [_notMarkedRule, _ctorNotSuitableRule, _structNotAllowedRule];
}
