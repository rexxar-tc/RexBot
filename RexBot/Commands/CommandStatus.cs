using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandStatus : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!status";
        public string HelpText => "Sets RexBot's status";
        public string Handle(SocketMessage message)
        {
            string arg = message.Content.Substring(Command.Length + 1);

            RexBotCore.Instance.RexbotClient.SetGame(arg);

            return string.Empty;
        }
    }
}
