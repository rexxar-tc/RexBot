using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandHelp : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!help";
        public string HelpText => "Shows help. !help with no argument lists commands. !help (command) shows help for that command.";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            bool isRexxar = message.Author.Id == RexBotCore.REXXAR_ID;
            if (message.Channel.Id != 345301157272354837)
                isRexxar = false;

            if (message.Content.Length == Command.Length)
            {
                var em = new DiscordEmbedBuilder();
                em.AddField("Info commands",
                            string.Join(", ", RexBotCore.Instance.InfoCommands.Where(i => i.Author == RexBotCore.REXXAR_ID && (i.IsPublic || isRexxar)).Select(j => j.Command)));
                em.AddField("User commands",
                            string.Join(", ", RexBotCore.Instance.InfoCommands.Where(i => i.Author != RexBotCore.REXXAR_ID && (i.IsPublic || isRexxar)).Select(j => j.Command)));
                em.AddField("System commands",
                            string.Join(", ", RexBotCore.Instance.ChatCommands.Where(c => c.HasAccess(message.Author)).Where(c => c.Access < CommandAccess.Rexxar || isRexxar).Select(c => c.Command)));
                em.Color = new DiscordColor(102, 153, 255);
               
                await message.Channel.SendMessageAsync($"{message.Author.Mention} Use `!help [command]` for more info", embed: em.Build());
                return null;
            }
            else
            {
                string search = message.Content.Substring(Command.Length + 1);
                if (!search.StartsWith("!"))
                    search = "!" + search;
                IChatCommand command = RexBotCore.Instance.ChatCommands.FirstOrDefault(c => c.Command.Equals(search, StringComparison.CurrentCultureIgnoreCase));
                if (command != null)
                    if (command.HasAccess(message.Author))
                    {
                        if (command.HelpEmbed == null)
                        {
                            var em = new DiscordEmbedBuilder();
                            em.AddField(command.Command,
                                        command.HelpText);
                            await message.Channel.SendMessageAsync(message.Author.Mention, embed: em.Build());
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync(message.Author.Mention, embed: command.HelpEmbed);
                        }
                        return null;
                    }
                    else
                        return "You aren't allowed to use that command!";

                foreach (RexBotCore.InfoCommand info in RexBotCore.Instance.InfoCommands)
                {
                    if (!info.Command.Equals(search, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    if (info.IsPublic || isRexxar)
                        return "Responds with text or an image";
                    return "You aren't allowed to use that command!";
                }

                return $"Couldn't find command `{search}`";
            }
        }
    }
}