using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Discord.Addons.CommandCache;
using LiteDB;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TownCrier.Database;

namespace TownCrier.Services
{
	public class CommandHandlingService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private readonly InteractiveService _interactive;
		private readonly IConfiguration _config;
		private readonly LiteDatabase _database;
		private CommandCacheService _cache;
		private IServiceProvider _provider;
		private bool Ready = false;

		public CommandHandlingService(IConfiguration config, IServiceProvider provider,LiteDatabase liteDatabase, DiscordSocketClient discord, CommandService commands, CommandCacheService cache, InteractiveService interactive)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_config = config;
			_interactive = interactive;
			_cache = cache;
			_database = liteDatabase;

			_discord.MessageReceived += MessageReceived;
			_discord.ReactionAdded += OnReactAdded;
			_discord.MessageUpdated += OnMessageUpdated;
		}

		public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _OldMsg, SocketMessage NewMsg, ISocketMessageChannel Channel)
		{
			var OldMsg = await _OldMsg.DownloadAsync();
			if (OldMsg.Source != MessageSource.User) return;


			if (_cache.TryGetValue(NewMsg.Id, out var CacheMsg))
			{
				var reply = await Channel.GetMessageAsync(CacheMsg.First());
				await reply.DeleteAsync();
			}
			await MessageReceived(NewMsg);
		}


		public async Task OnReactAdded(Cacheable<IUserMessage, ulong> _msg, ISocketMessageChannel channel, SocketReaction reaction)
		{
			//Jira Implementation here
		}

		public async Task InitializeAsync(IServiceProvider provider)
		{
			_provider = provider;
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
			// Add additional initialization code here...
		}

		private async Task MessageReceived(SocketMessage rawMessage)
		{
			// Ignore system messages and messages from bots
			if (!(rawMessage is SocketUserMessage message)) return;
			if (message.Source != MessageSource.User) return;

			var context = new SocketCommandContext(_discord, message);
			var Guild = (context.Guild == null) ? null : _database.GetCollection<TownGuild>("Guilds").FindOne(x => x.GuildId == context.Guild.Id);

			int argPos = 0;
			if (Guild == null && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;
			if (Guild != null && !message.HasStringPrefix(Guild.Prefix, ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

			if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1)
			{
				var chance = new Random().Next(0, 100);
				if (chance <= 25)
				{
					await context.Channel.SendMessageAsync("OOPSIE WOOPSIE!! Uwu We made a fucky wucky!! A wittle fucko boingo! The code monkeys at our headquarters are working VEWY HAWD to fix this!");
					return;
				}
			}
			var result = await _commands.ExecuteAsync(context, argPos, _provider);

			if (result.Error.HasValue && (result.Error.Value != CommandError.UnknownCommand))
			{
				Console.WriteLine(result.Error + "\n" + result.ErrorReason);
			}
		}
	}
}
