using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class Emoji : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!emoji";
        public string HelpText => "Gets a random emoji";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            return RexBotCore.Instance.GetRandomEmoji();
        }
    }
}
