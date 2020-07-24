using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Town_Crier.Services
{
    class AmIRight
    {
		DiscordSocketClient discord;

		public AmIRight(DiscordSocketClient discord)
		{
			this.discord = discord;

			discord.MessageReceived += AmIRightTask;
		}

		async Task AmIRightTask(SocketMessage message)
        {
			if (message.Content.ToLower().Contains("am i right town crier") || message.Content.ToLower().Contains("am i right tc") && message.Content.Contains("?"))
			{
				// 50% chance for yes or no
				if (new Random().Next(0, 10) > 5)
				{
					await message.Channel.SendMessageAsync(message.Author.Mention + " - Yes.");
				}
				else
				{
					await message.Channel.SendMessageAsync(message.Author.Mention + " - No.");
				}
			}
		}
	}
}
