using Microsoft.Data.Sqlite;
using Tests;

using SqliteConnection connection = new("Data Source=test.db");
connection.Open();
using SqliteCommand command = new("SELECT * FROM TestTable;", connection);

DbModel? dbModel = await command.GetSingleAsync<DbModel>();

Console.WriteLine(dbModel);
//List<DbModel> models = await command.GetListOfAsync<DbModel>();


//foreach (DbModel model in models)
//{
//	Console.WriteLine(model);
//}
