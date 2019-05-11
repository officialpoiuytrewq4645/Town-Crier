using Alta.WebApi.Models;
using LiteDB;
using System;

namespace TownCrier.Database
{
	public class TownUser
	{
		/// <summary>
		/// The user's Discord ID
		/// </summary>
		[BsonId]
		public ulong UserId { get; set; }
		
		/// <summary>
		/// Whether or not the user is an admin on this Guild's itteration of ChatCraft
		/// </summary>
		public bool isAdmin { get; set; } = false;
		
		/// <summary>
		/// Date in which the user first joined the server.
		/// </summary>
		public DateTime InitialJoin { get; set; }

		public UserAltaInfo AltaInfo { get; set; }

		public UserScording Scoring { get; set; }
	}

	public class UserScording
	{
		public DateTime LastMessage { get; set; }
		public uint Score { get; set; } = 0;

		public uint UsedHourPoints { get; set; } = 0;
		public DateTime UsedFirstHourPoint { get; set; }
	}

	public class UserAltaInfo
	{
		/// <summary>
		/// Alta ID
		/// </summary>
		public int Identifier { get; set; }

		/// <summary>
		/// Date of Expiration of their supporter status
		/// </summary>
		public DateTime? SupporterExpiry { get; set; }

		/// <summary>
		/// Returns wether or not the user is currently a supporter.
		/// </summary>
		public bool IsSupporter { get; set; }

		/// <summary>
		/// Returns the Alta username tied to this account (Updates evert 15 minutes)
		/// </summary>
		public string Username { get; set; }

		public void UpdateAltaCredentials(UserInfo info)
		{
			Identifier = info.Identifier;
			Username = info.Username;
		}

		public void Unlink()
		{
			Identifier = 0;
			SupporterExpiry = null;
			IsSupporter = false;
			Username = null;
		}
	}

	//public class ChattyUser
	//{
	//	public int bet { get; set; } = 0;

	//	public int coins { get; set; } = 0;

	//	/// <summary>
	//	/// The user's current location
	//	/// </summary>
	//	//public Location CurrentLocation { get; set; }

	//	//public List<Location> Locations { get; set; } = new List<Location>();
	//	//public List<Recipe> Recipes { get; set; } = new List<Recipe>();

	//	/// <summary>
	//	/// Amount of spars the user has won.
	//	/// </summary>
	//	public int WonSpars { get; set; } = 0;
	//	/// <summary>
	//	/// Total amount of spars fought.
	//	/// </summary>
	//	public int Spars { get; set; } = 0;

	//	/// <summary>
	//	/// The win-loss ration % of spars.
	//	/// </summary>
	//	[BsonIgnore]
	//	public decimal WinLoseRatio
	//	{
	//		get
	//		{
	//			return Math.Round((decimal)(Spars * WonSpars) / 100, 2);
	//		}
	//		set { }
	//	}
	//}
}
