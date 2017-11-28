using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandAllInfo : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!allinfo";
        public string HelpText => "Dumps all info commands";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            DiscordChannel channel = message.Channel;
            foreach (RexBotCore.InfoCommand command in RexBotCore.Instance.InfoCommands)
                if (!command.ImageResponse)
                    await channel.SendMessageAsync($"{message.Author.Mention} {command.Response}");
                else
                {
                    if (!command.Response.StartsWith("http"))
                        await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, command.Response), message.Author.Mention);
                    else
                    {
                        var em = new DiscordEmbedBuilder();
                        em.ImageUrl = command.Response;
                        await channel.SendMessageAsync(message.Author.Mention, embed: em.Build());
                    }
                }
            return string.Empty;
        }
    }
}