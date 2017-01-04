using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandAllInfo : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!allinfo";
        public string HelpText => "Dumps all info commands";
        public async Task<string> Handle(SocketMessage message)
        {
            var channel = message.Channel;
            foreach (var command in RexBotCore.Instance.InfoCommands)
            {
                if (!command.ImageResponse)
                    await channel.SendMessageAsync(command.Response);
                else
                    await channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, command.Response));
            }

            return string.Empty;
        }
    }
}
