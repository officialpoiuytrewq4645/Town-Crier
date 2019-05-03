using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Database
{
	public class User
	{ 
		[ExplicitKey]
		public ulong DiscordID { get; set; }

		public string Username { get; set; }

		public uint Coins { get; set; }

		public bool IsAdmin { get; set; }

		public DateTime JoinDate { get; set; }

		public DateTime LastMessage { get; set; }

		public uint Score { get; set; }

		public uint UsedHourPoints { get; set; }

		public DateTime UsedFirstHourPoint { get; set; }

		IDbConnection connection;

		public void SetConnection(IDbConnection connection)
		{
			this.connection = connection;
		}

		public async Task Save()
		{
			await connection.UpdateAsync(this);
		}
	}

	//public class DatabaseTable<T>
	//	where T : class
	//{
	//	public string Name { get; }
	//	public string IdName { get; }

	//	SQLiteConnection connection;

	//	SQLiteCommand insert;

	//	public DatabaseTable(SQLiteConnection connection, string name, string idName, params string[] fields)
	//	{
	//		this.connection = connection;

	//		Name = name;
	//		IdName = idName;

	//		StringBuilder builder = new StringBuilder();
	//		builder.Append("INSERT INTO ");
	//		builder.Append(name);
	//		builder.Append(" (");

	//		for (int i = 0; i < fields.Length; i++)
	//		{
	//			builder.Append(fields[i]);

	//			if (i + 1 < fields.Length)
	//			{
	//				builder.Append(", ");
	//			}
	//		}

	//		builder.Append(") VALUES (");

	//		for (int i = 0; i < fields.Length; i++)
	//		{
	//			builder.Append("?");

	//			if (i + 1 < fields.Length)
	//			{
	//				builder.Append(", ");
	//			}
	//		}

	//		insert = new SQLiteCommand(builder.ToString(), connection);
			
	//	}

	//	//public async Task<T> Get(int id)
	//	//{
	//	//	return await connection.<T>(string.Format("SELECT * FROM {0} WHERE {1} = {2}", Name, IdName, id));
	//	//}

	//	//public async Task<IEnumerable<T>> GetAll(string condition)
	//	//{
	//	//	return await connection.QueryAsync<T>(string.Format("SELECT * FROM {0} WHERE {1}", Name, condition));
	//	//}

	//	public async Task Insert(T value)
	//	{
	//		Dapper.Contrib.Extensions.SqlMapperExtensions.Insert(connection, value);

	//		connection.Insert(value);
	//	}
	//		//	connection.
	//		//	insert.Parameters.Clear();

	//		//	insert.Parameters.Add(book.Id);
	//		//	insert.Parameters.Add(book.Title);
	//		//	insert.Parameters.Add(book.Language);
	//		//	insert.Parameters.Add(book.PublicationDate);
	//		//	insert.Parameters.Add(book.Publisher);
	//		//	insert.Parameters.Add(book.Edition);
	//		//	insert.Parameters.Add(book.OfficialUrl);
	//		//	insert.Parameters.Add(book.Description);
	//		//	insert.Parameters.Add(book.EBookFormat);

	//		//	try
	//		//	{
	//		//		insertSQL.ExecuteNonQuery();
	//		//	}
	//		//	catch (Exception ex)
	//		//	{
	//		//		throw new Exception(ex.Message);
	//		//	}
	//		//}
	//	}


	public class DatabaseAccess : IDisposable
	{
		const string DatabaseFile = "../../database.sqlite";
		
		SQLiteConnection sqlConnection;

		public DatabaseAccess()
		{
			if (!File.Exists(DatabaseFile))
			{
				SQLiteConnection.CreateFile(DatabaseFile);
			}
		}

		public async Task<IDbConnection> Connect()
		{
			if (sqlConnection != null)
			{
				return sqlConnection;
			}

			sqlConnection = new SQLiteConnection("Data Source=" + DatabaseFile);
			await sqlConnection.OpenAsync();
						
			await Execute(@"CREATE TABLE IF NOT EXISTS Users (
								DiscordID INTEGER PRIMARY KEY, 
								Username STRING, 
								JoinDate STRING, 
								IsAdmin INTEGER, 
								Coins INTEGER, 
								Score INTEGER, 
								LastMessage STRING,
								UsedHourPoints INTEGER, 
								UsedFirstHourPoint STRING)");

			//await Execute("CREATE TABLE IF NOT EXISTS Players (DiscordID INTEGER PRIMARY KEY, TeamID INTEGER, IsLeftHandUsed INTEGER, IsRightHandUsed INTEGER");
			//await Execute("CREATE TABLE IF NOT EXISTS Items (ItemID INTEGER PRIMARY KEY, Name STRING, Type INTEGER, Description STRING, Durability INTEGER");
			//await Execute("CREATE TABLE IF NOT EXISTS Locations (LocationID INTEGER PRIMARY KEY, Name STRING");
			//await Execute("CREATE TABLE IF NOT EXISTS recipe");
			//await Execute("CREATE TABLE IF NOT EXISTS party");

			//await Execute("CREATE TABLE IF NOT EXISTS user_item (UserItemID INTEGER PRIMARY KEY, DiscordID INTEGER, ItemID INTEGER)");
			//await Execute("CREATE TABLE IF NOT EXISTS user_location (UserItemID INTEGER PRIMARY KEY, DiscordID INTEGER, LocationId INTEGER)");
			//await Execute("CREATE TABLE IF NOT EXISTS user_recipe");
			//await Execute("CREATE TABLE IF NOT EXISTS user_party");

			return sqlConnection;
		}

		//public DatabaseTable<T> GetTable<T>(string name, string idName)
		//{
		//	return new DatabaseTable<T>(sqlConnection, name, idName);
		//}

		public async Task Execute(string command)
		{
			await Connect();

			SQLiteCommand sqlCommand = new SQLiteCommand(command, sqlConnection);

			int result = await sqlCommand.ExecuteNonQueryAsync();
		}

		//public async Task<T> Scalar<T>(string command)
		//{
		//	SQLiteCommand sqlCommand = new SQLiteCommand(command, sqlConnection);

		//	return (T)await sqlCommand.ExecuteScalarAsync();
		//}

		//public async Task<DbDataReader> Read(string command)
		//{
		//	SQLiteCommand sqlCommand = new SQLiteCommand(command, sqlConnection);

		//	return await sqlCommand.ExecuteReaderAsync();
		//}

		public void Dispose()
		{
			((IDisposable)sqlConnection).Dispose();
		}
	}
}
