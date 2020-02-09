using Alta.WebApi.Models;
using Alta.WebApi.Models.DTOs.Responses;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier
{
	[Group("account")]
	public class AccountModule : InteractiveBase<SocketCommandContext>
	{
		public AltaAPI AltaApi { get; set; }
		public TownDatabase Database { get; set; }

		public AccountService AccountService { get; set; }

		//public class AccountDatabase
		//{
		//	public Dictionary<ulong, AccountInfo> accounts = new Dictionary<ulong, AccountInfo>();

		//	public Dictionary<int, ulong> altaIdMap = new Dictionary<int, ulong>();

		//	public SortedSet<AccountInfo> expiryAccounts = new SortedSet<AccountInfo>(new AccountInfo.Comparer());
		//}

		//public class AccountInfo
		//{
		//	public class Comparer : IComparer<AccountInfo>
		//	{
		//		public int Compare(AccountInfo x, AccountInfo y)
		//		{
		//			return x.supporterExpiry.CompareTo(y.supporterExpiry);
		//		}
		//	}

		//	public ulong discordIdentifier;
		//	public int altaIdentifier;
		//	public DateTime supporterExpiry;
		//	public bool isSupporter;
		//	public string username;
		//}

		[RequireUserPermission(GuildPermission.Administrator)]
		[Command("investigate")]
		public async Task Investigate(SocketGuildUser user)
		{
			await ReplyAsync(user.Mention + " " + user.JoinedAt.Value.ToLocalTime() + " " + user.CreatedAt.ToLocalTime());
		}

		[RequireUserPermission(GuildPermission.Administrator)]
		[Command("removethespamplz")]
		public async Task RemoveTheSpamPlz(int count)
		{
			await Context.Guild.DownloadUsersAsync();
			
			foreach (SocketGuildUser user in Context.Guild.Users)
			{
				if (user.JoinedAt.HasValue && DateTime.UtcNow - user.JoinedAt.Value.UtcDateTime < TimeSpan.FromMinutes(80))
				{
					if (DateTime.UtcNow - user.CreatedAt < TimeSpan.FromDays(1))
					{
						count++;

						await ReplyAsync("Kicking " + user.Mention + $"(#{count})" + user.JoinedAt.Value.ToLocalTime() + " " + user.CreatedAt.ToLocalTime());
						await user.KickAsync();
					}
				}
			}

			await ReplyAsync("I'm done joel, dammit");
		}

		[RequireUserPermission(GuildPermission.Administrator)]
		[Command("test")]
		public async Task Test()
		{
			await AccountService.SendGeneralMessage(Context.User as SocketGuildUser);
		}

		[Command]
		public async Task AccountInfo()
		{
			TownUser user = Database.GetUser(Context.User);

			if (user.AltaInfo == null || user.AltaInfo.Identifier == 0)
			{
				await ReplyAsync("You have not linked your alta account. Go to the launcher to link your account");
			}
			else
			{
				var account = await AltaApi.ApiClient.ShopClient.Account.GetShopAccountInfo(user.AltaInfo.Identifier);

				var stats = await AltaApi.ApiClient.UserClient.GetUserStatisticsAsync(user.AltaInfo.Identifier);

				await ReplyAsync($"In Game Username: {user.AltaInfo.Username}\nSupporter: {account.MemberStatus.IsMember}\nPlay Time: {stats.PlayTime.TotalHours:0.0} hours\nCreated Account: {stats.SignupTime.ToShortDateString()} ({(DateTime.UtcNow - stats.SignupTime).TotalDays:0} days ago) ");
			}
		}

		[Command("full")]
		public async Task AccountInfoFull()
		{
			TownUser user = Database.GetUser(Context.User);

			if (user.AltaInfo == null || user.AltaInfo.Identifier == 0)
			{
				await ReplyAsync("You have not linked your alta account. Go to the launcher to link your account");
			}
			else
			{
				var account = await AltaApi.ApiClient.ShopClient.Account.GetShopAccountInfo(user.AltaInfo.Identifier);

				var stats = await AltaApi.ApiClient.UserClient.GetUserStatisticsAsync(user.AltaInfo.Identifier);

				await ReplyAsync($"In Game Username: {user.AltaInfo.Username}\nTalems: {account.ShardBalance}\nSupporter: {account.MemberStatus.IsMember}\nSupporter End: {account.MemberStatus.MemberEndDate}\nPlay Time: {stats.PlayTime.TotalHours:0.0} hours\nCreated Account: {stats.SignupTime.ToShortDateString()} ({(DateTime.UtcNow - stats.SignupTime).TotalDays:0} days ago) ");
			}
		}

		[Command("talems")]
		public async Task GetTalems()
		{
			TownUser user = Database.GetUser(Context.User);

			if (user.AltaInfo == null || user.AltaInfo.Identifier == 0)
			{
				await ReplyAsync("You have not linked your alta account. Go to the launcher to link your account");
			}
			else
			{
				var account = await AltaApi.ApiClient.ShopClient.Account.GetShopAccountInfo();

				await ReplyAsync($"You have {account.ShardBalance} Talems");
			}
		}

		[Command("fix-names")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task FixAllUsernames()
		{
			foreach (var user in Database.Users.FindAll())
			{
				if (user.AltaInfo != null && user.AltaInfo.Identifier > 0 && string.IsNullOrEmpty(user.AltaInfo.Username))
				{
					await Task.Delay(50);

					try
					{
						var userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(user.AltaInfo.Identifier);

						user.AltaInfo.Username = userInfo.Username;

						Database.Users.Update(user);

						Console.WriteLine("Fixed missing username, DiscordId: {0} AltaId: {1} - {2}", user.UserId, user.AltaInfo.Identifier, user.AltaInfo.Username);
					}
					catch (Exception e)
					{
						await ReplyAsync(user.AltaInfo.Identifier + " " + user.UserId);
					}
				}
			}

			Console.WriteLine("Done");

			await ReplyAsync("Done");
		}

		[Command("link-all")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task LinkAllExisting()
		{
			foreach (var user in Database.Users.FindAll())
			{
				if (user.AltaInfo != null && user.AltaInfo.Identifier > 0)
				{
					await Task.Delay(50);

					try
					{
						await AltaApi.ApiClient.Account.LinkDiscordAccountAdmin(user.AltaInfo.Identifier, user.UserId);

						Console.WriteLine("Linked account: {0} with {1} - {2}", user.UserId, user.AltaInfo.Identifier, user.AltaInfo.Username);
					}
					catch (Exception e)
					{
						await ReplyAsync(user.AltaInfo.Identifier + " " + user.UserId);
					}
				}
			}

			Console.WriteLine("Done");
			await ReplyAsync("Done");
		}

		class VerifyData
		{
			public string discord;
		}

		// NOTE: Both of these commands will be tied to a global clock that will periodically update all accounts every 15~30 mins.

		[Command("who-reverse")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task WhoReverse(IUser user)
		{
			TownUser entry = Database.GetUser(user);

			if (entry.AltaInfo != null)
			{
				await ReplyAsync(entry.Name + " is " + entry.AltaInfo.Username);
			}
			else
			{
				await ReplyAsync(entry.Name + " hasnt linked their alta account");
			}
		}

		[Command("who")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task Who(string username)
		{
			TownUser entry = Database.Users.FindOne(item => item.AltaInfo != null && string.Compare(item.AltaInfo.Username, username, true) == 0);

			if (entry != null)
			{
				await ReplyAsync(username + " is " + entry.Name);
			}
			else
			{
				await ReplyAsync("Couldn't find " + username);
			}
		}

		[Command("link")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task Link(IUser user, int altaId)
		{
			TownUser entry = Database.GetUser(user);

			await Link(user, entry, altaId, null);

			await ReplyAsync("Done!");
		}

		async Task Link(IUser discordUser, TownUser user, int altaId, string linkToken)
		{
			if (user.AltaInfo == null)
			{
				user.AltaInfo = new UserAltaInfo();
			}

			if (user.AltaInfo.Identifier == altaId)
			{
				await ReplyAsync(discordUser.Mention + ", " + "Already connected!");
				await Context.Message.DeleteAsync();

				await AccountService.UpdateAsync(user, (SocketGuildUser)discordUser);
				return;
			}

			if (user.AltaInfo.Identifier != 0)
			{
				await ReplyAsync(discordUser.Mention + ", " + $"Unlinking your Discord from {user.AltaInfo.Username}...");
				await Context.Message.DeleteAsync();

				user.AltaInfo.Unlink();

				Database.Users.Update(user);
			}

			if (Database.Users.Exists(x => x.AltaInfo != null && x.AltaInfo.Identifier == altaId && x.UserId != discordUser.Id))
			{
				var oldUsers = Database.Users.Find(x => x.AltaInfo.Identifier == altaId && x.UserId != discordUser.Id);

				foreach (var x in oldUsers)
				{
					var olddiscorduser = Context.Client.GetUser(x.UserId);

					await ReplyAsync(discordUser.Mention + ", " + $"Unlinking your Alta account from {olddiscorduser.Mention}...");
					await Context.Message.DeleteAsync();

					x.AltaInfo.Unlink();

					Database.Users.Update(x);
				}
			}

			var userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(altaId);

			user.AltaInfo.Identifier = altaId;
			user.AltaInfo.Username = userInfo.Username;

			Database.Users.Update(user);

			try
			{
				if (linkToken != null)
				{
					await AltaApi.ApiClient.Account.LinkDiscordAccount(linkToken, discordUser.Id);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed linking to discord in the ATT Database");
			}

			await ReplyAsync(Context.User.Mention + ", " + $"Successfully linked to your Alta account! Hey there {user.AltaInfo.Username}!");
			await Context.Message.DeleteAsync();

			await AccountService.UpdateAsync(user, (SocketGuildUser)discordUser);
		}

		[Command("listextra")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task ListExtra()
		{
			foreach (SocketGuildUser user in Context.Guild.GetRole(547202953505800233).Members)
			{
				TownUser entry = Database.GetUser(user);

				if (entry.AltaInfo == null || !entry.AltaInfo.IsSupporter)
				{
					await ReplyAsync(user.Username + " " + (entry.AltaInfo == null));
				}
			}

			await ReplyAsync("Done!");
		}

		[Command("forceall")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task ForceAll()
		{
			await Context.Guild.DownloadUsersAsync();

			await ReplyAsync("Starting...");
			await AccountService.UpdateAll(true);
			await ReplyAsync("Done!");
		}

		[Command("update")]
		public async Task Update()
		{
			TownUser entry = Database.GetUser(Context.User);

			if (entry.AltaInfo != null)
			{
				await AccountService.UpdateAsync(entry, (SocketGuildUser)Context.User);

				await ReplyAsync(Context.User.Mention + ", " + $"Hey {entry.AltaInfo.Username}, your account info has been updated!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
		}

		[Command("forceupdate"), RequireUserPermission(Discord.GuildPermission.ManageGuild)]
		public async Task Update(SocketUser user)
		{
			TownUser entry = Database.GetUser(user);

			if (entry.AltaInfo != null)
			{
				await AccountService.UpdateAsync(entry, (SocketGuildUser)user);

				await ReplyAsync(Context.User.Mention + ", " + $"{entry.AltaInfo.Username}'s account info has been updated!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + user.Username + " have not linked to an Alta account!");
			}
		}

		[Command("unlink")]
		public async Task Unlink()
		{
			var user = Database.GetUser(Context.User);

			if (user.AltaInfo != null && user.AltaInfo.Identifier != 0)
			{
				user.AltaInfo.Unlink();

				Database.Users.Update(user);

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
			TownUser user = Database.GetUser(Context.User);

			if (user.AltaInfo == null || user.AltaInfo.Identifier == 0)
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + $"Your account is currently linkedto " + user.AltaInfo.Username + "!");
			}
		}

		[Command("Verify")]
		public async Task Verify([Remainder]string encoded)
		{
			JwtSecurityToken token;
			Claim userData;
			Claim altaId;

			TownUser user = Database.GetUser(Context.User);

			try
			{
				token = new JwtSecurityToken(encoded);

				userData = token.Claims.FirstOrDefault(item => item.Type == "user_data");
				altaId = token.Claims.FirstOrDefault(item => item.Type == "UserId");
			}
			catch
			{
				await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token.");
				await Context.Message.DeleteAsync();
				return;
			}

			if (userData == null || altaId == null)
			{
				await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token.");
				await Context.Message.DeleteAsync();
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
						await Context.Message.DeleteAsync();
						return;
					}

					int id = int.Parse(altaId.Value);

					bool isValid = await AltaApi.ApiClient.ServicesClient.IsValidShortLivedIdentityTokenAsync(token);

					if (isValid)
					{
						await Link(Context.User, user, id, encoded);
					}
					else
					{
						await ReplyAsync(Context.User.Mention + ", " + "Invalid token! Try creating a new one!");
						await Context.Message.DeleteAsync();
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