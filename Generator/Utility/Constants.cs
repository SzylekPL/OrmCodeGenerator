using System;
using System.Collections.Frozen;

namespace OrmGenerator.Utility;

internal static class Constants
{
	internal static readonly FrozenSet<string> DbDataTypes = Enum.GetNames(typeof(DbDataType)).ToFrozenSet();
}