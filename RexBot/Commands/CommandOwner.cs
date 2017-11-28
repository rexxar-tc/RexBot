using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandOwner:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!owner";
        public string HelpText => "Finds the owner of a given chat command.";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            if (string.IsNullOrEmpty(arg))
                return "You must specify a command to search for.";

            foreach (var info in RexBotCore.Instance.InfoCommands)
            {
                if (!info.Command.Equals(arg, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if(!info.IsPublic)
                    continue;

                return $"Command {info.Command} is owned by <@{info.Author}>.";
            }

            return $"Can't find command {arg}";
        }
    }
}
