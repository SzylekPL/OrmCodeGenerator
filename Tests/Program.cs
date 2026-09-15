using DbSourceMapper;
//using Microsoft.Data.Sqlite;
using MySqlConnector;
using Tests;

//using SqliteConnection connection = new("Data Source=test.db");
//connection.Open();
//using SqliteCommand command = new("SELECT * FROM TestTable;", connection);

using MySqlConnection connection = new("Data Source=test.db");
connection.Open();
using MySqlCommand command = new("SELECT * FROM TestTable;", connection);

List<DbModel> models = await command.GetListOfAsync<DbModel, MySqlCommand>();

foreach (DbModel model in models)
{
	Console.WriteLine(model);
}