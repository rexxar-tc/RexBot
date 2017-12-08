using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandAddAuto : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!addauto";
        public string HelpText => "Adds auto command. Syntax `!addauto \"regex|pattern\" \"response\"";
        public DiscordEmbed HelpEmbed { get; }
        public async Task<string> Handle(DiscordMessage message)
        {
            var args = Utilities.ParseCommand(message.Content);
            if (args.Length != 2)
                return "Could not parse arguments!";
            var auto = new RexBotCore.AutoCommand(args[0].Trim('"'), args[1].Trim('"'), message.Author.Id);
            RexBotCore.Instance.AutoCommands.Add(auto);
            RexBotCore.Instance.SaveCommands();

            return $"Added autocommand with pattern `{auto.Pattern}` response `{auto.Response}`";
        }
    }
}
