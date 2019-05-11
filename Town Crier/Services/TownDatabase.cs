using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier.Database;

namespace TownCrier.Services
{
	public class TownDatabase
	{
		LiteDatabase database;

		public LiteCollection<TownGuild> Guilds { get; }
		public LiteCollection<TownUser> Users { get; }

		public TownDatabase(LiteDatabase database)
		{
			this.database = database;

			Guilds = database.GetCollection<TownGuild>("Guilds");
			Users = database.GetCollection<TownUser>("Users");
		}

		public TownGuild GetGuild(IGuild guild)
		{
			return Guilds.FindById(guild.Id);
		}

		public TownUser GetUser(IUser user)
		{
			TownUser result = Users.FindById(user.Id);

			if (result == null)
			{
				result = new TownUser() { UserId = user.Id };

				Users.Insert(result);
			}

			return result;
		}
	}
}
