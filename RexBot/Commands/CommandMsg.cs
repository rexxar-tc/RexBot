using System.Threading.Tasks;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandMsg : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!msg";
        public string HelpText => "Sends a message to the given channel";

        public async Task<string> Handle(SocketMessage message)
        {
            string[] args = Utilities.ParseCommand(message.Content);

            ulong id;
            if (!ulong.TryParse(args[0], out id))
                return "Can't parse channel ID!";

            foreach (SocketGuild guild in RexBotCore.Instance.RexbotClient.Guilds)
            {
                SocketGuildChannel channel = guild.GetChannel(id);
                if (channel == null)
                    continue;

                await ((ISocketMessageChannel)channel).SendMessageAsync(string.Join(" ", args, 1, args.Length - 1));
                break;
            }

            return "Ok";
        }
    }
}