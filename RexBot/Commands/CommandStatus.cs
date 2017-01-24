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
        public string HelpText => "Sets RexBot's current game";
        public async Task<string> Handle(SocketMessage message)
        {
            if (message.Content.Length == Command.Length)
            {
                string status = await RexBotCore.Instance.SetRandomStatus();
                return $"Set status to random entry `{status}`";
            }

            string arg = message.Content.Substring(Command.Length + 1);

            await RexBotCore.Instance.RexbotClient.SetGame(arg);

            return $"Set status to `{arg}`";
        }
    }
}
