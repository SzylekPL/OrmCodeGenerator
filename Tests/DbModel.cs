using DbSourceMapper;
using Microsoft.Data.Sqlite;
using MySqlConnector;

namespace Tests;

[DbSourceModel<SqliteCommand>(ModelOptions.GenerateToString)]
[DbSourceModel<MySqlCommand>]
public partial class DbModel
{
	public int Id { get; set; }
	public int Row1 { get; set; }
	public Point Point { get; set; }
	public DateOnly Row4 { get; set; }
}

[DbSourceModel<SqliteCommand>(ModelOptions.GenerateToString)]
[DbSourceModel<MySqlCommand>]
public partial record Point(int X, int Y);

[DbSourceModel(ModelOptions.UsePrimaryConstructor)]
public partial class Coords(int x, int y)
{
	public int X => x;
	public int Y => y;
}