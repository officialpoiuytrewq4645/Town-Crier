using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Town_Crier.Services
{
    class ContributorsList
    {
		DiscordSocketClient discord;

		public ContributorsList(DiscordSocketClient discord)
		{
			this.discord = discord;

			discord.MessageReceived += Contributors;
		}

		// Contributors, add your name to this list.

		async Task Contributors(SocketMessage message)
		{
			if (message.Content.ToLower().Contains("!contributors"))
			{			
				await message.Channel.SendMessageAsync("These are the people who helped make Town Crier! " +
					"Narmdo (Joel_Alta), " +
					"Timo_Alta, " +
					"Mattssu, " +
					"Klives, " +
					"yoshisman8, " +
					"Snoofo, " +
					"officialpoiuytrewq4645. ");
			}
		}
	}
}
