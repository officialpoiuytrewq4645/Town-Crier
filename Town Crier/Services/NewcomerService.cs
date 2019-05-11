using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LiteDB;
using TownCrier.Database;

namespace TownCrier.Services
{
	public class NewcomerService
	{
		private readonly DiscordSocketClient _discord;
		private IServiceProvider _provider;
		private readonly IConfiguration _config;
		private readonly LiteDatabase _database;

		public NewcomerService(DiscordSocketClient client,LiteDatabase database,IServiceProvider provider,IConfiguration configuration)
		{
			_discord = client;
			_provider = provider;
			_database = database;
			_config = configuration;

			_discord.UserJoined += UserJoined;
			_discord.UserLeft += AlertTeam;
		}

		public async Task UserJoined(SocketGuildUser user)
		{
			// Locate TownGuild in the database
			var guild = _database.GetCollection<TownGuild>("Guild").FindById(user.Guild.Id);
			
			// Create a variable that contains the SocketGuild associated with the TownGuild
			var discordguild = _discord.GetGuild(guild.GuildId);

			// TextChannel where Notifications go
			var NotificationChannel = discordguild.GetTextChannel(guild.NotificationChannel);

			// Send welcome message, parsing the welcome message string from the TownGuild property
			if (guild.WelcomeMessage != "")
			{
				var welcome = await NotificationChannel.SendMessageAsync(guild.ParseMessage(user, _discord));

				await welcome.AddReactionAsync(new Emoji("👋"));
			}

			if (guild.MilestoneMessage!="" &&(discordguild.Users.Count % guild.MilestoneMarker) == 0)
			{
				await NotificationChannel.SendMessageAsync($"We've now hit {discordguild.Users.Count} members! Wooooo!");

				await Task.Delay(1000 * 20);

				await NotificationChannel.SendMessageAsync($"Partaayyy!");
			}

			// Fetch the collection of TownResidents
			var Residents = _database.GetCollection<TownResident>("Users");

			// If there's no entry in the user database for this user, add one.
			if (!Residents.Exists(x => x.UserId == user.Id)) Residents.Insert(new TownResident() { UserId = user.Id });

		}

		public async Task AlertTeam(SocketGuildUser user)
		{
			// Locate TownGuild in the database
			var guild = _database.GetCollection<TownGuild>("Guild").FindById(user.Guild.Id);

			// Create a variable that contains the SocketGuild associated with the TownGuild
			var discordguild = _discord.GetGuild(guild.GuildId);

			// TextChannel where Notifications go
			var AdminChannel = discordguild.GetTextChannel(guild.AdminChannel);

			// Fetch the user's TownResident entry
			var Resident = _database.GetCollection<TownResident>("Users").FindById(user.Id);

			await AdminChannel.SendMessageAsync("The user: " + user.Username + " left. They joined: " + Resident.InitialJoin.ToString("dd/MMM/yyyy hh:mm: tt"));
		}
	}
}
