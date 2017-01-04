using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandRexStatus : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!rstatus";
        public string HelpText => "secret";
        public async Task<string> Handle(SocketMessage message)
        {
            UserStatus status;

            if (!Enum.TryParse(message.Content.Substring(Command.Length + 1), out status))
            {
                return $"Correct values are {string.Join(", ", Enum.GetNames(typeof(UserStatus)))}";
            }

            await RexBotCore.Instance.RexxarClient.SetStatus(status);
            return $"Set rexxar to {status}";
        }
    }
}
