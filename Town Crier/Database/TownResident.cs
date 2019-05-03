using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using TownCrier.Modules.ChatCraft;

namespace TownCrier.Database
{
	public class TownResident
	{
		[BsonId]
		public ulong UserId { get; set; }

		#region ChatCraft
		public int bet { get; set; } = 0;

		public int coins { get; set; } = 0;

		public Location currentLocation { get; set; }

		public List<Location> locations { get; set; } = new List<Location>();
		public List<Recipe> recipes { get; set; } = new List<Recipe>();

		public bool isAdmin { get; set; } = false;

		public int sparWins { get; set; } = 0;
		public int spars { get; set; } = 0;

		//public DateTime joined; 
		//Uneeded, it's a property from SocketGuildUser

		public DateTime lastMessage { get; set; }
		public uint score { get; set; } = 0;

		public uint usedHourPoints { get; set; } = 0;
		public DateTime usedFirstHourPoint { get; set; }
		#endregion

		#region AltaVariables
		public int altaIdentifier { get; private set; }
		public DateTime supporterExpiry { get; private set; }
		public bool isSupporter { get; private set; }
		#endregion
	}
}
