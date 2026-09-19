using DbSourceMapper;
using Microsoft.Data.Sqlite;
using MySqlConnector;

//using MySqlConnector;
using Tests;

using SqliteConnection sqliteConnection = new("Data Source=test.db");
sqliteConnection.Open();
using SqliteCommand sqliteCommand = new("SELECT * FROM TestTable;", sqliteConnection);

using MySqlConnection mySqlConnection = new("some connection string");
mySqlConnection.Open();
using MySqlCommand mySqlCommand = new("SELECT * FROM TestTable;", mySqlConnection);

List<DbModel> models = await mySqlCommand.GetListOfAsync<DbModel, MySqlCommand>();
foreach (DbModel model in models)
{
	Console.WriteLine(model);
}

models = await sqliteCommand.GetListOfAsync<DbModel, SqliteCommand>();
foreach (DbModel model in models)
{
	Console.WriteLine(model);
}