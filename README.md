# OrmCodeGenerator

## Introduction
OrmCodeGenerator is a simple, zero-reflection micro-ORM based on ADO.NET and source generators.

## How it works
The library provides an easy, convinient and efficient way of mapping database query results into objects.

Firstly, DB models need to be partial and marked with the `[OrmModel]` attribute:
```cs
[OrmModel(ModelOptions.GenerateToString)]
public partial class DbModel
{
	public int Id { get; set; }
	public string Row1 { get; set; }
	public Point Point { get; set; }
	public string Row4 { get; set; }
}

[OrmModel(ModelOptions.GenerateToString | ModelOptions.DisableNesting)]
public partial record Point(int X, int Y);
```

After bulding the project and running the generator, the model can be used like so:
```cs

using SqliteConnection connection = new("Data Source=test.db");
connection.Open();
using SqliteCommand command = new("SELECT * FROM TestTable;", connection);

DbModel? dbModel = await command.GetFirstOrNullAsync<DbModel>();

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
public T? GetFirstOrNull<T>();
public async Task<T?> GetFirstOrNullAsync<T>(CancellationToken token = default);
public List<T> GetListOf<T>();
public async Task<List<T>> GetListOfAsync<T>(CancellationToken token = default);
public IEnumerable<T> GetEnumerableOf<T>();
public async IAsyncEnumerable<T> GetAsyncEnumerableOf<T>(CancellationToken token = default);
```
## Features
- Works during compilation, no runtime penalties.
- NativeAOT-friendly.
- Supports nested models.
- Supports both property-based and constructor-based mapping.
## Limitations
- Value reading is based on property order in the model class, not possible to change in current version.