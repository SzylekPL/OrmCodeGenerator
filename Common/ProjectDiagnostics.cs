using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shared;

public static class ProjectDiagnostics
{
	public static readonly DiagnosticDescriptor _notMarkedRule = new(
		id: "ORM001",
		title: "Model not marked",
		messageFormat: "The type {0} of property {1} must be a marked model",
		category: "Syntax",
		description: "The property is neither a standard database type, nor a model marked with OrmModelAttribute.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	public static readonly DiagnosticDescriptor _notNestableRule = new(
		id: "ORM002",
		title: "Disabled nesting violation",
		messageFormat: "The model of type {0} has disabled nesting and mustn't contain other models",
		category: "Syntax",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	public static readonly DiagnosticDescriptor _ctorNotSuitableRule = new(
		id: "ORM003",
		title: "Primary constructor not suitable",
		messageFormat: "The model of type {0} doesn't have a suitable primary constructor",
		category: "Syntax",
		description: "The model is configured to use primary constructor for mapping, but it doesn't exist or is parameterless.",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	public static readonly ImmutableArray<DiagnosticDescriptor> DefinedDiagnostics = [_notMarkedRule, _ctorNotSuitableRule, _notNestableRule];
}
