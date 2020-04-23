using Discord;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier
{
	public class UserJoinManagement
	{
		TownDatabase database;
		DiscordSocketClient client;
		Dictionary<ulong, TownGuild> settings = new Dictionary<ulong, TownGuild>();
		bool hasSubscribed;

		public UserJoinManagement(DiscordSocketClient client, TownDatabase database)
		{
			this.database = database;
			this.client = client;

			client.GuildAvailable += GuildAvailable;
		}

		Task GuildAvailable(SocketGuild arg)
		{
			var guild = database.Guilds.FindById(arg.Id);

			if (guild.RoleGrantingSettings != null)
			{
				settings.Add(guild.GuildId, guild);

				Console.WriteLine("Handling reactions in guild: " + guild.GuildId);

				try
				{
					ValidateAllReactionsAndRolesForGuild(guild);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Console.WriteLine("Failed validating all emotes on startup");
				}
			}

			if (!hasSubscribed)
			{
				hasSubscribed = true;

				client.ReactionAdded += HandleReactionAdded;

				Console.WriteLine("Starting to listen to reactions");
			}

			return Task.CompletedTask;
		}

		async void ValidateAllReactionsAndRolesForGuild(TownGuild guildSettings)
		{
			SocketGuild guild = client.GetGuild(guildSettings.GuildId);

			foreach (var messageSettings in guildSettings.RoleGrantingSettings.MessageSettings)
			{
				var channel = guild.GetChannel(messageSettings.Channel);

				var message = await (channel as ISocketMessageChannel).GetMessageAsync(messageSettings.MessageToMonitor) as IUserMessage;

				foreach (var reaction in message.Reactions)
				{
					if (messageSettings.ReactionsToRoles.TryGetValue(reaction.Key.Name, out GrantingRoleSettings grantingSettings))
					{
						var reactedUsers = message.GetReactionUsersAsync(reaction.Key, 1000);

						var flattenedUsers = await AsyncEnumerableExtensions.FlattenAsync(reactedUsers);

						foreach (var reactedUser in flattenedUsers)
						{
							if (client.CurrentUser.Id == reactedUser.Id)
							{
								continue;
							}

							await GrantUserRoleBasedOnReaction(reaction.Key, message, guild, guildSettings, guild.GetUser(reactedUser.Id));
						}
					}
				}

				await message.RemoveAllReactionsAsync();

				Emoji[] emojis = messageSettings.ReactionsToRoles.Select(item => new Emoji(item.Key)).ToArray();

				await message.AddReactionsAsync(emojis);
			}
		}

		async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
		{
			try
			{
				if (client.CurrentUser.Id == reaction.UserId)
				{
					return; //Handling own user
				}

				Console.WriteLine($"{reaction.UserId} reacted with: {reaction.Emote.Name} to message: {reaction.MessageId} in channel: {reaction.Channel}");

				IUserMessage message = cachedMessage.Value;

				SocketGuild guild = (channel as SocketGuildChannel).Guild;

				ulong guildId = guild.Id;

				if (!settings.TryGetValue(guildId, out TownGuild guildSettings))
				{
					return; //different guild 
				}

				ReactionBasedRoleGrantingSettings grantingSettings = guildSettings.RoleGrantingSettings;

				if (!grantingSettings.MessagesMap.TryGetValue(cachedMessage.Id, out MessageSettings messageSettings))
				{
					return; //different message
				}

				if (!messageSettings.ReactionsToRoles.TryGetValue(reaction.Emote.Name, out GrantingRoleSettings roleSettings))
				{
					if (message == null)
					{
						message = await cachedMessage.DownloadAsync();
					}

					IUser user = GetReactionUser(reaction);

					await message.RemoveReactionAsync(reaction.Emote, user);

					return; //wrong react
				}

				if (message == null)
				{
					message = await cachedMessage.DownloadAsync();
				}

				IGuildUser guildUser = guild.GetUser(reaction.UserId);

				if (guildUser.RoleIds.Any(r => r == roleSettings.RoleToGrant))
				{
					if (guildSettings.RoleGrantingSettings.RemoveReactOnSuccess)
					{
						await message.RemoveReactionAsync(reaction.Emote, guildUser);
					}

					return; //already have role
				}

				await GrantUserRoleBasedOnReaction(reaction.Emote, message, guild, guildSettings, guildUser);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine("Failed handling emote on message");
			}
		}

		async Task GrantUserRoleBasedOnReaction(IEmote emote, IUserMessage message, SocketGuild guild, TownGuild guildSettings, IGuildUser guildUser)
		{
			GrantingRoleSettings roleSettings = guildSettings.RoleGrantingSettings.MessagesMap[message.Id].ReactionsToRoles[emote.Name];

			SocketRole role = guild.GetRole(roleSettings.RoleToGrant);

			Console.WriteLine($"{guildUser} reacted with: {emote.Name} and is being given role: " + role);

			await guildUser.AddRoleAsync(role);

			if (guildSettings.RoleGrantingSettings.RemoveReactOnSuccess)
			{
				await message.RemoveReactionAsync(emote, guildUser);
			}

			string welcomeMessage = guildSettings.FormatMessage(roleSettings.GrantedMessage, guildUser, client);

			await (guild.GetChannel(roleSettings.MessageChannel) as SocketTextChannel).SendMessageAsync(welcomeMessage);

			if (roleSettings.DirectMessageOnGrant)
			{
				string directMessage = guildSettings.FormatMessage(roleSettings.DirectMessage, guildUser, client);

				await guildUser.SendMessageAsync(roleSettings.DirectMessage);
			}
		}

		IUser GetReactionUser(SocketReaction reaction)
		{
			IUser user = reaction.User.GetValueOrDefault();

			if (!reaction.User.IsSpecified)
			{
				user = client.GetUser(reaction.UserId);
			}

			return user;
		}
	}
}
