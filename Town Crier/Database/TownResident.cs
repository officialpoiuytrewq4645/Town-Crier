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
		/// <summary>
		/// The user's Discord ID
		/// </summary>
		[BsonId]
		public ulong UserId { get; set; }

		#region ChatCraft
		public int bet { get; set; } = 0;

		public int coins { get; set; } = 0;

		/// <summary>
		/// The user's current location
		/// </summary>
		public Location currentLocation { get; set; }

		public List<Location> locations { get; set; } = new List<Location>();
		public List<Recipe> recipes { get; set; } = new List<Recipe>();

		/// <summary>
		/// Whether or not the user is an admin on this Guild's itteration of ChatCraft
		/// </summary>
		public bool isAdmin { get; set; } = false;

		/// <summary>
		/// Amount of spars the user has won.
		/// </summary>
		public int WonSpars { get; set; } = 0;
		/// <summary>
		/// Total amount of spars fought.
		/// </summary>
		public int spars { get; set; } = 0;

		/// <summary>
		/// The win-loss ration % of spars.
		/// </summary>
		[BsonIgnore]
		public decimal WinLoseRatio
		{
			get
			{
				return Math.Round((decimal)(spars * WonSpars) / 100, 2);
			}
			private set { }
		}

		/// <summary>
		/// Date in which the user first joined the server.
		/// </summary>
		public DateTime InitialJoin; 

		public DateTime lastMessage { get; set; }
		public uint score { get; set; } = 0;

		public uint usedHourPoints { get; set; } = 0;
		public DateTime usedFirstHourPoint { get; set; }
		#endregion

		#region AltaVariables
		/// <summary>
		/// Alta ID
		/// </summary>
		public int altaIdentifier { get; private set; }
		/// <summary>
		/// Date of Expiration of their supporter status
		/// </summary>
		public DateTime supporterExpiry { get; private set; }
		/// <summary>
		/// Returns wether or not the user is currently a supporter.
		/// </summary>
		public bool isSupporter { get; private set; }
		#endregion
	}
}
