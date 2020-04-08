using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Timers;

namespace TownCrier.Services
{
	public class RoutineAnnouncementService
	{
		Timer Clock { get; set; } = new Timer();
		
		DiscordSocketClient discord;
		SocketTextChannel channel;

		Random random = new Random();

		int categoryIndex = 0;

		string[][] announcements =
		{
			new string[]
			{
				"Have feedback for the game?\nCheck out https://feedback.townshiptale.com !",
				"Looking for a wiki?\nCheck out https://townshiptale.gamepedia.com !",
				"Can't play right now but want to check out the game?\nVisit https://youtube.com/c/townshiptale !",
			},

			new string[]
			{
				"Did you know Supporters get credit ***every month*** to spend in the store!?\nCheck out https://townshiptale.com/supporter for more information!",
				"Why does the store have two prices for everything? Well, it turns out Supporters get a discount on everything there! \nCheck out https://townshiptale.com/supporter for more information!",
				"Keen to see A Township Tale grow? Become a Supporter! \nCheck out https://townshiptale.com/supporter for more information!",
			},

			new string[]
			{
				"Hey there folk! Hope you're all well. Just dropping in to make sure you're all behaving yourself!",
				"^ This person is awesome ^",
				"Did I see a newcomer!? Welcome all new people!",
				"Today I realized, as a bot, I don't have eyes, and thus can't play A Township Tale. Why am I even here?",
				"v Next person is awesome v",
				"Sometimes I feel like I'm just saying things because I've been told to do so.",
				"<Insert Hourly Announcement Here>",
			},

			new string[]
			{
				"Keen to contribute to the wiki, own a server, or be involved in other 'meta' A Township Tale activities?\nJoin the ATT Meta Discord at <https://discord.gg/GNpmEN2> !",
				"Have you joined A Township Tale's subreddit? ?\nI've heard there's some cool stuff there!\n https://www.reddit.com/r/TownshipTale !",
				"Did you know A Township Tale has a Youtube channel?\nCheck out https://youtube.com/c/townshiptale !",
			},

			new string[]
			{
				"Want to support the devs? Become a Supporter! \nCheck out https://townshiptale.com/supporter for more information!",
				"SPOILER ALERT! Supporters get access to a top secret #supporter-spoilers channel. \nCheck out https://townshiptale.com/supporter for more information!",
				"Sneaky! Rumour has it, Supporters have access to preview servers. \nCheck out https://townshiptale.com/supporter for more information!",
			},
		};

		public RoutineAnnouncementService(DiscordSocketClient discord)
		{
			this.discord = discord;
			
			Clock.Interval = 60 * 60 * 1000;
			Clock.AutoReset = true;
			Clock.Elapsed += Announce;
			Clock.Start();
		}

		async void Announce(object sender, ElapsedEventArgs e)
		{
			if (channel == null)
			{
				channel = discord.GetChannel(334933825383563266) as SocketTextChannel;


				if (channel == null)
				{
					Console.WriteLine("Couldn't find routine announcement channel!");
					return;
				}
			}

			string[] category = announcements[categoryIndex];

			string announcement = category[random.Next(category.Length)];

			await channel.SendMessageAsync(announcement);

			categoryIndex = (categoryIndex + 1) % announcements.Length;
		}

	}
}
