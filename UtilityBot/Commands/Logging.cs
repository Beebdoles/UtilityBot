using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityBot.Commands
{
    public class Logging : BaseCommandModule
    {
        [Command("Ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("Pong");
        }
    }
}
