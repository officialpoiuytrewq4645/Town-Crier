using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Collections.Generic;
using Discord.Addons.Interactive;
using System.Linq;
using System;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Timers;
using Alta.WebApi.Models.DTOs.Responses;
using Discord.WebSocket;
using Alta.WebApi.Models;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier
{
	[Group("account")]
	public class AccountModule : InteractiveBase<SocketCommandContext>
	{
		public AltaAPI altaApI { get; set; }
		public LiteDatabase database { get; set; }

		public class AccountDatabase
		{
			public Dictionary<ulong, AccountInfo> accounts = new Dictionary<ulong, AccountInfo>();

			public Dictionary<int, ulong> altaIdMap = new Dictionary<int, ulong>();

			public SortedSet<AccountInfo> expiryAccounts = new SortedSet<AccountInfo>(new AccountInfo.Comparer());
		}

		public class AccountInfo
		{
			public class Comparer : IComparer<AccountInfo>
			{
				public int Compare(AccountInfo x, AccountInfo y)
				{
					return x.supporterExpiry.CompareTo(y.supporterExpiry);
				}
			}

			public ulong discordIdentifier;
			public int altaIdentifier;
			public DateTime supporterExpiry;
			public bool isSupporter;
			public string username;
		}

		class VerifyData
		{
			public string discord;
		}

		static SocketGuild guild;
		static SocketRole supporterRole;
		static SocketTextChannel supporterChannel;
		static SocketTextChannel generalChannel;

		// NOTE: Both of these commands will be tied to a global clock that will periodically update all accounts every 15~30 mins.

		//[Command("update")]
		//public async Task Update()
		//{
		//	if (database.accounts.TryGetValue(Context.User.Id, out AccountInfo info))
		//	{
		//		await UpdateAsync(info, (SocketGuildUser)Context.User);

		//		await ReplyAsync(Context.User.Mention + ", " + $"Hey {info.username}, your account info has been updated!");
		//	}
		//	else
		//	{
		//		await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
		//	}
		//}


		//[Command("forceupdate")]
		//public async Task Update(SocketUser user)
		//{
		//	if (database.accounts.TryGetValue(user.Id, out AccountInfo info))
		//	{
		//		await UpdateAsync(info, null);

		//		await ReplyAsync(Context.User.Mention + ", " + $"{info.username}'s account info has been updated!");
		//	}
		//	else
		//	{
		//		await ReplyAsync(Context.User.Mention + ", " + user.Username + " have not linked to an Alta account!");
		//	}
		//}


		[Command("unlink")]
		public async Task Unlink()
		{
			var collection= database.GetCollection<TownResident>("Users");
			var townie = collection.FindOne(x => x.UserId == Context.User.Id);

			if (!townie.altaIdentifier.HasValue)
			{
				townie.Unlink();
				await ReplyAsync(Context.User.Mention + ", " + "You are no longer linked to an Alta account!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
		}

		[Command("IsLinked"), Alias("Linked")]
		public async Task IsLinked()
		{
			var UserCollection = database.GetCollection<TownResident>("Users");
			if (!UserCollection.Exists(x => x.UserId != Context.User.Id)) UserCollection.Insert(new TownResident() { UserId = Context.User.Id });

			var user = UserCollection.FindOne(x => x.UserId == Context.User.Id);

			if (!user.altaIdentifier.HasValue)
			{
				await ReplyAsync(Context.User.Mention+", "+ $"Your account is currently linkedto "+user.AltaUsername+"!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
		}
		[Command("Verify")]
		[RequireContext(ContextType.Guild)]
		public async Task Verifywrong([Remainder]string encoded)
		{
			await Context.Message.DeleteAsync();
			await ReplyAsync("For security reasons, you can only use this command via DMs! Please send this command again via DMs.");
		}
		[Command("Verify")] [RequireContext(ContextType.DM)]
		public async Task Verify([Remainder]string encoded)
		{
			JwtSecurityToken token;
			Claim userData;
			Claim altaId;

			var UserCollection = database.GetCollection<TownResident>("Users");
			if (!UserCollection.Exists(x => x.UserId != Context.User.Id)) UserCollection.Insert(new TownResident() { UserId = Context.User.Id });

			
			var user = UserCollection.FindOne(x=>x.UserId==Context.User.Id);

			try
			{
				token = new JwtSecurityToken(encoded);

				userData = token.Claims.FirstOrDefault(item => item.Type == "user_data");
				altaId = token.Claims.FirstOrDefault(item => item.Type == "UserId");
			}
			catch
			{
				await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token.");
				return;
			}

			if (userData == null || altaId == null)
			{
				await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token.");
			}
			else
			{
				try
				{
					VerifyData result = JsonConvert.DeserializeObject<VerifyData>(userData.Value);

					string test = result.discord.ToLower();
					string expected = Context.User.Username.ToLower() + "#" + Context.User.Discriminator;
					string alternate = Context.User.Username.ToLower() + " #" + Context.User.Discriminator;


					if (test != expected.ToLower() && test != alternate.ToLower())
					{
						await ReplyAsync(Context.User.Mention + ", " + "Make sure you correctly entered your account info! You entered: " + result.discord + ". Expected: " + expected);
						return;
					}

					int id = int.Parse(altaId.Value);

					bool isValid = await altaApI.ApiClient.ServicesClient.IsValidShortLivedIdentityTokenAsync(token);

					if (isValid)
					{
						if (user.altaIdentifier == id)
						{
							await ReplyAsync(Context.User.Mention + ", " + "Already connected!");
							return;
						}
						if(user.altaIdentifier.HasValue)
						{
							user.altaIdentifier = null;
							await ReplyAsync(Context.User.Mention + ", " + $"Unlinking your Discord from {user.AltaUsername}...");
						}

						if (UserCollection.Exists(x => x.altaIdentifier == id&&x.UserId!=Context.User.Id))
						{
							var oldUsers = UserCollection.Find(x => x.altaIdentifier == id && x.UserId != Context.User.Id);

							foreach (var x in oldUsers)
							{
								var olddiscorduser = Context.Client.GetUser(x.UserId);

								await ReplyAsync(Context.User.Mention + ", " + $"Unlinking your Alta account from {olddiscorduser.Mention}...");

								x.Unlink();
							}
						}

						user.altaIdentifier = id;
						user.AltaUsername = altaApI.ApiClient.UserClient.GetUserInfoAsync(id).GetAwaiter().GetResult().Username;

						await ReplyAsync(Context.User.Mention + ", " + $"Successfully linked to your Alta account! Hey there {user.AltaUsername}!");
					}
					else
					{
						await ReplyAsync(Context.User.Mention + ", " + "Invalid token! Try creating a new one!");
					}
				}
				catch (Exception e)
				{
					await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token : " + e.Message);
				}
			}
		}
	}
}