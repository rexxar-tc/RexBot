using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandRexStatus : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!rstatus";
        public string HelpText => "secret";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            UserStatus status;

            if (!Enum.TryParse(message.Content.Substring(Command.Length + 1), out status))
                return $"Correct values are {string.Join(", ", Enum.GetNames(typeof(UserStatus)))}";

            await RexBotCore.Instance.RexbotClient.UpdateStatusAsync(null, status);
            return $"Set RexBot to {status}";
        }
    }
}