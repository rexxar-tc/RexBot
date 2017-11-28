using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandStatus : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Developer;
        public string Command => "!status";
        public string HelpText => "Sets RexBot's current game";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            if (message.Content.Length == Command.Length)
            {
                string status = await RexBotCore.Instance.SetRandomStatus();
                return $"Set status to random entry `{status}`";
            }

            string arg = message.Content.Substring(Command.Length + 1);

            await RexBotCore.Instance.RexbotClient.UpdateStatusAsync(new DiscordGame(arg));

            return $"Set status to `{arg}`";
        }
    }
}