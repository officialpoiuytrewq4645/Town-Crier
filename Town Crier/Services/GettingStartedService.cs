using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace TownCrier.Services
{
    public class GettingStartedService
	{
		DiscordSocketClient discord;

		public GettingStartedService(DiscordSocketClient discord)
		{
			this.discord = discord;

			//discord.MessageReceived += DealWith;
		}

		async Task DealWith(SocketMessage message)
		{
			if (!(message.Channel is ITextChannel channel))
			{
				return;
			}
			var chnl = message.Channel as SocketGuildChannel;
			var msg = message.Content.ToLower();
			if ((message.Content.Contains("download") 
				| (msg.Contains("game") | msg.Contains("township") | msg.Contains("att") | msg.Contains("alpha") | msg.Contains("beta") && msg.Contains("get") | msg.Contains("download") | msg.Contains("how") )
				| (msg.Contains("where") | msg.Contains("look") && msg.Contains("game") | msg.Contains("download") | msg.Contains("get") | msg.Contains("att") | msg.Contains("township") | msg.Contains("alpha") | msg.Contains("beta"))
				)
				&& (DateTime.UtcNow - chnl.Guild.GetUser(message.Author.Id).JoinedAt.Value.UtcDateTime > TimeSpan.FromMinutes(60)) 
				&& !message.Author.IsBot)
			{
				await message.Channel.SendMessageAsync("To download A Township Tale check out <#450499963999223829>");
			}
				

		}
	}
}
