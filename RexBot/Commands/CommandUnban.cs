using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandUnban : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Developer;
        public string Command => "!unban";
        public string HelpText => "Allows banned users to access RexBot again.";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            if (!message.MentionedUsers.Any())
                return "Must specify at least one user to unban.";

            foreach (SocketUser user in message.MentionedUsers)
                RexBotCore.Instance.BannedUsers.Remove(user.Id);

            RexBotCore.Instance.SaveBanned();

            return $"Unbanned {string.Join(", ", message.MentionedUsers.Select(u => u.Mention))}";
        }
    }
}