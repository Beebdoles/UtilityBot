using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.EventArgs;
using System.Text;


namespace UtilityBot.EventArgs
{
    public class Events : BaseCommandModule
    {
        private static HashSet<string> BannedStrings = new HashSet<string>();
        private static HashSet<ulong> Blacklist = [623854263155818496];
        private static Boolean startup = false;
        private static DateTime lastUpdate = new DateTime(2025, 1, 1, 1, 1, 1);

        public async Task MessageCreatedHandlerTest(DiscordClient s, MessageCreateEventArgs e)
        {
            if (!startup)
            {
                ReadFile();
            }
            if (Blacklist.Contains(e.Message.Author.Id) && BannedStrings.Contains(e.Message.Content))
            {
                await e.Message.DeleteAsync();
                await e.Message.RespondAsync("Yimmy has been silenced");
            }
        }

        [Command("AddPhrase")]
        [Description("Increase banned words for yimmy")]
        public async Task ExpandBannedWords(CommandContext ctx, params string[] phrase)
        {
            string str = "";
            for (int i = 0; i < phrase.Length; ++i)
            {
                if (i != phrase.Length - 1) { str += phrase[i] + " "; } else { str += phrase[i]; }

            }
            if (!str.Equals("")) { BannedStrings.Add(str); } else { await ctx.RespondAsync("Please specify a word/phrase to ban for yimmy"); return; }

            await ctx.RespondAsync("Added " + str + " to ban list for yimmy");

            if (DateTime.Now - lastUpdate > new TimeSpan(0, 1, 0, 0)) 
            {
                UpdateFile();
                lastUpdate = DateTime.Now;
            }
        }

        [Command("ViewBannedWords")]
        [Description("View all words and phrases yimmy cant say")]
        public async Task ViewBannedWords(CommandContext ctx)
        {
            string str = "";
            foreach (string s in BannedStrings)
            {
                str += s + ", ";
            }
            await ctx.RespondAsync("Banned words: " + str);
        }

        private void UpdateFile()
        {
            string path = "C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Events\\banlist.txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                foreach (string s in BannedStrings)
                {
                    sw.WriteLine(s);
                }
            }
        }

        private void ReadFile()
        {
            string path = "C:\\Users\\Beebd\\source\\repos\\UtilityBot\\UtilityBot\\Events\\banlist.txt";
            using (StreamReader sr = File.OpenText(path))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    BannedStrings.Add(s);
                }
            }
        }
    }
}
