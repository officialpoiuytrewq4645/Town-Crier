using Alta.WebApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Discord;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier.Database;

namespace TownCrier.Services
{
	public interface ITableAccess<T>
	{
		string Name { get; }
		int Count();
		//int Count(Expression<Func<T, bool>> predicate);
		//int Count(Query query);
		//int Delete(Query query);
		//int Delete(Expression<Func<T, bool>> predicate);
		//bool Delete(BsonValue id);
		//bool DropIndex(string field);
		//bool EnsureIndex<K>(Expression<Func<T, K>> property, string expression, bool unique = false);
		//bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false);
		//bool EnsureIndex(string field, string expression, bool unique = false);
		//bool EnsureIndex(string field, bool unique = false);
		//bool Exists(Query query);
		//bool Exists(Expression<Func<T, bool>> predicate);
		//IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
		//IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue);
		IEnumerable<T> FindAll();
		T FindOne();
		T FindById(ulong id);
		T FindByIndex(object id, string index, string fieldName);
		IEnumerable<T> FindAllByIndex(object id, string index, string fieldName);
		//T FindOne(Query query);
		//T FindOne(Expression<Func<T, bool>> predicate);
		//IEnumerable<IndexInfo> GetIndexes();
		//LiteCollection<T> Include(string path);
		//LiteCollection<T> Include(string[] paths);
		//LiteCollection<T> Include<K>(Expression<Func<T, K>> path);
		//LiteCollection<T> IncludeAll(int maxDepth = -1);
		//void Insert(BsonValue id, T document);
		//int Insert(IEnumerable<T> docs);
		void Insert(T document);
		//int InsertBulk(IEnumerable<T> docs, int batchSize = 5000);
		//long LongCount(Query query);
		//long LongCount(Expression<Func<T, bool>> predicate);
		//long LongCount();
		//BsonValue Max(string field);
		//BsonValue Max();
		//BsonValue Max<K>(Expression<Func<T, K>> property);
		//BsonValue Min();
		//BsonValue Min<K>(Expression<Func<T, K>> property);
		//BsonValue Min(string field);
		bool Update(T document);
		//bool Update(BsonValue id, T document);
		//int Update(IEnumerable<T> documents);
		//bool Upsert(BsonValue id, T document);
		//bool Upsert(T document);
		//int Upsert(IEnumerable<T> documents);
	}

	public class LiteDBTableAccess<T> : ITableAccess<T>
	{
		LiteDatabase Database { get; }

		ILiteCollection<T> Table { get; }

		public string Name { get; }

		public LiteDBTableAccess(LiteDatabase database, string name)
		{
			Name = name;

			Database = database;

			Table = database.GetCollection<T>(name);
		}

		public int Count()
		{
			return Table.Count();
		}

		public T FindOne()
		{
			return Table.FindOne(item => true);
		}

		public T FindById(ulong id)
		{
			return Table.FindById(new BsonValue((decimal)id));
		}
		
		public T FindByIndex(object value, string index, string fieldName)
		{
			return Table.FindOne(Query.EQ(fieldName, new BsonValue((int)value)));
		}

		public IEnumerable<T> FindAllByIndex(object value, string index, string fieldName)
		{
			return Table.Find(Query.EQ(fieldName, new BsonValue((int)value)));
		}

		public void Insert(T document)
		{
			Table.Insert(document);
		}

		public bool Update(T document)
		{
			return Table.Update(document);
		}

		public IEnumerable<T> FindAll()
		{
			List<BsonDocument> failed = new List<BsonDocument>();

			foreach (BsonDocument bson in Database.GetCollection(Name).FindAll())
			{
				T item = default(T);

				try
				{
					 item = BsonMapper.Global.ToObject<T>(bson);
				}
				catch (Exception e)
				{
					failed.Add(bson);
					continue;
				}
			
				yield return item;
			}

			Console.WriteLine(failed.Count);
		}
	}

