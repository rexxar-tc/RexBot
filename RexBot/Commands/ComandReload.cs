using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class ComandReload : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!reloadcommands";
        public string HelpText => "Reloads commands from disk";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            RexBotCore.Instance.InfoCommands.Clear();
            RexBotCore.Instance.LoadCommands();
            return "Done.";
        }
    }
}