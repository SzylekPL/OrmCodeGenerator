using System.Collections.Frozen;
using System.Linq;

namespace Shared;

internal static class Constants
{
	//public static FrozenDictionary<string, string> DbDataTypes => new Dictionary<string, string>
	//{
	//	{"bool","Boolean" },
	//	{"byte","Byte"},
	//	{"char","Char"},
	//	{"DateTime","DateTime"},
	//	{"decimal","Decimal"},
	//	{"double","Double"},
	//	{"float","Single"},
	//	{"Guid","Guid"},
	//	{"short","Int16"},
	//	{"int","Int32"},
	//	{"long","Int64"},
	//	{"string","String"},
	//	{"TimeSpan","TimeSpan"},
	//}.ToFrozenDictionary();
	internal static FrozenSet<string> DbDataTypes =>
	[
		"Boolean",
		"Byte",
		"Char",
		"DateTime",
		"Decimal",
		"Double",
		"Single",
		"Guid",
		"Int16",
		"Int32",
		"Int64",
		"String",
		"TimeSpan",
	];
	internal static readonly int InternThreshold = DbDataTypes.Max(static s => s.Length);
}