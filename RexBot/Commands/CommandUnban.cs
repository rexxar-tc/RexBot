using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandUnban : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Moderator;
        public string Command => "!unban";
        public string HelpText => "Allows banned users to access RexBot again.";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            if (!message.MentionedUsers.Any())
                return "Must specify at least one user to unban.";

            foreach (var user in message.MentionedUsers)
                RexBotCore.Instance.BannedUsers.Remove(user.Id);

            RexBotCore.Instance.SaveBanned();

            return $"Unbanned {string.Join(", ", message.MentionedUsers.Select(u => u.Mention))}";
        }
    }
}