using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandRemoveCommand : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!removecommand";
        public string HelpText => "Removes info command. `!removecommand [!commandKey]";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            string target = message.Content.Substring(Command.Length + 1);
            foreach (RexBotCore.InfoCommand info in RexBotCore.Instance.InfoCommands)
                if (info.Command.Equals(target, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (info.Author != message.Author.Id && !message.Author.IsRexxar())
                        return "You can't remove a command added by another user!";

                    RexBotCore.Instance.InfoCommands.Remove(info);
                    RexBotCore.Instance.SaveCommands();
                    return $"Removing command {info.Command}";
                }

            return $"Couldn't find {target}";
        }
    }
}