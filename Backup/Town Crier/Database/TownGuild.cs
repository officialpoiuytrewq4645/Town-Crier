using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using Discord;
using Discord.WebSocket;
using Amazon.DynamoDBv2.DataModel;

namespace TownCrier.Database
{
	public class ReactionBasedRoleGrantingSettings
	{
		[DynamoDBProperty("message_settings")]
		public HashSet<MessageSettings> MessageSettings { get; set; }

		[DynamoDBProperty("remove_reacts_on_success")]
		public bool RemoveReactOnSuccess { get; set; } = true;

		[DynamoDBIgnore]
		public Dictionary<ulong, MessageSettings> MessagesMap
		{
			get
			{
				if (messagesMap == null)
				{
					messagesMap = MessageSettings.ToDictionary(item => item.MessageToMonitor);
				}

				return messagesMap;
			}
		}

		Dictionary<ulong, MessageSettings> messagesMap;
	}

	public class MessageSettings
	{
		[DynamoDBProperty("message_to_monitor")]
		public ulong MessageToMonitor { get; set; }

		[DynamoDBProperty("channel")]
		public ulong Channel { get; set; }

		[DynamoDBProperty("reactions_to_roles")]
		public Dictionary<string, GrantingRoleSettings> ReactionsToRoles { get; set; }
	}

	public class GrantingRoleSettings
	{
		[DynamoDBProperty("role_to_grant")]
		public ulong RoleToGrant { get; set; }

		[DynamoDBProperty("granted_message")]
		public string GrantedMessage { get; set; }

		[DynamoDBProperty("messaging_channel")]
		public ulong MessageChannel { get; set; }

		[DynamoDBProperty("dm_on_grant")]
		public bool DirectMessageOnGrant { get; set; }

		[DynamoDBProperty("dm_message")]
		public string DirectMessage { get; set; }
	}

	[DynamoDBTable("TC_Guilds")]
	public class TownGuild
	{
		/// <summary>
		/// Guild's Discord ID
		/// </summary>
		[BsonId]
		[DynamoDBHashKey("id")]
		public ulong GuildId { get; set; }

		/// <summary>
		/// Guild's Prefix
		/// </summary>
		[DynamoDBProperty("prefix")]
		public string Prefix { get; set; } = "!";

		/// <summary>
		/// These roles are auto-assigned to users based on their activity.
		/// </summary>
		[DynamoDBProperty("activity_roles")]
		public List<ActivityRole> ActivityRoles { get; set; } = new List<ActivityRole>();

		/// <summary>
		/// These channels are pinged when the role is mentioned
		/// </summary>
		[DynamoDBProperty("cross_alerts")]
		public List<CrossAlert> CrossAlerts { get; set; } = new List<CrossAlert>();

		/// <summary>
		/// These filters only allow particular content in channels
		/// </summary>
		[DynamoDBProperty("channel_filter")]
		public List<ChannelFilter> ChannelFilters { get; set; } = new List<ChannelFilter>();

		/// <summary>
		/// This role is assinged to Alta supporters.
		/// </summary>
		[DynamoDBProperty("supporter_role")]
		public SocketRole SupporterRole { get; set; } = null;

		/// <summary>
		/// This role is labeled as the Admin role, assignable by the server owner.
		/// Allows bypassing of several permission checks
		/// </summary>
		[DynamoDBProperty("admin_role")]
		public ulong AdminRole { get; set; } = 0;

		/// <summary>
		/// Channel assigned for public-level notifications.
		/// </summary>
		[DynamoDBProperty("notification_channel")]
		public ulong NotificationChannel { get; set; } = 0;

		/// <summary>
		/// Channel assigned for leaving notifications
		/// </summary>
		[DynamoDBProperty("leaver_channel")]
		public ulong LeaverChannel { get; set; } = 0;

		/// <summary>
		/// A dictionary with some server settings:
		/// <list type="bullet">
		/// <item>
		/// <description> ModuleName: Whether or not this module is enabled/disabled.</description>
		/// </item>
		/// </list>
		/// </summary>
		[DynamoDBProperty("settings")]
		public Dictionary<string, bool> Settings { get; set; } = new Dictionary<string, bool>();

		/// <summary>
		/// Message to be displayed when a user joins.
		/// <list type="bullet">
		/// <item>
		/// <description><para>"{user}" is replaced with a mention of the user who joined.</para></description>
		/// </item>
		/// <item>
		/// <description><para>"{server}" is replaced with the server's name.</para></description>
		/// </item>
		/// <item>
		/// <description><para>"{server:count}" is replaced with the amount of users in the server.</para></description>
		/// </item>
		/// <item>
		/// <description><para>"{staff}" is replaced with a mention of the staff role (if it exists).</para></description>
		/// </item>
		/// </list>
		/// </summary>
		[DynamoDBProperty("welcome_message")]
		public string WelcomeMessage { get; set; } = "";

		/// <summary>
		/// Message to be displayed once the server usercount reaches a certain number defined by the Milestone variable
		/// </summary>
		/// <see cref="WelcomeMessage"/>
		[DynamoDBProperty("milestone_message")]
		public string MilestoneMessage { get; set; } = "";

		/// <summary>
		/// Indicates how often should the server anounce a new milestone in server user count.
		/// </summary>
		[DynamoDBProperty("milestone_marker")]
		public int MilestoneMarker { get; set; } = 1000;

		[DynamoDBProperty("wiki_name")]
		public string WikiName { get; set; } //A Township Tale Wiki

		[DynamoDBProperty("wiki_url")]
		public string WikiUrl { get; set; } //https://townshiptale.gamepedia.com

		[DynamoDBProperty("wiki_icon")]
		public string WikiIcon { get; set; } //https://d1u5p3l4wpay3k.cloudfront.net/atownshiptale_gamepedia_en/9/9e/WikiOnly.png

		[DynamoDBProperty("role_granting_settings")]
		public ReactionBasedRoleGrantingSettings RoleGrantingSettings { get; set; }

		public string ParseMessage(IGuildUser user, DiscordSocketClient client)
		{
			return FormatMessage(WelcomeMessage, user, client);
		}

		public string FormatMessage(string message, IGuildUser user, DiscordSocketClient client)
		{
			string returnstring = message.Replace("{user}", user.Mention).Replace("{server}", client.GetGuild(GuildId).Name).Replace("{server:count}", client.GetGuild(GuildId).Users.Count.ToString());

			returnstring = AdminRole != 0 ? returnstring.Replace("{admin}", user.Guild.GetRole(AdminRole)?.Mention) : returnstring;

			return returnstring;
		}
	}

	public class ActivityRole
	{
		[DynamoDBProperty("activity_type")]
		public ActivityFlag ActivityType { get; set; }

		[DynamoDBProperty("activity_name")]
		public string ActivityName { get; set; }

		[DynamoDBProperty("associated_role")]
		public ulong AssociatedRole { get; set; }
	}

	public class CrossAlert
	{
		[DynamoDBProperty("channel")]
		public ulong Channel { get; set; }

		[DynamoDBProperty("role")]
		public ulong Role { get; set; }
	}

	public class ChannelFilter
	{
		public enum FilterType
		{
			Heading,
			Image
		}

		[DynamoDBProperty("channel")]
		public ulong Channel { get; set; }

		[DynamoDBProperty("alert_channel")]
		public ulong AlertChannel { get; set; }

		[DynamoDBProperty("type")]
		public FilterType Type { get; set; }
	}
}
