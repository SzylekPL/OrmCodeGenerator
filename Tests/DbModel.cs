using OrmGenerator;
namespace Tests;

[OrmModel(ModelOptions.GenerateToString)]
public partial class DbModel
{
	public int Id { get; set; }
	public string Row1 { get; set; }
	public Point Point { get; set; }
	public string Row4 { get; set; }
}

[OrmModel(ModelOptions.GenerateToString)]
public partial record Point(int X, int Y);

[OrmModel(ModelOptions.UsePrimaryConstructor)]
public partial class Coords(int x, int y)
{
	public int X => x;
	public int Y => y;
}