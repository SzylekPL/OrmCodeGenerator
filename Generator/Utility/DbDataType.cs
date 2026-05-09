namespace OrmGenerator.Utility;

public enum DbDataType
{
	Boolean,
	Byte,
	Char,
	DateTime,
	Decimal,
	Double,
	Single,
	Guid,
	Int16,
	Int32,
	Int64,
	String,
	TimeSpan,
}
public static class DbDataTypeExtensions
{
	extension(DbDataType type)
	{
		public string CsString => type switch
		{
			DbDataType.Single => "Float",
			_ => type.ToString()
		};
	}
}