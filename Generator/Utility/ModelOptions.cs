using System;

namespace OrmGenerator.Utility;

[Flags]
internal enum ModelOptions
{
	None = 0,
	/// <summary>
	/// Using this flag will enforce all properties to be native database types.
	/// This may result in a slight improvement in the generator's performance.
	/// </summary>
	/// <remarks>
	/// If the flag is  not present, the optimization will still take place provided that
	/// type requirements are met.
	/// </remarks>
	DisableNesting = 1 << 0,
	/// <summary>
	/// Using this flag will make the generator use the type's primary constructor for mapping instead of all suitable properties.
	/// </summary>
	/// <remarks>
	/// Record types will always behave as if this flag was enabled.
	/// </remarks>
	UsePrimaryConstructor = 1 << 1,
	/// <summary>
	/// Determines whether the generator will emit a simple <c>ToString()</c> override for the model or not.
	/// </summary>
	GenerateToString = 1 << 2,
}