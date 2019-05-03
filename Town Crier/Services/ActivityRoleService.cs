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

	public class ActivityRoleManager
	{
		public bool IsEnabled { get; private set; }

		private readonly DiscordSocketClient _client;
		private readonly CommandService _command;
		private readonly IServiceProvider _provider;
		private readonly LiteDatabase _database;
		private readonly IConfiguration _config;

		List<ActivityDefinition> activities = new List<ActivityDefinition>();
		List<SocketGuild> guilds = new List<SocketGuild>();

		public ActivityRoleManager(IServiceProvider provider, DiscordSocketClient client, LiteDatabase database)
		{
			_provider = provider;
			_client = client;
			_database = database;

			_client.GuildMemberUpdated += _client_UserUpdated;
		}

		private async Task _client_UserUpdated(SocketGuildUser OldUser, SocketGuildUser NewUser)
		{
			// If both activities are the same (in type & message), return
			if (OldUser.Activity == NewUser.Activity) return; 

			// Fetch the guild from the Database
			var guild = _database.GetCollection<TownGuild>("Guilds").FindById(NewUser.Guild.Id);
			
			// Pull all roles tied to that particular activity type
			foreach(var x in guild.GivableRoles.Where(x=>x.ActivityType == NewUser.Activity.Type))
			{
				// If the user wasn't playing/watching/streaming X but now is, give the role
				if(OldUser.Activity.Name.ToLower() != x.ActivityName 
					&& NewUser.Activity.Name.ToLower()==x.ActivityName.ToLower())
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

		private bool RegexValidate(string activity, string regex)
		{
			return new Regex(regex).IsMatch(activity);
		}
	}
}
