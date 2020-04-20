# Town-Crier
A Township Tale's official Discord bot, written with Discord.NET

Join https://discord.gg/townshiptale to see Town Crier in action!
Join https://discord.gg/GNpmEN2 to discuss contributing to the bot.

This is still early days, so still working out the kinks of how this will work on GitHub.

This project is a .NET Core 2.1 Project. As such it can be compiled to any platform that .NET Core supports. 

Who to talk to about stuff:
- Joel_Alta or Timo_Alta in either Discord linked above

There is a setup guide at the bottom of this readme.

Things that need some serious work:
- 	Program.cs - *throws up*, this whole project is what happens when someone hacks in things with very little thought as to where/how things are arranged.
- 	Right now ChatCraft's config00 file contains all game configuration (locations, items, etc.) as well as every 'player'. This means that the file is stupidly big on large severs, and the whole thing is loaded into RAM on startup.
- 	ChatCraft's player profile also has non-chatcraft related information, such as join date etc.
- 	Ideally 'player profiles' are moved to some form of database system.
- 	Ideally the game also isn't one huge file, but instead broken down in some way, to potentially allow for easier contribution of sets of items, locations, etc.

Hurdles that we need to work out:
-	The project relies on three internal projects (called WebApiClient, WebApiModels, and AltaLogging).
	These we have hooked up through Nuget to our private repository. I've included the DLL's in the repo manually.
	
Some other things to be vaguely aware of:
-	Chatty Township is half way through a rewrite, and the first version wasn't even completed.... So a lot of mess there
	Anything with !tc is semi-legacy and getting replaced
-	There's some automatic JIRA reporting code in there. It's not used, as I didn't have time to work it out.
		
Random other information:
-	`reporter.json` goes somewhere if you want to look into that JIRA feature mentioned above.
	Content should be something like the following:

```json
{ 
  "AllowedRolesIDs": [ 
    416788657673076737, 
    334938548535033857 
  ], 
  "Version": "0.0.2.3", 
  "ServerID": 0, 
  "Username": "<email>", 
  "Password": "<password>", 
  "JiraUrl": "<jira URL>", 
  "JiraProject": "<jira project>", 
  "BugIssueType": "1", 
  "UserStory": "7", 
  "CustomFieldId": "0" 
}
```

**Building**
As this is now a .NET Core project you will need to publish and executable for the runtime you want. You can do this by running 
```
dotnet publish -c Release -r win10-x64 #windows build
dotnet publish -c Release -r ubuntu.16.10-x64 #linux build
```
in the CMD line in the project folder. You can still run it normally inside visual studio.


**Setup Guide:**
1. Create a file called `config.json` next to the executable. 
2. Fill out the file with the following info:
```json
{
	"status": "<STATUS THAT WILL APPEAR IN DISCORD>",
	"timerInterval" : "<INTERVAL IN MS TO UPDATE VARIOUS THINGS (300000 is a good default)>" 
}
```
3. Setup the Environment Variables

| Variable | Description | Required |
| --- | --- | --- |
| DISCORD_BOT_TOKEN | Your discord bot token, see here for more info https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token | YES |
| USE_ENV_ALTA_LOGIN | Set to true if you want to use the environement vars to provide the Alta login credentials else it will still use the account.txt | Only if NOT using account.txt |
| ALTA_USERNAME | Your Alta username, only used if the above is true | Only if NOT using account.txt |
| ALTA_PASSWORD | Your Alta password | Only if NOT using account.txt |
| TC_ACCESS_KEY | If present TC will connect to DynamoDB to store the users information. Leave out to use the default LiteDB. AWS Access Key | NO, only if using DDB |
| TC_SECRET_KEY | AWS Secret Key, see above for more details | NO |
| TWITCH_CLIENT_ID | Used to mark players who are streaming the game on Twitch | NO |

You will need to set these in visual studio when you're testing your bot as well as in the environment you're deploying in. 

If using the account.txt to provide your Alta login credentials.
Create file called `account.txt` next to the executable and then fill out the file with the following info
```
<ALTA USERNAME>|<ALTA PASSWORD>
```

4. Run the bot and type in `@YOUR_BOT guild init` and follow the prompts