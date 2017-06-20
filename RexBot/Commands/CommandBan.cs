using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandBan : IChatCommand
    { 
        public CommandAccess Access => CommandAccess.Developer;
        public string Command => "!ban";
        public string HelpText => "Bans users from all RexBot functions.";

        public async Task<string> Handle(SocketMessage message)
        {
            if (!message.MentionedUsers.Any())
            {
                var guild = (message.Author as SocketGuildUser)?.Guild;

                if (!RexBotCore.Instance.BannedUsers.Any())
                    return "No users currently banned.";

                var sb = new StringBuilder();
                sb.AppendLine("Banned users:");
                foreach (var id in RexBotCore.Instance.BannedUsers)
                {
                    var user =RexBotCore.Instance.RexbotClient.GetUser(id);
                    sb.AppendLine(guild?.GetUser(id).Nickname ?? user.Username);
                }
                return sb.ToString();
            }

            foreach (SocketUser user in message.MentionedUsers)
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