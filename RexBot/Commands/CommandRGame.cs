using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandRGame : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!rgame";
        public string HelpText => "";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            await RexBotCore.Instance.RexxarClient.UpdateStatusAsync(new DiscordGame(arg));
            return null;
        }
    }
}
