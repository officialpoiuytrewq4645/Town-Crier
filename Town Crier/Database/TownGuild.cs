using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using Discord;
using Discord.WebSocket;

namespace TownCrier.Database
{
	public class TownGuild
	{
		[BsonId]
		public ulong GuildId { get; set; }
		public string Prefix { get; set; } = "!";


		public List<GivableRole> GivableRoles { get; set; } = new List<GivableRole>();
		
	}
	public class GivableRole
	{
		public ActivityType ActivityType { get; set; }
		public string ActivityName { get; set; }
		public SocketRole AssociatedRole { get; set; }
	}
}
