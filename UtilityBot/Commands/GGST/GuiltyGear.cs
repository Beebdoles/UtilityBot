using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static UtilityBot.Commands.GGST.Structs;
using System.IO;

namespace UtilityBot.Commands.GGST
{
    public class GuiltyGear : BaseCommandModule
    {
        private bool isRead = false;
        private bool isMatchOngoing = false;
        private String player1;
        private String player2;

        private List<string> activePool = new List<string>();                                       //use this for updating while program is running
        private List<string> people = new List<string>();                                           //use this as final storage
        private List<Structs.Player> playerInfoContainer = new List<Structs.Player>();
        private List<string> duelMessages = new List<string>();
        private List<Structs.PlayerMatches> playerMatches = new List<Structs.PlayerMatches>();


        [Command("Duel")]
        [DSharpPlus.CommandsNext.Attributes.Description("Starts a duel by randomly choosing people from the active pool")]
        public async Task Duel(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("optional: add \"-r\" for random characters")] string optional = "")
        {
            if (!isRead)
            {
                startUp();
            }

            DiscordEmbedBuilder dmb = new DiscordEmbedBuilder();
            dmb.Title = "Wheel of Misfortune";

            int activecount = 0;
            int totalcount = 0;
            foreach (Structs.Player p in playerInfoContainer)
            {
                if (p.willFight)
                {
                    ++activecount;
                }
                ++totalcount;
            }

            if (activecount > 1)
            {
                isMatchOngoing = false;
                Random rd = new Random();

                int a = rd.Next(0, totalcount);

                while (!playerInfoContainer[a].willFight)
                {
                    a = rd.Next(0, totalcount);
                }

                int b = a;

                while (!playerInfoContainer[b].willFight || playerInfoContainer[a] == playerInfoContainer[b])
                {
                    b = rd.Next(0, totalcount);
                }

                dmb.AddField("Poor souls: ", string.Format(duelMessages[rd.Next(0, duelMessages.Count)], activePool[a], activePool[b]) + "\nPlayer 1: " + activePool[a] + " Player 2: " + activePool[b]);
                //await ctx.RespondAsync(string.Format(duelMessages[rd.Next(0, duelMessages.Count)], activePool[a], activePool[b]) + "\nPlayer 1: " + activePool[a] + " Player 2: " + activePool[b]);

                if (optional.Equals("-r"))
                {
                    Random rd2 = new Random();
                    string choicea = "", choiceb = "";
                    for (int i = 0; i < playerInfoContainer.Count; ++i)
                    {
                        if (playerInfoContainer[i].playerName.Equals(activePool[a]))
                        {
                            choicea = playerInfoContainer[i].characters[rd2.Next(0, playerInfoContainer[i].characters.Count)];
                            if (playerInfoContainer[i].randomize == false)
                            {
                                choicea = playerInfoContainer[i].defaultCharacter;
                            }
                        }
                        if (playerInfoContainer[i].playerName.Equals(activePool[b]))
                        {
                            choiceb = playerInfoContainer[i].characters[rd2.Next(0, playerInfoContainer[i].characters.Count)];
                            if (playerInfoContainer[i].randomize == false)
                            {
                                choiceb = playerInfoContainer[i].defaultCharacter;
                            }
                        }
                    }
                    dmb.AddField("Chosen characters: ", activePool[a] + " will play: " + choicea + ", " + activePool[b] + " will play: " + choiceb, false);
                }

                isMatchOngoing = true;
                player1 = activePool[a];
                player2 = activePool[b];

                dmb.Color = DiscordColor.Green;
                dmb.Title = ">duel";

                await ctx.Channel.SendMessageAsync(dmb.Build());
            }
            else
            {
                List<(string, string)> list = new List<(string, string)>{("Not enough people to create a duel", "add more people to active pool")};
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("duel", true, list));
            }
        }

        [Command("End")]
        [DSharpPlus.CommandsNext.Attributes.Description("Ends an ongoing match")]
        public async Task EndMatch(CommandContext ctx)
        {
            if (!isRead)
            {
                startUp();
            }
            if (isMatchOngoing)
            {
                isMatchOngoing = false;
                await ctx.RespondAsync("Ended the ongoing match");
            }
            else
            {
                await ctx.RespondAsync("There is no ongoing match. No changes were made");
            }
        }

        [Command("saveMatch")]
        [DSharpPlus.CommandsNext.Attributes.Description("Saves the current match, or add a new one through [OVERRIDE]")]
        public async Task matchdataEntry(
            CommandContext ctx,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 1's character")] String character1,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 2's character")] String character2,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 1's score")] int score1,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 2's score")] int score2,
            [DSharpPlus.CommandsNext.Attributes.Description("[OVERRIDE] Player1's name")] String optionalPlayer1 = null,
            [DSharpPlus.CommandsNext.Attributes.Description("[OVERRIDE Player2's name")] String optionalPlayer2 = null,
            [DSharpPlus.CommandsNext.Attributes.Description("[OVERRIDE] whether or not a match override should be executed")] Boolean matchOverride = false)
        {
            if (!isRead)
            {
                startUp();
            }
            if (isMatchOngoing || matchOverride)
            {
                isMatchOngoing = false;
                Structs.DuelData dd = new Structs.DuelData();

                String currentid1;
                String currentid2;
                if (matchOverride)
                {
                    currentid1 = optionalPlayer1 + optionalPlayer2;
                    currentid2 = optionalPlayer2 + optionalPlayer1;
                }
                else
                {
                    currentid1 = player1 + player2;
                    currentid2 = player2 + player1;
                }
                Boolean playerMatchExists = false;
                foreach (Structs.PlayerMatches pm in playerMatches)
                {
                    String id = pm.player1 + pm.player2;

                    if (id.ToLower().Equals(currentid1.ToLower()) || id.ToLower().Equals(currentid2.ToLower()))
                    {
                        playerMatchExists = true;

                        if (score1 > score2)
                        {
                            if (pm.player1.Equals(player1) || (matchOverride && pm.player1.Equals(optionalPlayer1)))
                            {
                                pm.player1Wins += 1;

                                dd.player1Character = character1;
                                dd.player1Score = score1;
                                dd.player2Character = character2;
                                dd.player2Score = score2;
                            }
                            else
                            {
                                pm.player2Wins += 1;

                                dd.player2Character = character1;
                                dd.player2Score = score1;
                                dd.player1Character = character2;
                                dd.player1Score = score2;
                            }
                        }
                        else
                        {
                            if (pm.player2.Equals(player2) || (matchOverride && pm.player2.Equals(optionalPlayer2)))
                            {
                                pm.player2Wins += 1;

                                dd.player2Character = character2; dd.player2Score = score2; dd.player1Character = character1; dd.player1Score = score1;
                            }
                            else
                            {
                                pm.player1Wins += 1;

                                dd.player2Character = character1; dd.player2Score = score1; dd.player1Character = character2; dd.player1Score = score2;
                            }
                        }
                        pm.totalMatches += 1;
                        dd.matchNumber = pm.totalMatches;
                        pm.matches.Add(dd);
                    }
                }
                if (!playerMatchExists)
                {
                    Structs.PlayerMatches pm = new Structs.PlayerMatches();
                    if (matchOverride)
                    {
                        pm.player1 = optionalPlayer1;
                        pm.player2 = optionalPlayer2;
                    }
                    else
                    {
                        pm.player1 = player1;
                        pm.player2 = player2;
                    }
                    pm.player1Wins = 0;
                    pm.player2Wins = 0;
                    pm.totalMatches = 1;
                    pm.matches = new List<Structs.DuelData>();

                    if (score1 > score2)
                    {
                        pm.player1Wins = 1;
                    }
                    else
                    {
                        pm.player2Wins = 1;
                    }

                    dd.player1Character = character1;
                    dd.player2Character = character2;
                    dd.player1Score = score1;
                    dd.player2Score = score2;

                    dd.matchNumber = pm.totalMatches;
                    pm.matches.Add(dd);
                    playerMatches.Add(pm);
                }
                WriteJson();
                await ctx.RespondAsync("Match has been saved");
            }
            else
            {
                await ctx.RespondAsync("No match is ongoing, and no override has been stated, or insufficient optional parameters");
            }
        }

        [Command("removeMatch")]
        [DSharpPlus.CommandsNext.Attributes.Description("Removes the specified match if it exists")]
        public async Task RemoveMatch(CommandContext ctx,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 1's name")] String player1,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 2's name")] String player2,
            [DSharpPlus.CommandsNext.Attributes.Description("The match number")] int matchNum)
        {
            if (!isRead)
            {
                startUp();
            }
            String id = player1 + player2;
            Boolean found = false;
            foreach (Structs.PlayerMatches pm in playerMatches)
            {
                if (id.Equals(pm.player1 + pm.player2) || id.Equals(pm.player2 + pm.player1))
                {
                    if (matchNum <= pm.totalMatches)
                    {
                        Structs.DuelData tempDD = pm.matches[matchNum - 1];
                        if (tempDD.player1Score > tempDD.player2Score)
                        {
                            --pm.player1Wins;
                        }
                        else
                        {
                            --pm.player2Wins;
                        }
                        pm.matches.Remove(pm.matches[matchNum - 1]);
                        for (int i = matchNum - 1; i < pm.matches.Count; ++i)
                        {
                            pm.matches[i].matchNumber -= 1;
                        }
                        pm.totalMatches -= 1;
                        WriteJson();
                        await ctx.RespondAsync("Match has been removed");
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                await ctx.RespondAsync("Match was not found. Nothing has been changed");
            }
        }

        [Command("mergeMatch")]
        [DSharpPlus.CommandsNext.Attributes.Description("merge two existing matches")]
        public async Task Merge(CommandContext ctx)
        {

        }

        [Command("showMatches")]
        [DSharpPlus.CommandsNext.Attributes.Description("Display all existing player matches")]
        public async Task ShowMatches(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("[optional] which page to view")] int optionalPage = 1)
        {
            Console.WriteLine("hi");
            if (!isRead)
            {
                startUp();
            }
            ReadJson();

            if (optionalPage > Convert.ToInt32(Math.Ceiling((double)playerMatches.Count / 5)))
            {
                await ctx.RespondAsync("Page does not exist");
            }
            else
            {
                try
                {
                    DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

                    builder.Color = new DiscordColor(156, 11, 11);

                    for (int i = (optionalPage - 1) * 5; i < optionalPage * 5; ++i)
                    {
                        if (i < playerMatches.Count)
                        {
                            Structs.PlayerMatches pm = playerMatches[i];
                            String player1Wins = pm.player1 + " wins: " + pm.player1Wins;
                            String player2Wins = pm.player2 + " wins: " + pm.player2Wins;
                            int spacing = Math.Abs(player1Wins.Length - player2Wins.Length);
                            StringBuilder sb = new StringBuilder();
                            for (int j = 0; j < spacing; ++j)
                            {
                                sb.Append(" ");
                            }
                            sb.Append("\t");
                            String response;
                            if (pm.totalMatches - 1 >= 0)
                            {
                                if (pm.matches[pm.totalMatches - 1].player1Score > pm.matches[pm.totalMatches - 1].player2Score)
                                {
                                    response = pm.player1 + " victory as " + pm.matches[pm.totalMatches - 1].player1Character;
                                }
                                else
                                {
                                    response = pm.player2 + " victory as " + pm.matches[pm.totalMatches - 1].player2Character;
                                }
                            }
                            else
                            {
                                response = "no duels between these two players";
                            }
                            builder.AddField
                                    (
                                        pm.player1 + " vs " + pm.player2,
                                        player1Wins + "\n" + player2Wins,
                                        true
                                    );
                            String column2Row1 = "Total duels: " + pm.totalMatches;
                            String column2Row2 = "Most recent duel: " + response;
                            StringBuilder sb0 = new StringBuilder();
                            if (column2Row1.Length > column2Row2.Length)
                            {
                                for (int j = 0; j < (column2Row1.Length) + 0.75; j++)
                                {
                                    sb0.Append("-");
                                }
                            }
                            else
                            {
                                for (int j = 0; j < (column2Row2.Length) * 0.75; j++)
                                {
                                    sb0.Append("-");
                                }
                            }
                            builder.AddField
                                (
                                    sb0.ToString(),
                                    column2Row1 + "\n" + column2Row2,
                                    true
                                );
                            builder.AddField("*", "|", true);
                        }
                    }

                    builder.Title = "List of player matches";
                    builder.WithThumbnail("https://steamuserimages-a.akamaihd.net/ugc/1852672135569818139/26EC73A2C1287846F79B1F277C134189EA0BFCB8/");

                    //builder.ImageUrl = ctx.Message.Author.AvatarUrl;
                    //builder.Url = ctx.Message.Author.AvatarUrl;
                    if (optionalPage == 1)
                    {
                        builder.Description = "First page is shown by default";
                    }
                    builder.Author = new DiscordEmbedBuilder.EmbedAuthor();
                    //builder.Author.IconUrl = ctx.Message.Author.AvatarUrl;
                    //builder.Author.Name = "username of author";
                    //builder.Author.Url = ctx.Message.Author.AvatarUrl;

                    builder.Footer = new DiscordEmbedBuilder.EmbedFooter();
                    builder.Footer.IconUrl = "https://cdn2.steamgriddb.com/icon/eb63dbf55a3f40d1a2ea77fd884abd26.png";
                    builder.Footer.Text = "Page " + optionalPage + " of " + Convert.ToInt32(Math.Ceiling((double)playerMatches.Count / 5));

                    await ctx.RespondAsync(builder.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        [Command("showMatches")]
        [DSharpPlus.CommandsNext.Attributes.Description("Display matches between two players")]
        public async Task ShowMatches(CommandContext ctx,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 1's name")] String player1,
            [DSharpPlus.CommandsNext.Attributes.Description("Player 2's name")] String player2,
            [DSharpPlus.CommandsNext.Attributes.Description("[optional] which page to view")] int optionalPage = 1)
        {
            if (!isRead)
            {
                startUp();
            }
            ReadJson();

            String id1 = player1 + player2;
            String id2 = player2 + player1;

            Boolean isFound = false;
            foreach (Structs.PlayerMatches pm in playerMatches)
            {
                if ((pm.player1 + pm.player2).Equals(id1) || (pm.player1 + pm.player2).Equals(id2))
                {
                    if (optionalPage > Convert.ToInt32(Math.Ceiling((double)pm.matches.Count / 5)))
                    {
                        isFound = true;
                        await ctx.RespondAsync("Page does not exist");
                    }
                    else
                    {
                        isFound = true;
                        DiscordEmbedBuilder build = new DiscordEmbedBuilder();

                        build.WithTitle("List of matches done by " + pm.player1 + " and " + pm.player2);
                        if (optionalPage == 1)
                        {
                            build.Description = "First page is shown by default";
                        }
                        for (int i = (optionalPage - 1) * 5; i < optionalPage * 5; ++i)
                        {
                            if (i < pm.matches.Count)
                            {
                                Structs.DuelData dd = pm.matches[i];
                                String s1;
                                String s2;
                                if (dd.player1Score > dd.player2Score)
                                {
                                    s1 = pm.player1 + " victory";
                                    s2 = " ";
                                }
                                else
                                {
                                    s1 = " ";
                                    s2 = pm.player2 + " victory";
                                }
                                StringBuilder sb = new StringBuilder();
                                if (s1.Length > s2.Length)
                                {
                                    for (int j = 0; j < s1.Length; ++j)
                                    {
                                        sb.Append("-");
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < s2.Length; ++j)
                                    {
                                        sb.Append("-");
                                    }
                                }
                                build.AddField
                                    (
                                        "Match " + dd.matchNumber,
                                        pm.player1 + " as " + dd.player1Character + ": " + dd.player1Score + "\n" + pm.player2 + " as " + dd.player2Character + ": " + dd.player2Score + " ",
                                        true
                                    );
                                build.AddField
                                    (
                                        sb.ToString(),
                                        s1 + "\n" + s2,
                                        true
                                    );
                                build.AddField
                                    (
                                        "*",
                                        "|",
                                        true
                                    );
                            }
                        }
                        build.Color = new DiscordColor(156, 11, 11);
                        build.WithThumbnail("https://www.dustloop.com/wiki/images/thumb/2/29/GGST_May_Mr._Dolphin_Horizontal.png/455px-GGST_May_Mr._Dolphin_Horizontal.png");
                        build.Footer = new DiscordEmbedBuilder.EmbedFooter();
                        build.Footer.IconUrl = "https://cdn2.steamgriddb.com/icon/eb63dbf55a3f40d1a2ea77fd884abd26.png";
                        build.Footer.Text = "Page " + optionalPage + " of " + Convert.ToInt32(Math.Ceiling((double)pm.matches.Count / 5));

                        await ctx.RespondAsync(build.Build());
                        break;
                    }
                }
            }
            if (!isFound)
            {
                await ctx.RespondAsync("Player match not found");
            }
        }

        [Command("Add")]
        [DSharpPlus.CommandsNext.Attributes.Description("Add a new person to the active pool [ADMIN ONLY]")]
        public async Task Add(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("Person to add")] string person)
        {
            List<(string, string)> list = new List<(string, string)>();

            var json = string.Empty;
            string path = @"C:\Users\Beebd\source\repos\UtilityBot\UtilityBot\config.json";

            using (var fs = System.IO.File.OpenRead(path))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            if (ctx.Member.Id == configJson.ID)
            {
                if (!isRead)
                {
                    startUp();
                }
                if (!activePool.Contains(person))
                {
                    activePool.Add(person);
                    await ctx.RespondAsync("Added " + person + " to the active pool");
                }
                else
                {
                    await ctx.RespondAsync(person + " is already in the active pool");
                }
                WriteFile();
            }
            else
            {
                list.Add(("ADMIN ONLY COMMAND", "sry, but using this without access to raw json might cause ireversible problems :("));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("add", true, list));
            }
        }

        [Command("Remove")]
        [DSharpPlus.CommandsNext.Attributes.Description("remove a person from the active pool [ADMIN ONLY]")]
        public async Task Remove(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("Person to remove")] string person)
        {
            List<(string, string)> list = new List<(string, string)>();

            var json = string.Empty;
            string path = @"C:\Users\Beebd\source\repos\UtilityBot\UtilityBot\config.json";

            using (var fs = System.IO.File.OpenRead(path))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            if (ctx.Member.Id == configJson.ID)
            {
                if (!isRead)
                {
                    startUp();
                }

                if (activePool.Remove(person))
                {
                    await ctx.RespondAsync("Removed " + person + " from the active pool");

                    foreach (Structs.Player p in playerInfoContainer)
                    {
                        if (p.playerName.Equals(person))
                        {
                            playerInfoContainer.Remove(p);
                            await ctx.RespondAsync("Removed " + person + " from character randomization pool");

                            break;
                        }
                    }

                    SyncPlayerInfo();
                }
                else
                {
                    await ctx.RespondAsync("Cannot find " + person + " in active pool");
                }
                WriteFile();
            }
            else
            {
                list.Add(("ADMIN ONLY COMMAND", "sry, but using this without access to raw json might cause ireversible problems :("));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("remove", true, list));
            }
        }

        [Command("Rename")]
        [DSharpPlus.CommandsNext.Attributes.Description("Rename an existing player")]
        public async Task Rename(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("Person/player to rename")] String person, [DSharpPlus.CommandsNext.Attributes.Description("Person's new name")] String newName)
        {
            if (!isRead)
            {
                startUp();
            }
            ReadJson();

            List<(string, string)> list = new List<(string, string)>();

            if (people.Contains(person))
            {
                people.Remove(person);
                people.Add(newName);
                list.Add(("Replaced " + person + " with " + newName, "seizures"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("rename", false, list));

                if (activePool.Contains(person))
                {
                    activePool.Remove(person);
                    activePool.Add(newName);
                }
                WriteFile();

                foreach (Structs.PlayerMatches pm in playerMatches)
                {
                    if (pm.player1.Equals(person))
                    {
                        pm.player1 = newName;
                    }
                    else if (pm.player2.Equals(person))
                    {
                        pm.player2 = newName;
                    }
                }

                foreach (Structs.Player p in playerInfoContainer)
                {
                    if (p.playerName.Equals(person))
                    { 
                        p.playerName = newName; 
                    }
                }

                WriteJson();
                SyncPlayerInfo();
            }
            else
            {
                list.Add(("Cannot find " + person + " in overall player pool", "did u make a typo?"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("rename", true, list));
            }
        }

        [Command("willfight")]
        [DSharpPlus.CommandsNext.Attributes.Description("determine whether or not to count a player in duel")]
        public async Task WillFight(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("name of player")] string playerName, [DSharpPlus.CommandsNext.Attributes.Description("status")] Boolean status)
        {
            List<(string, string)> list = new List<(string, string)> ();

            if (!activePool.Contains(playerName))
            {
                list.Add(("Cannot find " + playerName + " in active pool", "did u spell something wrong?"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("addcharacter", true, list));
                return;
            }

            foreach (Structs.Player p in playerInfoContainer)
            {
                if (p.playerName.Equals(playerName))
                {
                    p.willFight = status;
                    list.Add(("Set " + playerName + "'s status to " + status.ToString(), "[this is a field]"));
                    await ctx.Channel.SendMessageAsync(BuildGenericEmbed("willfight", false, list));
                    SyncPlayerInfo();
                    return;
                }
            }

            SyncPlayerInfo();
        }

        [Command("addcharacter")]
        [DSharpPlus.CommandsNext.Attributes.Description("add a character to a player's name during randomization")]
        public async Task AddCharacter(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("name of player")] string playerName, [DSharpPlus.CommandsNext.Attributes.Description("name of character")] string character)
        {
            if (!isRead)
            {
                startUp();
            }

            List<(string, string)> list = new List<(string, string)> ();

            if (!activePool.Contains(playerName))
            {
                list.Add(("Cannot find " + playerName + " in active pool", "did u spell something wrong?"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("addcharacter", true, list));
                return;
            }

            Boolean notFound = true;
            for (int i = 0; i < playerInfoContainer.Count; ++i)
            {
                if (playerInfoContainer[i].playerName.Equals(playerName))
                {
                    if (!playerInfoContainer[i].characters.Contains(character))
                    {
                        playerInfoContainer[i].characters.Add(character);
                        if (playerInfoContainer[i].defaultCharacter == null)
                        {
                            playerInfoContainer[i].defaultCharacter = character;
                        }

                        list.Add(("Added " + character + " to " + playerName + "'s list", "[this is a field]"));
                        await ctx.Channel.SendMessageAsync(BuildGenericEmbed("addcharacter", false, list));
                    }
                    else
                    {
                        list.Add((character + " is already present in this player's list", "skill issue idk"));
                        await ctx.Channel.SendMessageAsync(BuildGenericEmbed("addcharacter", true, list));
                    }
                    notFound = false;
                    break;
                }
                else 
                {
                    notFound = notFound && true;
                }
            }

            if(notFound) 
            {
                Structs.Player temp = new Structs.Player();
                temp.characters = new List<string>();
                temp.characters.Add(character);
                temp.playerName = playerName;
                temp.willFight = true;
                playerInfoContainer.Add(temp);

                list.Add(("Added " + character + " to " + playerName + "'s list", "[this is a field]"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("addcharacter", false, list));
            }
            SyncPlayerInfo();
        }

        [Command("removecharacter")]
        [DSharpPlus.CommandsNext.Attributes.Description("remove a character to a player's name during randomization")]
        public async Task RemoveCharacter(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("name of player")] string playerName, [DSharpPlus.CommandsNext.Attributes.Description("name of character")] string character)
        {
            if (!isRead)
            {
                startUp();
            }

            List<(string, string)> list = new List<(string, string)>();

            if (!activePool.Contains(playerName))
            {
                list.Add(("Cannot find " + playerName + " in active pool", "run \">ggstplayers\" for current list of active players"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("removecharacter", true, list));
                return;
            }

            for (int i = 0; i < playerInfoContainer.Count; ++i)
            {
                if (playerInfoContainer[i].playerName.Equals(playerName))
                {
                    if (playerInfoContainer[i].characters.Contains(character))
                    {
                        playerInfoContainer[i].characters.Remove(character);

                        list.Add(("Removed " + character + " from " + playerName + "'s list", "[this is a field]"));
                        await ctx.Channel.SendMessageAsync(BuildGenericEmbed("removecharacter", false, list));
                    }
                    else
                    {
                        list.Add((character + " is not present in this player's list", "did you spell something wrong?"));
                        await ctx.Channel.SendMessageAsync(BuildGenericEmbed("removecharacter", true, list));
                    }
                }
            }
            SyncPlayerInfo();
        }

        [Command("setdefault")]
        [DSharpPlus.CommandsNext.Attributes.Description("set the default character for a player")]
        public async Task SetDefault(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("player name")] string player, [DSharpPlus.CommandsNext.Attributes.Description("player name")] string character)
        {
            List<(string, string)> list = new List<(string, string)>();

            if (!activePool.Contains(player))
            {
                list.Add(("Cannot find " + player + " in active pool", "run \">ggstplayers\" for current list of active players"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("allowrandomize", true, list));
                return;
            }
            foreach (Structs.Player p in playerInfoContainer)
            { 
                if(p.playerName.Equals(player)) 
                {
                    if (!p.characters.Contains(character))
                    {
                        list.Add((player + " does not have " + character + "in their list", "did u make a typo?"));
                        await ctx.Channel.SendMessageAsync(BuildGenericEmbed("setdefault", true, list));
                        return;
                    }

                    p.defaultCharacter = character;
                    list.Add(("Set " + character + " as " + player + "'s default", "[this is a field]"));
                    await ctx.Channel.SendMessageAsync(BuildGenericEmbed("setdefault", false, list));
                    break;
                }
            }
        }

        [Command("randomize")]
        [DSharpPlus.CommandsNext.Attributes.Description("Set whether or not a player's character can be randomized with \"-r\" flag in >duel")]
        public async Task Randomize(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("player name")] string player, [DSharpPlus.CommandsNext.Attributes.Description("status to change to")] Boolean status)
        {
            List<(string, string)> list = new List<(string, string)>();

            if (!activePool.Contains(player))
            {
                list.Add(("Cannot find " + player + " in active pool", "run \">ggstplayers\" for current list of active players"));
                await ctx.Channel.SendMessageAsync(BuildGenericEmbed("randomize", true, list));
                return;
            }
            foreach (Structs.Player p in playerInfoContainer)
            {
                if (p.playerName.Equals(player))
                {
                    p.randomize = status;
                    list.Add(("Set " + player + "'s status to " + status.ToString(), "[this is a field]"));
                    await ctx.Channel.SendMessageAsync(BuildGenericEmbed("randomize", false, list));
                    SyncPlayerInfo();
                    return;
                }
            }
        }
        
        [Command("ggstplayers")]
        [DSharpPlus.CommandsNext.Attributes.Description("Display all registered players")]
        public async Task GGSTPlayers(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("[optional: enter \"-a\"] check for players in total player pool")] String optionalPool = "active")
        {
            if (!isRead)
            {
                startUp();
            }

            StringBuilder sb = new StringBuilder();
            String mod = "";
            SyncFile();
            if (optionalPool.Equals(("-a").ToLower()))
            {
                mod = "all players";
                foreach (String s in people)
                {
                    sb.Append(s + '\n');
                }
            }
            else
            {
                mod = "active";
                foreach (String s in activePool)
                {
                    sb.Append(s + '\n');
                }
            }
            await ctx.RespondAsync(String.Format("Current players in {0} pool: \n" + sb.ToString(), mod));
        }

        private DiscordEmbed BuildGenericEmbed(string title, Boolean isError, List<(string, string)> fields)
        {
            DiscordEmbedBuilder dmb = new DiscordEmbedBuilder();

            dmb.Title = ">" + title;

            foreach ((string, string) field in fields)
            {
                dmb.AddField(field.Item1, field.Item2);
            }

            dmb.Color = DiscordColor.Green;
            if (isError)
            {
                dmb.Color = DiscordColor.Red;
            }
            return dmb.Build();
        }

        private void SyncFile()
        {
            WriteFile();
            activePool.Clear();
            people.Clear();
            ReadFile();
        }

        private void SyncPlayerInfo()
        {
            WritePlayerInfo();
            playerInfoContainer.Clear();
            ReadPlayerInfo();
        }

        private void ReadFile()
        {
            StreamReader sr = new StreamReader("C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTPlayers.txt");
            string line = sr.ReadLine();
            while (line != null)
            {
                activePool.Add(line);
                people.Add(line);
                line = sr.ReadLine();
            }
            sr.Close();
        }

        private void WriteFile()
        {
            System.IO.File.WriteAllText("C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTPlayers.txt", string.Empty);
            StreamWriter sw = new StreamWriter("C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTPlayers.txt");
            foreach (string s in activePool)
            {
                sw.WriteLine(s);
            }
            sw.Close();
        }

        private void WriteJson()
        {
            var json = JsonConvert.SerializeObject(playerMatches.ToArray(), Formatting.Indented);
            System.IO.File.WriteAllText(@"C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTMatches.json", json);
        }

        private void ReadJson()
        {
            try
            {
                playerMatches = JsonConvert.DeserializeObject<List<Structs.PlayerMatches>>(System.IO.File.ReadAllText(@"C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTMatches.json"));
                if (playerMatches == null)
                {
                    playerMatches = new List<Structs.PlayerMatches>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ReadPlayerInfo()
        {
            try
            {
                playerInfoContainer = JsonConvert.DeserializeObject<List<Structs.Player>>(System.IO.File.ReadAllText(@"C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTPlayerInfo.json"));
                if (playerInfoContainer == null)
                {
                    playerInfoContainer = new List<Structs.Player>();
                }
                foreach (Structs.Player p in playerInfoContainer)
                {
                    if (p.defaultCharacter == null)
                    {
                        if (p.characters.Count != 0) { p.defaultCharacter = p.characters[0]; };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);    
            }
        }

        private void WritePlayerInfo()
        {
            var json = JsonConvert.SerializeObject(playerInfoContainer.ToArray(), Formatting.Indented);
            System.IO.File.WriteAllText(@"C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Commands\\GGST\\GGSTPlayerInfo.json", json);
        }

        private void startUp()
        {
            ReadFile();
            isRead = true;
            ReadJson();
            ReadPlayerInfo();
            duelMessages.Add("{0} will now be subject to the wrath of {1}");
            duelMessages.Add("Wheel has chosen {0} and {1} to fight");
            duelMessages.Add("{0} wants to be 632146P'ed by {1}");
            duelMessages.Add("{0} and {1} will be sent to the gulag");
        }
    }
}
