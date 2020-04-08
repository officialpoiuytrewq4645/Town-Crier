using Alta.WebApi.Models;
using Alta.WebApi.Models.DTOs.Responses;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using TownCrier.Services;

namespace TownCrier
{
	[RequireContext(ContextType.Guild)]
	[Group("info")]
	public class UserInfoModule : InteractiveBase<SocketCommandContext>
	{
		const long AltaGuild = 669745996204343297;

		public AltaAPI AltaApi { get; set; }

		[RequireUserPermission(GuildPermission.ManageChannels)]
		[Command("discord")]
		public async Task GetDiscordInfo(IGuildUser user)
		{
			if (Context.Guild.Id != AltaGuild)
			{
				return;
			}

			string message = $@"Name: {user.Nickname}
ID: {user.Id}
Created At: {user.CreatedAt}
Joined At: {user.JoinedAt}";

			await ReplyAsync(message);
		}

		[RequireUserPermission(GuildPermission.ManageChannels)]
		[Command("alta")]
		public async Task GetAltaInfo(string userString)
		{
			if (Context.Guild.Id != AltaGuild)
			{
				return;
			}

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

            try
            {
                LinkedAccountInfo accountInfo = await AltaApi.ApiClient.Account.GetLinkedDiscordAccount(user.Identifier);

                var discordUser = Context.Client.GetUser(accountInfo.Identifier);

                message += $@"Discord Username: {discordUser.Username}
Discord ID: {discordUser.Id}";
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

			await ReplyAsync(message);
		}
	}
}