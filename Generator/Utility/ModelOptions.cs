using System;

namespace OrmGenerator.Utility;

[Flags]
internal enum ModelOptions
{
	None = 0,
	/// <summary>
	/// Using this flag will make the generator use the type's primary constructor for mapping instead of all suitable properties.
	/// </summary>
	/// <remarks>
	/// Record types will always behave as if this flag was enabled.
	/// </remarks>
	UsePrimaryConstructor = 1 << 0,
	/// <summary>
	/// Determines whether the generator will emit a simple <c>ToString()</c> override for the model or not.
	/// </summary>
	GenerateToString = 1 << 1,
}