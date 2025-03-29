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

namespace UtilityBot.Commands.GGST
{
    public class GuiltyGear : BaseCommandModule
    {
        private bool isRead = false;
        private bool isMatchOngoing = false;
        private String player1;
        private String player2;

        private List<string> activePool = new List<string>();
        private List<string> people = new List<string>();
        private List<string> duelMessages = new List<string>();
        private List<Structs.PlayerMatches> playerMatches = new List<Structs.PlayerMatches>();


        [Command("Duel")]
        [DSharpPlus.CommandsNext.Attributes.Description("Starts a duel by randomly choosing people from the active pool")]
        public async Task Duel(CommandContext ctx)
        {
            if(!isRead)
            {
                startUp();
            }
            if (activePool.Count > 1)
            {
                isMatchOngoing = false;
                Random rd = new Random();
                int a = rd.Next(0, activePool.Count);
                int b = a;
                while (a == b)
                {
                    b = rd.Next(0, activePool.Count);
                }
                await ctx.RespondAsync(string.Format(duelMessages[rd.Next(0, duelMessages.Count)], activePool[a], activePool[b]) + "\nPlayer 1: " + activePool[a] + " Player 2: " + activePool[b]);
                //await ctx.RespondAsync("Player 1: " + activePool[a] + ". Player 2: " + activePool[b]);
                isMatchOngoing = true;
                player1 = activePool[a];
                player2 = activePool[b];
            }
            else
            {
                await ctx.RespondAsync("not enough people to create a duel");
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
            if(!isRead)
            {
                startUp();
            }
            if (isMatchOngoing || matchOverride)
            {
                isMatchOngoing = false;
                Structs.DuelData dd = new Structs.DuelData();

                String currentid1;
                String currentid2;
                if(matchOverride)
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
                            if(pm.player1.Equals(player1) || (matchOverride && pm.player1.Equals(optionalPlayer1)))
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
                    if(matchOverride)
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
            foreach(Structs.PlayerMatches pm in playerMatches)
            {
                if(id.Equals(pm.player1 + pm.player2) || id.Equals(pm.player2 + pm.player1))
                {
                    if(matchNum <= pm.totalMatches)
                    {
                        Structs.DuelData tempDD = pm.matches[matchNum - 1];
                        if(tempDD.player1Score > tempDD.player2Score)
                        {
                            --pm.player1Wins;
                        }
                        else
                        {
                            --pm.player2Wins;
                        }
                        pm.matches.Remove(pm.matches[matchNum - 1]);
                        for(int i = matchNum - 1; i < pm.matches.Count; ++i)
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
            if(!found)
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
                    if(optionalPage == 1)
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
            foreach(Structs.PlayerMatches pm in playerMatches)
            {
                if((pm.player1 + pm.player2).Equals(id1) || (pm.player1 + pm.player2).Equals(id2))
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
            if(!isFound)
            {
                await ctx.RespondAsync("Player match not found");
            }
        }

        [Command("Add")]
        [DSharpPlus.CommandsNext.Attributes.Description("Add a new person to the player and active pool")]
        public async Task Add(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("Person to add")] string person)
        {
            if (!isRead)
            {
                startUp();
            }
            if (people.Contains(person))
            {
                if (!activePool.Contains(person))
                {
                    activePool.Add(person);
                    await ctx.RespondAsync("Added " + person + " to the active pool");
                }
                else
                {
                    await ctx.RespondAsync(person + " is already in the active pool");
                }
            }
            else
            {
                activePool.Add(person);
                people.Add(person);
                await ctx.RespondAsync("Added " + person + " to the player pool and active pool");
            }
            WriteFile();
        }

        [Command("Remove")]
        [DSharpPlus.CommandsNext.Attributes.Description("remove a person from the active pool")]
        public async Task Remove(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("Person to remove")] string person)
        {
            if (!isRead)
            {
                startUp();
            }
            if (activePool.Remove(person))
            {
                await ctx.RespondAsync("Removed " + person + " from the active pool");
            }
            else
            {
                await ctx.RespondAsync("Cannot find " + person + " in active pool");
            }
            WriteFile();
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
            if (people.Contains(person))
            {
                people.Remove(person);
                people.Add(newName);
                await ctx.RespondAsync("Replaced " + person + " with " + newName);

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
                    else if(pm.player2.Equals(person))
                    {
                        pm.player2 = newName;
                    }
                }
                WriteJson();
            }
            else
            {
                await ctx.RespondAsync("Cannot find " + person + " in overall player pool");
            }
        }

        [Command("ggstplayers")]
        [DSharpPlus.CommandsNext.Attributes.Description("Display all registered players")]
        public async Task GGSTPlayers(CommandContext ctx, [DSharpPlus.CommandsNext.Attributes.Description("[optional: enter \"people\"] check for players in active player pool")] String optionalPool = "active")
        {
            if(!isRead)
            {
                startUp();
            }

            StringBuilder sb = new StringBuilder();
            String mod = "";
            if (optionalPool.Equals(("active").ToLower()))
            {
                mod = "active duel";
                foreach(String s in activePool)
                {
                    sb.Append(s + " ");
                }
            }
            else if(optionalPool.Equals("people"))
            {
                mod = "overall player";
                foreach(String s in people)
                {
                    sb.Append(s + " ");
                }
            }
            await ctx.RespondAsync(String.Format("Current players in {0} pool: \n" + sb.ToString(), mod));
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
            foreach (string s in people)
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
                if(playerMatches == null)
                {
                    playerMatches = new List<Structs.PlayerMatches>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void startUp()
        {
            ReadFile();
            isRead = true;
            ReadJson();
            duelMessages.Add("{0} will now be subject to the wrath of {1}");
            duelMessages.Add("Wheel has chosen {0} and {1} to fight");
            duelMessages.Add("{0} wants to be 632146P'ed by {1}");
            duelMessages.Add("{0} and {1} will be sent to the gulag");
        }
    }
}
