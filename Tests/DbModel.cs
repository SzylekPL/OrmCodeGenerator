using OrmGenerator;
namespace Tests;

[OrmModel(GenerateToString = true, DisableNesting = false)]
public partial class DbModel
{
	public int Id { get; set; }
	public string Row1 { get; set; }
	public Point Point { get; set; }
	public string Row4 { get; set; }

	//public override string ToString() => 
	//	$"""
	//	Id: {Id}
	//	Row1: {Row1}
	//	Point.X: {Point.X}
	//	Point.Y: {Point.Y}
	//	Row4: {Row4}
	//	""";
}

[OrmModel]
public partial class Point
{
	public int X { get; set; }
	public int Y { get; set; }
}

//[OrmModel]
//sealed partial record RecordModel(string Name, int Age);