using Alta.WebApi.Models;
using Alta.WebApi.Models.DTOs.Responses;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier.Database;

namespace TownCrier.Services
{
	public class AccountService
	{
		public TownDatabase Database { get; set; }
		public AltaAPI AltaApi { get; set; }
		public TimerService Timer { get; set; }

		static SocketGuild guild;
		static SocketRole supporterRole;
		static SocketTextChannel supporterChannel;
		static SocketTextChannel generalChannel;

		public AccountService()
		{
			Timer.OnClockInterval += UpdateAll;

			foreach (TownUser user in Database.Users.Find(item => item.AltaInfo != null))
			{
				UpdateAsync(user, null).Wait();
			}
		}

		async void UpdateAll(object sender, IServiceProvider e)
		{
			DateTime time = DateTime.Now;

			foreach (TownUser user in Database.Users.Find(item => item.AltaInfo != null && item.AltaInfo.SupporterExpiry < time))
			{
				await UpdateAsync(user, null);
			}
		}

		public async Task UpdateAsync(TownUser townUser, SocketGuildUser user)
		{
			try
			{
				UserInfo userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(townUser.AltaInfo.Identifier);

				MembershipStatusResponse result = await AltaApi.ApiClient.UserClient.GetMembershipStatus(townUser.AltaInfo.Identifier);

				if (userInfo == null)
				{
					Console.WriteLine("Couldn't find userinfo for " + townUser.Name);
					return;
				}

				if (result == null)
				{
					Console.WriteLine("Couldn't find membership status for " + townUser.Name);
					return;
				}

				townUser.AltaInfo.SupporterExpiry = result.ExpiryTime ?? DateTime.MinValue;
				townUser.AltaInfo.IsSupporter = result.IsMember;
				townUser.AltaInfo.Username = userInfo.Username;


				if (user == null)
				{
					user = guild.GetUser(townUser.UserId);
				}

				if (user == null)
				{
					Console.WriteLine("Couldn't find Discord user for " + townUser.Name + " " + townUser.UserId);
					return;
				}

				if (supporterRole == null)
				{
					supporterRole = guild.GetRole(547202953505800233);
					supporterChannel = guild.GetTextChannel(547204432144891907);
					generalChannel = guild.GetChannel(334933825383563266) as SocketTextChannel;
				}

				if (townUser.AltaInfo.IsSupporter)
				{
					if (user.Roles == null || !user.Roles.Contains(supporterRole))
					{
						try
						{
							await user.AddRoleAsync(supporterRole);
						}
						catch (Exception)
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

				Database.Users.Update(townUser);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error updating " + townUser.Name);
				Console.WriteLine(e.Message);
			}
		}

	}
}
