using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandOwner:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!owner";
        public string HelpText => "Finds the owner of a given chat command.";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
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
