using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandOpenBug : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!openbug";
        public string HelpText => "";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            Process.Start($"https://jira.keenswh.com:442/browse/{arg}");
            return null;
        }
    }
}
