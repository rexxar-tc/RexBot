using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandBan : IChatCommand
    { 
        public CommandAccess Access => CommandAccess.Moderator;
        public string Command => "!ban";
        public string HelpText => "Bans users from all RexBot functions.";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            if (!message.MentionedUsers.Any())
            {
                if (!RexBotCore.Instance.BannedUsers.Any())
                    return "No users currently banned.";

                var sb = new StringBuilder();
                sb.AppendLine("Banned users:");
                foreach (var id in RexBotCore.Instance.BannedUsers)
                {
                    var user = await RexBotCore.Instance.RexbotClient.GetUserAsync(id);
                    sb.AppendLine(user.NickOrUserName());
                }
                return sb.ToString();
            }

            foreach (var user in message.MentionedUsers)
            {
                ulong id = user.Id;
                if ((id == RexBotCore.REXXAR_ID) || (id == RexBotCore.REXBOT_ID))
                    return "Cannot ban Rexxar or RexBot!";

                if (!RexBotCore.Instance.BannedUsers.Contains(id))
                    RexBotCore.Instance.BannedUsers.Add(id);
            }

            RexBotCore.Instance.SaveBanned();

            return $"Banned {string.Join(", ", message.MentionedUsers.Select(u => u.Mention))} from all RexBot functions.";
        }
    }
}