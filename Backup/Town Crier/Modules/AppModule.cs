using System.Threading.Tasks;
using Discord.Commands;
using System;
using Discord.Addons.Interactive;

namespace DiscordBot.Modules
{
	[RequireUserPermission(Discord.GuildPermission.Administrator)]
	[Group("app")]
	public class AppModule : InteractiveBase<SocketCommandContext>
	{
		[Summary("Restart Town Crier")]
		[Command("quit")]
		public async Task Quit()
		{
			Console.WriteLine("Quitting Town Crier Application");

			Environment.Exit(0);
		}
	}
}
