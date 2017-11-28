using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    public class CommandTest : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!test";
        public string HelpText => "";
        public DiscordEmbed HelpEmbed { get; }
        
        public async Task<string> Handle(DiscordMessage message)
        {
            return "tested";
        }
    }
}