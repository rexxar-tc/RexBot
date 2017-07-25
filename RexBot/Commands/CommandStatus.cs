using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandStatus : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Developer;
        public string Command => "!status";
        public string HelpText => "Sets RexBot's current game";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            if (message.Content.Length == Command.Length)
            {
                string status = await RexBotCore.Instance.SetRandomStatus();
                return $"Set status to random entry `{status}`";
            }

            string arg = message.Content.Substring(Command.Length + 1);

            await RexBotCore.Instance.RexbotClient.SetGameAsync(arg);

            return $"Set status to `{arg}`";
        }
    }
}