using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Text;
using UtilityBot.Commands;
using UtilityBot.Commands.GGST;

namespace UtilityBot.EventArgs
{
    public class Events : DiscordEventArgs
    {
        public async Task MessageCreatedHandler(DiscordClient s, MessageCreateEventArgs e)
        {
            Console.WriteLine("Hi");
            if (e.Author.Id == 442104107126489098)
            {
                Console.WriteLine("Workd");
                await e.Message.RespondAsync("HI");
            }
            /*
            if (e.Guild?.Id == 379378609942560770 && e.Author.Id == 168548441939509248)
            {
                await e.Message.DeleteAsync();
            }
            */
        }
    }
}
