using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandRexStatus : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!rstatus";
        public string HelpText => "secret";

        public async Task<string> Handle(SocketMessage message)
        {
            UserStatus status;

            if (!Enum.TryParse(message.Content.Substring(Command.Length + 1), out status))
                return $"Correct values are {string.Join(", ", Enum.GetNames(typeof(UserStatus)))}";

            await RexBotCore.Instance.RexxarClient.SetStatusAsync(status);
            return $"Set rexxar to {status}";
        }
    }
}