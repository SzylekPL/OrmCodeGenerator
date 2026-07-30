using Microsoft.CodeAnalysis;
using System;

namespace OrmGenerator.Utility;

internal interface IGeneratorModel<T> : IEquatable<T>
{
	internal void RegisterModelOutput(SourceProductionContext context);
}