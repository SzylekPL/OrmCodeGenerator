using Microsoft.Data.Sqlite;
using Tests;

using SqliteConnection connection = new("Data Source=test.db");
connection.Open();
using SqliteCommand command = new("SELECT * FROM TestTable;", connection);

List<DbModel> models = await command.GetListOfAsync<DbModel>();


foreach (DbModel model in models)
	Console.WriteLine($"{model.Id}\t{model.Row1}\t{model.Row2}");