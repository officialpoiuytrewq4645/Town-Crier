using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TownCrier.Services
{
	public class AltaProtocolService
	{
		DiscordSocketClient discord;

		public AltaProtocolService(DiscordSocketClient discord)
		{
			this.discord = discord;

			discord.MessageReceived += Handle;
		}

		async Task Handle(SocketMessage message)
		{
			//Match match = Regex.Match(message.Content, @"alta://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

			//if (match.Success)
			//{
			//	EmbedBuilder embed = new EmbedBuilder();

			//	embed.Title = match.Value;
			//	embed.Description = $"<{match.Value}>";

			//	await message.Channel.SendMessageAsync(embed: embed.Build());
			//}
		}
	}
}
