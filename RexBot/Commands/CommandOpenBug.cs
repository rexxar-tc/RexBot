using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandOpenBug : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!openbug";
        public string HelpText => "";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            Process.Start($"https://jira.keenswh.com:442/browse/{arg}");
            return null;
        }
    }
}
