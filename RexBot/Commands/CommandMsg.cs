using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandMsg : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!msg";
        public string HelpText => "Sends a message to the given channel";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            string[] args = Utilities.ParseCommand(message.Content);

            ulong id;
            if (!ulong.TryParse(args[0], out id))
                return "Can't parse channel ID!";

            foreach (var e in RexBotCore.Instance.RexbotClient.Guilds)
            {
                var guild = e.Value;
                var channel = guild.GetChannel(id);
                if (channel == null)
                    continue;

                await channel.SendMessageAsync(string.Join(" ", args, 1, args.Length - 1));
                break;
            }

            return "Ok";
        }
    }
}