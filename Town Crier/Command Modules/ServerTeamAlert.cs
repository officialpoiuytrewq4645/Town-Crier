using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace TownCrier.Features
{
	static class ServerTeamAlert
	{
		public static async Task Process(SocketMessage message)
		{
			var channel = message.Channel as ITextChannel;

			if (channel != null && channel.Id != 560633017517867019)
			{
				if (message.MentionedRoles.Count > 0 && message.MentionedRoles.Contains(channel.Guild.GetRole(560631812876009472)))
				{
					ITextChannel text = await channel.Guild.GetTextChannelAsync(560633017517867019);

					IUserMessage response = await text.SendMessageAsync(message.Author.Mention + " in " + channel.Mention + ": " + message.Content);

					await response.AddReactionAsync(new Emoji("✔"));
				}
			}
		}
	}
}