	public class DynamoTableAccess<T> : ITableAccess<T>
		where T : class
	{
		public DynamoDBContext Context { get; }

		public Table Table { get; }

		public string Name => throw new NotImplementedException();

		public DynamoTableAccess(DynamoDBContext context)
		{
			Context = context;

			Table = Context.GetTargetTable<T>();
		}

		public int Count()
		{
			return Table.Scan(new ScanFilter()).Count;
		}

		public T FindOne()
		{
			Document result = Table.Scan(new ScanOperationConfig() { Limit = 1 }).GetRemainingAsync().Result.FirstOrDefault();

			if (result == null)
			{
				return null;
			}

			return Context.FromDocument<T>(result);
		}

		public T FindById(ulong id)
		{
			return Context.LoadAsync<T>(id).Result;
		}

		public T FindByIndex(object value, string index, string fieldName)
		{
			return Context.LoadAsync<T>(value, new DynamoDBOperationConfig() { IndexName = index }).Result;
		}

		public IEnumerable<T> FindAllByIndex(object value, QueryOperator queryOperator, string index)
		{
			return Context.QueryAsync<T>(value, new DynamoDBOperationConfig() { IndexName = index }).GetRemainingAsync().Result;
		}

		public IEnumerable<T> FindAllByIndex(object value, string index, string fieldName)
		{
			return Context.QueryAsync<T>(value, new DynamoDBOperationConfig() { IndexName = index }).GetRemainingAsync().Result;
		}

		public void Insert(T document)
		{
			Context.SaveAsync<T>(document).Wait();
		}

		public bool Update(T document)
		{
			Context.SaveAsync<T>(document).Wait();

			return true;
		}

		public IEnumerable<T> FindAll()
		{
			throw new NotImplementedException();
		}
	}


	public class TownDatabase
	{
		LiteDatabase database;

		DynamoDBContext dynamo;

		public ITableAccess<TownGuild> Guilds { get; }
		public ITableAccess<TownUser> Users { get; }

		public TownDatabase(IAmazonDynamoDB dbContext, LiteDatabase database)
		{
			this.database = database;
			
			Guilds = new LiteDBTableAccess<TownGuild>(database, "Guilds");
			Users = new LiteDBTableAccess<TownUser>(database, "Users");
				
			Console.WriteLine("Migrating to dynamo!");
			Migrate(dbContext);

			Guilds = new DynamoTableAccess<TownGuild>(dynamo);
			Users = new DynamoTableAccess<TownUser>(dynamo);
		}

		public TownDatabase(IAmazonDynamoDB dbContext)
		{
			Console.WriteLine("Running DDB!");

			dynamo = new DynamoDBContext(dbContext);

			Guilds = new DynamoTableAccess<TownGuild>(dynamo);
			Users = new DynamoTableAccess<TownUser>(dynamo);
		}

		public TownDatabase(LiteDatabase database)
		{
			Console.WriteLine("Running LiteDB!");
			this.database = database;

			Guilds = new LiteDBTableAccess<TownGuild>(database, "Guilds");
			Users = new LiteDBTableAccess<TownUser>(database, "Users");
		}

		void Migrate(IAmazonDynamoDB dbContext)
		{
			dynamo = new DynamoDBContext(dbContext);

			var newGuilds = new DynamoTableAccess<TownGuild>(dynamo);
			var newUsers = new DynamoTableAccess<TownUser>(dynamo);

			Migrate(Guilds, newGuilds);
			Migrate(Users, newUsers, FixUser);
		}

		void FixUser(TownUser user)
		{
			if (user.AltaInfo != null)
			{
				user.AltaId = user.AltaInfo.Identifier;

				user.SupporterExpiry = user.AltaInfo.SupporterExpiry;

				user.SupporterExpiryDay = user.SupporterExpiry?.Date;
			}
		}

		void Migrate<T>(ITableAccess<T> from, ITableAccess<T> to, Action<T> modify = null)
		{
			T last;
			foreach (T item in from.FindAll())
			{
				last = item;
				modify?.Invoke(item);

				to.Insert(item);
			}
		}

		public TownGuild GetGuild(IGuild guild)
		{
			return Guilds.FindById(guild.Id);
		}

		public TownUser GetUser(IUser user)
		{
			TownUser result = Users.FindById(user.Id);

			bool isChanged = false;

			if (result == null)
			{
				result = new TownUser() { UserId = user.Id, Name = user.Username };

				Users.Insert(result);
			}
			else if (result.Name != user.Username)
			{
				result.Name = user.Username;

				isChanged = true;
			}

			if (result.InitialJoin == default(DateTime) && user is IGuildUser guildUser && guildUser.JoinedAt.HasValue)
			{
				result.InitialJoin = guildUser.JoinedAt.Value.UtcDateTime;

				isChanged = true;
			}

			if (isChanged)
			{
				Users.Update(result);
			}

			return result;
		}
	}
}
