using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class ComandReload : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!reloadcommands";
        public string HelpText => "Reloads commands from disk";

        public string Handle( SocketMessage message )
        {
            RexBotCore.Instance.InfoCommands.Clear();
            RexBotCore.Instance.LoadCommands();
            return "Done.";
        }
    }
}
