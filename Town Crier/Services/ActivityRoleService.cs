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
		}

		 async Task UserUpdated(SocketGuildUser OldUser, SocketGuildUser NewUser)
		{
			// If both activities are the same (in type & message), return
			if (OldUser.Activity == NewUser.Activity) return;

			// Fetch the guild from the Database
			var guild = database.GetGuild(NewUser.Guild);

			// Pull all roles tied to that particular activity type
			foreach (var x in guild.GivableRoles.Where(x => x.ActivityType == NewUser.Activity.Type))
			{
				// If the user wasn't playing/watching/streaming X but now is, give the role
				if (OldUser.Activity.Name.ToLower() != x.ActivityName
					&& NewUser.Activity.Name.ToLower() == x.ActivityName.ToLower())
				{
					await NewUser.AddRoleAsync(x.AssociatedRole);
				}
				//if the user was playing/watching/streaming X but now isn't. remove the role
				if (NewUser.Activity.Name.ToLower() != x.ActivityName
					&& OldUser.Activity.Name.ToLower() == x.ActivityName.ToLower())
				{
					await NewUser.RemoveRoleAsync(x.AssociatedRole);
				}
			}
		}

		bool RegexValidate(string activity, string regex)
		{
			return new Regex(regex).IsMatch(activity);
		}
	}
}
