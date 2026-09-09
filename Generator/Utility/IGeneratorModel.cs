using Microsoft.CodeAnalysis;
using System;

namespace DbSourceMapper.Utility;

internal interface IGeneratorModel<T> : IEquatable<T>
{
	internal void RegisterModelOutput(SourceProductionContext context);
}