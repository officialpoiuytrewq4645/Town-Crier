using Discord.WebSocket;
using Octokit;
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

		// I plan on actually requesting the data and amount of commits but heres a temporary version
	    async Task Contributors(SocketMessage message)
		{
			var github = new GitHubClient(new ProductHeaderValue("Town-Crier"));
			var user = await github.User.Get("Narmdo");
			Console.WriteLine(user.Followers + " folks love the half ogre!");
		}
	}
}
