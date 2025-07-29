using OrmCodeGenerator;
namespace Tests;

[OrmModel]
public partial class DbModel
{
	public int Id { get; set; }
	public string Row1 { get; set; }
	public int Row2 { get; set; }
}
