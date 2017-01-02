using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public string Handle( SocketMessage message )
        {
            var channel = message.Channel;
            Parallel.ForEach(RexBotCore.Instance.InfoCommands, command =>
                                                               {
                                                                   if (!command.ImageResponse)
                                                                       channel.SendMessageAsync(command.Response);
                                                                   else
                                                                       channel.SendFileAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, command.Response));
                                                               });
           // Process(channel).RunSynchronously();
            return string.Empty;
        }

        private async Task Process(ISocketMessageChannel channel)
        {
            //foreach(var command in RexBotCore.Instance.InfoCommands)
        }
    }
}
