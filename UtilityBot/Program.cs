using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Text;
using UtilityBot.Commands;
using UtilityBot.Commands.GGST;
using UtilityBot.EventArgs;

namespace UtilityBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var json = string.Empty;
            string path = @"C:\Users\Beebd\source\repos\UtilityBot\UtilityBot\config.json";

            using (var fs = File.OpenRead(path))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = configJson.token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents | DiscordIntents.GuildMessages
            });

            var commands = discordClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { configJson.Prefix }
            });

            commands.RegisterCommands<Logging>();
            commands.RegisterCommands<GuiltyGear>();
            commands.RegisterCommands<Events>();

            discordClient.MessageCreated += new Events().MessageCreatedHandlerTest;
            

            await discordClient.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}