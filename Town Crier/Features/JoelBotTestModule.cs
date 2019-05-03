using System.Data;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Database;

namespace DiscordBot
{
	[Group("joel")]
	public class JoelBotTestModule: CrierModuleBase
	{
		public JoelBotTestModule(DatabaseAccess database) : base(database)
		{

		}

		[Command("isadmin")]
		public async Task Admin()
		{
			User user = await GetUser();
			
			await ReplyAsync(user.IsAdmin ? $"You're an admin!" : "You're not an admin.");
		}

		[Command("addadmin")]
		public async Task MakeAdmin(IUser target)
		{
			User user = await GetUser();

			if (!user.IsAdmin)
			{
				await ReplyAsync("You must be an admin to add an admin!");
				return;
			}

			User targetUser = await GetUser(target);

			if (targetUser.IsAdmin)
			{
				await ReplyAsync("They are already an admin!");
				return;
			}

			targetUser.IsAdmin = true;
			await targetUser.Save();

			await ReplyAsync($"{targetUser.Username} is now an admin!");
		}

		[Command("removeadmin")]
		public async Task RemoveAdmin(IUser target)
		{
			User user = await GetUser();

			if (!user.IsAdmin)
			{
				await ReplyAsync("You must be an admin to remove an admin!");
				return;
			}

			User targetUser = await GetUser(target);

			if (!targetUser.IsAdmin)
			{
				await ReplyAsync("They are already not an admin!");
				return;
			}

			targetUser.IsAdmin = false;
			await targetUser.Save();

			await ReplyAsync($"{targetUser.Username} is no longer an admin!");
		}

		[Command("coins")]
		public async Task Coins()
		{
			User user = await GetUser();

			await ReplyAsync($"You have {user.Coins} coins!");
		}

		[Command("grab")]
		public async Task Grab()
		{
			User user = await GetUser();

			user.Coins += 1;

			await user.Save();

			await ReplyAsync($"You have 1 more coin!");
		}
	}
}