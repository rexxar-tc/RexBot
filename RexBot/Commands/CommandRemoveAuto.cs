using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandRemoveAuto : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!removeauto";
        public string HelpText => "Removes an autocommand at the given index. Use `!listautos` to find the command to remove.";
        public DiscordEmbed HelpEmbed { get; }
        public async Task<string> Handle(DiscordMessage message)
        {
            int arg;
            if (!int.TryParse(Utilities.StripCommand(this, message.Content).Trim(), out arg))
                return "Couldn't parse index!";

            var auto = RexBotCore.Instance.AutoCommands[arg];

            if (message.Author.Id == auto.Author || Utilities.HasAccess(CommandAccess.Moderator, message.Author))
            {
                RexBotCore.Instance.AutoCommands.Remove(auto);
                RexBotCore.Instance.SaveCommands();
                return "Removed command.";
            }
            else
                return "Only the command author can remove commands!";
        }
    }
}
