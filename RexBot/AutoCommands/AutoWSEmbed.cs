using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using RexBot.Commands;

namespace RexBot.AutoCommands
{
    class AutoWSEmbed : IAutoCommand
    {
        public Regex Pattern => new Regex(@"^(http[s]{0,1}://){0,1}[^/]*steamcommunity.com/sharedfiles/filedetails", RegexOptions.IgnoreCase);
        public async Task<string> Handle(DiscordMessage message)
        {
            if (message.Content.StartsWith("!ws", StringComparison.InvariantCultureIgnoreCase))
                return null;

            await CommandSteamWsEmbed.HandleInternal(message.Content, message, true, "syntax error");
            return null;
        }
    }
}
