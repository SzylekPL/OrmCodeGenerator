# OrmCodeGenerator

## Introduction
OrmCodeGenerator is a simple, zero-reflection micro-ORM based on ADO.NET and source generators.

## How it works
The library provides an easy, convinient and efficient way of mapping database query results into objects.

Firstly, DB models need to be partial and marked with `[OrmModel]` or `[NestableOrmModel]`:
```cs
[NestableOrmModel]
public partial class DbModel
{
	public int Id { get; set; }
	public string FirstName { get; set; }
	public Point Point { get; set; }
	public string LastName { get; set; }
}

[OrmModel]
public partial class Point
{
	public int X { get; set; }
	public int Y { get; set; }
}
```
*Tip: Theoretically, using `[NestableOrmModel]` everywhere will work just fine, however using `[OrmModel]` where it's appropriate should improve the generator's performance.*

After bulding the project and running the generator, the model can be used like so:
```cs

using SqliteConnection connection = new("Data Source=test.db");
connection.Open();
using SqliteCommand command = new("SELECT * FROM TestTable;", connection);

DbModel? dbModel = await command.GetSingleAsync<DbModel>();

Console.WriteLine(dbModel);
List<DbModel> models = await command.GetListOfAsync<DbModel>();


foreach (DbModel model in models)
{
	Console.WriteLine(model);
}
```
*Note: SQLite has been used as an example, other database providers for ADO.NET should also work properly.*

Currently, the library provides 6 `DbCommand` extension methods for mapping data:
```cs
	public static T? GetSingle<T>(this DbCommand command) where T: IOrmModel<T>?
	public static async Task<T?> GetSingleAsync<T>(this DbCommand command, CancellationToken token = default) where T: IOrmModel<T>?
	public static List<T> GetListOf<T>(this DbCommand command) where T: IOrmModel<T>
	public static async Task<List<T>> GetListOfAsync<T>(this DbCommand command, CancellationToken token = default) where T: IOrmModel<T>
	public static IEnumerable<T> GetEnumerableOf<T>(this DbCommand command) where T: IOrmModel<T>
	public static async IAsyncEnumerable<T> GetAsyncEnumerableOf<T>(this DbCommand command, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default) where T: IOrmModel<T>
```
## Advantages
- Works during compilation, no runtime penalties.
- NativeAOT-friendly.
- Supports nested models (when marked properly).
## Limitations
- Value reading is based on property order in the model class, not possible to change in current version.