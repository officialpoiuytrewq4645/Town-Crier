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
		Random random;

		public AmIRight(DiscordSocketClient discord)
		{
			this.discord = discord;
			
			random = new Random();

			discord.MessageReceived += AmIRightTask;
		}

		async Task AmIRightTask(SocketMessage message)
        {
			string lowercase = message.Content.ToLower();
			
			if ((lowercase.Contains("am i right town crier") || lowercase.Contains("am i right tc")) && lowercase.Contains("?"))
			{
				// 50% chance for yes or no
				if (random.Next(0, 2) == 1)
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
