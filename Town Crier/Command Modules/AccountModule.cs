using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Collections.Generic;
using Discord.Addons.Interactive;
using System.Linq;
using System;
using TownCrier.Modules.ChatCraft;
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
	

		public async Task UpdateAsync(AccountInfo account, SocketGuildUser user)
		{
			try
			{
				UserInfo userInfo = await altaApI.ApiClient.UserClient.GetUserInfoAsync(account.altaIdentifier);
				
				MembershipStatusResponse result = await altaApI.ApiClient.UserClient.GetMembershipStatus(account.altaIdentifier);

				if (userInfo == null)
				{
					Console.WriteLine("Couldn't find userinfo for " + account.username);
					return;
				}

				if (result == null)
				{
					Console.WriteLine("Couldn't find membership status for " + account.username);
					return;
				}
				
				account.supporterExpiry = result.ExpiryTime ?? DateTime.MinValue;
				account.isSupporter = result.IsMember;
				account.username = userInfo.Username;

				if (account.isSupporter)
				{
					database.expiryAccounts.Add(account);
				}
				
				if (user == null)
				{
					user = guild.GetUser(account.discordIdentifier);
				}

				if (user == null)
				{
					Console.WriteLine("Couldn't find Discord user for " + account.username + " " + account.discordIdentifier);
					return;
				}

				if (supporterRole == null)
				{
					supporterRole = guild.GetRole(547202953505800233);
					supporterChannel = guild.GetTextChannel(547204432144891907);
					generalChannel = guild.GetChannel(334933825383563266) as SocketTextChannel;
				}
				
				if (account.isSupporter)
				{
					if (user.Roles == null || !user.Roles.Contains(supporterRole))
					{
						try
						{
							await user.AddRoleAsync(supporterRole);
						}
						catch (Exception e)
						{
							Console.WriteLine("Error adding role");
							Console.WriteLine(user);
							Console.WriteLine(supporterRole);
						}

						await supporterChannel.SendMessageAsync($"{user.Mention} joined. Thanks for the support!");
						await generalChannel.SendMessageAsync($"{user.Mention} became a supporter! Thanks for the support!\nIf you'd like to find out more about supporting, visit https://townshiptale.com/supporter");
					}
				}
				else
				{
					await user.RemoveRoleAsync(supporterRole);
				}

				isChanged = true;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error updating " + account.username);
				Console.WriteLine(e.Message);
			}
		}

		[Command("who")]
		[RequireUserPermission(GuildPermission.KickMembers)]
		public async Task Who([Remainder] SocketUser Username)
		{
			var Townie = database.GetCollection<TownResident>("Users").FindById(Username.Id);

			if (info != null)
			{
				await ReplyAsync(Context.User.Mention+", "+ username + " is " + guild.GetUser(info.discordIdentifier)?.Username);
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ","+"Couldn't find " + username);
			}
		}

		[Command("update")]
		public async Task Update()
		{
			if (database.accounts.TryGetValue(Context.User.Id, out AccountInfo info))
			{
				await UpdateAsync(info, (SocketGuildUser)Context.User);

				await ReplyAsync(Context.User.Mention + ", " + $"Hey {info.username}, your account info has been updated!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
		}


		[Command("forceupdate")]
		public async Task Update(SocketUser user)
		{
			if (database.accounts.TryGetValue(user.Id, out AccountInfo info))
			{
				await UpdateAsync(info, null);

				await ReplyAsync(Context.User.Mention + ", " + $"{info.username}'s account info has been updated!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + user.Username + " have not linked to an Alta account!");
			}
		}


		[Command("unlink")]
		public async Task Unlink()
		{
			if (database.accounts.TryGetValue(Context.User.Id, out AccountInfo info))
			{
				database.accounts.Remove(Context.User.Id);
				database.expiryAccounts.Remove(info);
				database.altaIdMap.Remove(info.altaIdentifier);

				await ReplyAsync(Context.User.Mention + ", " + "You are no longer linked to an Alta account!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
		}

		[Command(), Alias("linked")]
		public async Task IsLinked()
		{
			if (database.accounts.TryGetValue(Context.User.Id, out AccountInfo info))
			{
				await ReplyAsync(Context.User.Mention+", "+ $"Hey {info.username}, your account is linked!");
			}
			else
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
			}
		}
		[Command("verify")]
		[RequireContext(ContextType.Guild)]
		public async Task Verify([Remainder]string encoded)
		{
			await Context.Message.DeleteAsync();
			await ReplyAsync("For security reasons, you can only use this command via DMs! Please send this command again via DMs.");
		}
		[Command("verify")] [RequireContext(ContextType.DM)]
		public async Task Verify([Remainder]string encoded)
		{
			JwtSecurityToken token;
			Claim userData;
			Claim altaId;

			var UserCollection = database.GetCollection<TownResident>("Users");
			var user = UserCollection.FindById(Context.User.Id);

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

						AccountInfo old = database.accounts[database.altaIdMap[id]];

						SocketGuildUser oldUser = Context.Guild.GetUser(old.discordIdentifier);

						await ReplyAsync(Context.User.Mention + ", " + $"Unlinking your Alta account from {oldUser.Mention}...");
							
						database.accounts.Remove(database.altaIdMap[id]);
						database.expiryAccounts.Remove(old);
					}

					database.altaIdMap[id] = Context.User.Id;

					AccountInfo account;

					if (database.accounts.TryGetValue(Context.User.Id, out account))
					{
						await ReplyAsync(Context.User.Mention + ", " + $"Unlinking your Discord from {account.username}...");
					}
					else
					{
						account = new AccountInfo()
						{
							discordIdentifier = Context.User.Id
						};

						database.accounts.Add(account.discordIdentifier, account);
					}

					account.altaIdentifier = id;

					await UpdateAsync(account, (SocketGuildUser)Context.User);

					await ReplyAsync(Context.User.Mention + ", " + $"Successfully linked to your Alta account! Hey there {account.username}!");
						
					isChanged = true;
					
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