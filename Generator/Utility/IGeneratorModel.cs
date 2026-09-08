using Microsoft.CodeAnalysis;
using System;

namespace DbSourceMapper.Utility;

internal interface IGeneratorModel<T>
{
	internal void RegisterModelOutput(SourceProductionContext context);
}