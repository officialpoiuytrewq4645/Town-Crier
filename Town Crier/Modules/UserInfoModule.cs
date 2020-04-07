using Alta.WebApi.Models.DTOs.Responses;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System.Threading.Tasks;
using TownCrier.Services;

namespace TownCrier
{
	[Group("info")]
	public class UserInfoModule : InteractiveBase<SocketCommandContext>
	{
		public AltaAPI AltaApi { get; set; }

		[RequireUserPermission(GuildPermission.Administrator)]
		[Command("discord")]
		public async Task GetDiscordInfo(IGuildUser user)
		{
			string message = $@"Name: {user.Nickname}
ID: {user.Id}
Created At: {user.CreatedAt}
Joined At: {user.JoinedAt}";

			await ReplyAsync(message);
		}

		[RequireUserPermission(GuildPermission.Administrator)]
		[Command("alta")]
		public async Task GetAltaInfo(string userString)
		{
			PersonalUserInfoResponse user;

			if (!int.TryParse(userString, out int userId))
			{
				var temp = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(userString);

				userId = temp.Identifier;
			}

			user = await AltaApi.ApiClient.UserClient.GetPersonalUserInfo(userId);

			string message = $@"Username: {user.Username}
ID: {user.Identifier}
Email: {user.Email}";

			await ReplyAsync(message);
		}
	}
}