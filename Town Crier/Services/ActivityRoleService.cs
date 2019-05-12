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

	public class ActivityDefinition
	{
		readonly string nameRegex;
		readonly string roleName;
		readonly int activities;

		Dictionary<IGuild, IRole> guildToRoleMap = new Dictionary<IGuild, IRole>();

		public ActivityDefinition(string nameRegex, string roleName, ActivityFlag activities = (ActivityFlag)(-1))
		{
			this.nameRegex = nameRegex;

			this.roleName = roleName;

			this.activities = (int)activities;
		}

		public void InitializeForGuild(IGuild guild)
		{
			if (!guildToRoleMap.ContainsKey(guild))
			{
				IRole role = guild.Roles.FirstOrDefault(test => test.Name == roleName);

				guildToRoleMap.Add(guild, role);
			}
		}

		public async Task ApplyRole(IGuildUser user)
		{
			IRole role;

			if (TryGetRole(user.Guild, out role))
			{
				await ApplyRole(role, user);
			}
		}

		public async Task ApplyRole(SocketGuild guild)
		{
			IRole role;

			if (TryGetRole(guild, out role))
			{
				foreach (IGuildUser user in guild.Users)
				{
					await ApplyRole(role, user);
				}
			}
		}

		public async Task RemoveRole(SocketGuild guild)
		{
			IRole role;

			if (TryGetRole(guild, out role))
			{
				foreach (IGuildUser user in guild.Users)
				{
					await user.RemoveRoleAsync(role);
				}
			}
		}

		async Task ApplyRole(IRole role, IGuildUser user)
		{
			if (IsMatched(user.Activity))
			{
				if (!user.RoleIds.Contains(role.Id))
				{
					await user.AddRoleAsync(role);
				}
			}
			else if (user.RoleIds.Contains(role.Id))
			{
				await user.RemoveRoleAsync(role);
			}
		}

		bool TryGetRole(IGuild guild, out IRole role)
		{
			return guildToRoleMap.TryGetValue(guild, out role);
		}

		bool IsMatched(IActivity activity)
		{
			return
				activity != null &&
				(activities & (1 << (int)activity.Type)) != 0 &&
				Regex.IsMatch(activity.Name, nameRegex, RegexOptions.IgnoreCase);
		}
	}

	public class ActivityRoleService
	{
		public bool IsEnabled { get;  set; }

		 readonly DiscordSocketClient client;
		 readonly IServiceProvider provider;
		 readonly TownDatabase database;

		List<ActivityDefinition> activities = new List<ActivityDefinition>();
		List<SocketGuild> guilds = new List<SocketGuild>();

		public ActivityRoleService(IServiceProvider provider, DiscordSocketClient client, TownDatabase database)
		{
			this.provider = provider;
			this.client = client;
			this.database = database;

			this.client.GuildMemberUpdated += UserUpdated;

			this.client.Ready += Ready;
		}

		async Task Ready()
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
				IEnumerable<ActivityRole> oldPotentialRoles = oldUser == null ? guild.ActivityRoles : guild.ActivityRoles.Where(x => x.ActivityType == oldUser.Activity.Type);

				// Removing old roles
				foreach (var activity in oldPotentialRoles)
				{
					IRole role = newUser.Roles.FirstOrDefault(x => x.Id == activity.AssociatedRole);

					if (role != null)
					{
						if (!Regex.IsMatch(newUser.Activity.Name, activity.ActivityName))
						{
							await newUser.RemoveRoleAsync(role);
						}
					}
				}
			}

			if (newUser.Activity != null)
			{
				// Adding new roles
				foreach (var activity in guild.ActivityRoles.Where(x => x.ActivityType == newUser.Activity.Type))
				{
					IRole role = newUser.Guild.GetRole(activity.AssociatedRole);

					if (!newUser.Roles.Contains(role))
					{
						if (Regex.IsMatch(newUser.Activity.Name, activity.ActivityName))
						{
							await newUser.AddRoleAsync(role);
						}
					}
				}
			}
		}

		bool RegexValidate(string activity, string regex)
		{
			return new Regex(regex).IsMatch(activity);
		}
	}
}
