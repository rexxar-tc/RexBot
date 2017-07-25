using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandRGame : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!rgame";
        public string HelpText => "";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            await RexBotCore.Instance.RexxarClient.SetGameAsync(arg);
            return null;
        }
    }
}
