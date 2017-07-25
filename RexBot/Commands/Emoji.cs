using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class Emoji : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!emoji";
        public string HelpText => "Gets a random emoji";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            return RexBotCore.Instance.GetRandomEmoji();
        }
    }
}
