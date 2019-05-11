using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier.Modules.Admin
{
	[Group("guild"), RequireUserPermission(Discord.GuildPermission.ManageGuild)]
	public class GuildModule : InteractiveBase<SocketCommandContext>
	{
		public delegate bool TryGetValue<T>(SocketMessage message, out T value);
		public delegate bool TryGetValueText<T>(string message, out T value);

		const char SkipCharacter = 'x';

		public TownDatabase Database { get; set; }

		[Command("initialize"), Alias("init")]
		public async Task Initialize()
		{
			TownGuild guild = Database.GetGuild(Context.Guild);

			bool isNew = guild == null;

			if (isNew)
			{
				guild = new TownGuild()
				{
					GuildId = Context.Guild.Id
				};

			}

			await ReplyAsync("Initializing Guild...\n" +
				$"Please answer the following questions ('{SkipCharacter}' to skip / leave default)");

			await AskString($"What is your desired prefix?", guild.Prefix, value => guild.Prefix = value);

			await AskChannel($"Where should newcomers be announced?", guild.NotificationChannel, value => guild.NotificationChannel = value);

			await AskString("How should newcomers be announced? Use {user} to mention them, and {server} to show the servers name.", guild.WelcomeMessage, value => guild.WelcomeMessage = value);

			await AskChannel($"Where should leavers be announced?", guild.LeaverChannel, value => guild.LeaverChannel = value);

			await ReplyAsync("All configured!");

			if (isNew)
			{
				Database.Guilds.Insert(guild);
			}
			else
			{
				Database.Guilds.Update(guild);
			}
		}

		public Task<bool> AskInt(string question, int defaultValue, Action<int> apply)
		{
			return Ask<int>(question, defaultValue, apply, int.TryParse);
		}

		public Task<bool> AskBool(string question, bool defaultValue, Action<bool> apply)
		{
			return Ask<bool>(question, defaultValue, apply, bool.TryParse);
		}

		public Task<bool> AskString(string question, string defaultValue, Action<string> apply)
		{
			return Ask<string>(question, defaultValue, apply, (SocketMessage message, out string value) =>
			{
				value = message.Content;
				return true;
			});
		}

		public Task<bool> AskChannel(string question, ulong defaultValue, Action<ulong> apply)
		{
			return Ask<ulong>(question, defaultValue, apply, (SocketMessage message, out ulong value) =>
			{
				if (message.MentionedChannels.Count == 1)
				{
					value = message.MentionedChannels.First().Id;
					return true;
				}

				value = 0;
				return false;
			});
		}

		public Task<bool> Ask<T>(string question, T defaultValue, Action<T> apply, TryGetValueText<T> getValue)
		{
			return Ask(question, defaultValue, apply, (SocketMessage message, out T value) => getValue(message.Content, out value));
		}

		public async Task<bool> Ask<T>(string question, T defaultValue, Action<T> apply, TryGetValue<T> getValue)
		{
			if (defaultValue != null)
			{
				question += $" (Default {defaultValue})";
			}

			await ReplyAsync(question);

			SocketMessage response;
			T result;

			do
			{
				response = await Interactive.NextMessageAsync(Context, true, true);

				if (defaultValue != null && response.Content.Length == 1 && response.Content[0] == SkipCharacter)
				{
					return false;
				}

			}
			while (!getValue(response, out result));

			apply(result);

			return true;
		}
	}
}
