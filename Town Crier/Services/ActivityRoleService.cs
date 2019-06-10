using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier
{
	public enum ActivityFlag
	{
		Playing = 1 << ActivityType.Playing,
		Streaming = 1 << ActivityType.Streaming,
		Listening = 1 << ActivityType.Listening,
		Watching = 1 << ActivityType.Watching,
	}
	
	public class ActivityRoleService
	{
		public bool IsEnabled { get;  set; }

		readonly DiscordSocketClient client;
		readonly IServiceProvider provider;
		readonly TownDatabase database;
		
		public ActivityRoleService(IServiceProvider provider, DiscordSocketClient client, TownDatabase database)
		{
			this.provider = provider;
			this.client = client;
			this.database = database;

			this.client.GuildMemberUpdated += UserUpdated;

			Ready();
		}

		async void Ready()
		{
			foreach (TownGuild guild in database.Guilds.FindAll())
			{
				if (guild.ActivityRoles.Count == 0)
				{
					continue;
				}

				SocketGuild discordGuild = client.GetGuild(guild.GuildId);

				await discordGuild.DownloadUsersAsync();
				
				foreach (SocketGuildUser user in discordGuild.Users)
				{
					await UpdateRoles(null, user, guild);
				}
			}
		}

		async Task UserUpdated(SocketGuildUser oldUser, SocketGuildUser newUser)
		{
			// If both activities are the same (in type & message), return
			if (oldUser.Activity == newUser.Activity) return;

			// Fetch the guild from the Database
			var guild = database.GetGuild(newUser.Guild);

			await UpdateRoles(oldUser, newUser, guild);
		}

		async Task UpdateRoles(SocketGuildUser oldUser, SocketGuildUser newUser, TownGuild guild)
		{
			if (oldUser == null || oldUser.Activity != null)
			{
				int activityFlag = oldUser == null ? 0 : 1 << (int)oldUser.Activity.Type;

				IEnumerable<ActivityRole> oldPotentialRoles = oldUser == null ? guild.ActivityRoles : guild.ActivityRoles.Where(x => ((int)x.ActivityType & activityFlag) != 0);

				// Removing old roles
				foreach (var activity in oldPotentialRoles)
				{
					IRole role = newUser.Roles.FirstOrDefault(x => x.Id == activity.AssociatedRole);

					if (role != null)
					{
						if (!Regex.IsMatch(newUser.Activity.Name, activity.ActivityName, RegexOptions.IgnoreCase))
						{
							await newUser.RemoveRoleAsync(role);
						}
					}
				}
			}

			if (newUser.Activity != null)
			{
				int activityFlag = 1 << (int)newUser.Activity.Type;

				// Adding new roles
				foreach (var activity in guild.ActivityRoles.Where(x => ((int)x.ActivityType & activityFlag) != 0))
				{
					IRole role = newUser.Guild.GetRole(activity.AssociatedRole);

					if (!newUser.Roles.Contains(role))
					{
						if (Regex.IsMatch(newUser.Activity.Name, activity.ActivityName, RegexOptions.IgnoreCase))
						{
							await newUser.AddRoleAsync(role);
						}
					}
				}
			}
		}
	}
}
