using Alta.WebApi.Models;
using Alta.WebApi.Models.DTOs.Responses;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TableParser;
using TownCrier.Services;

namespace TownCrier.Modules
{
	[Group("servers"), Alias("s", "server")]
	public class ServersModule : InteractiveBase<SocketCommandContext>
	{
		public AltaAPI AltaApi { get; set; }

		public enum Map
		{
			Town,
			Tutorial,
			TestZone
		}

		static readonly string[] OnlineTableHeaders = new string[] { "Name", "Type", "Players" };

		[Command("t"), Alias("table")]
		public async Task Table()
		{
			IEnumerable<GameServerInfo> servers = await AltaApi.ApiClient.ServerClient.GetOnlineServersAsync();

			servers = servers.OrderBy(item => item.Name);

			string result = servers.ToStringTable(OnlineTableHeaders, server => server.Name, server => (Map)server.SceneIndex, server => server.OnlinePlayers.Count());

			int total = servers.Sum(item => item.OnlinePlayers.Count());

			await ReplyAsync("```" + result + $"\nTotal Players: {total}```");
		}

		[Command()]
		public async Task Online()
		{
			IEnumerable<GameServerInfo> servers = await AltaApi.ApiClient.ServerClient.GetOnlineServersAsync();

			servers = servers.OrderBy(item => item.Name);

			EmbedBuilder builder = new EmbedBuilder();
			
			foreach (GameServerInfo info in servers)
			{
				builder.AddField(info.Name, info.OnlinePlayers.Count(), true);
			}

			int total = servers.Sum(item => item.OnlinePlayers.Count());

			builder.AddField("Total Players", total, false);

			await ReplyAsync("", embed: builder.Build());
		}

		[Command("info"), Alias("player", "p", "i", "players")]
		public async Task Players([Remainder]string serverName)
		{
			serverName = serverName.ToLower();

			IEnumerable<GameServerInfo> servers = await AltaApi.ApiClient.ServerClient.GetOnlineServersAsync();

			StringBuilder response = new StringBuilder();

			response.AppendLine("Did you mean one of these?");

			foreach (GameServerInfo server in servers)
			{
				response.AppendLine(server.Name);

				if (Regex.Match(server.Name, @"\b" + serverName + @"\b", RegexOptions.IgnoreCase).Success)
				{
					SocketGuildUser guildUser = Context.User as SocketGuildUser;

					if ((guildUser == null || !guildUser.GuildPermissions.ManageChannels) && Regex.Match(server.Name, "pvp", RegexOptions.IgnoreCase).Success)
					{
						await ReplyAsync("PvP Player List is disabled");
						return;
					}

					response.Clear();
					
					EmbedBuilder tempBuilder = new EmbedBuilder();

					tempBuilder.AddField("Name", server.Name);
					tempBuilder.AddField("Type", (Map)server.SceneIndex);
					tempBuilder.AddField("Players", server.OnlinePlayers.Count());
					
					foreach (UserInfo user in server.OnlinePlayers)
					{
						MembershipStatusResponse membershipResponse = await AltaApi.ApiClient.UserClient.GetMembershipStatus(user.Identifier);

						response.AppendFormat("- {1}{0}\n", user.Username, membershipResponse.IsMember ? "<:Supporter:547252984481054733> " : "");
					}

					tempBuilder.WithDescription(response.ToString());
					await ReplyAsync("", embed: tempBuilder.Build());

					return;
				}
			}

			await ReplyAsync(response.ToString());
		}
	}
}