using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandStats : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!stats";
        public string HelpText => "Gets post statistics for a channel or user. Usage: `!stats @rexxar` or `!stats #general`";

        public async Task<string> Handle(SocketMessage message)
        {
            var guild = (message.Channel as IGuildChannel)?.Guild;
            if (!((guild?.Id == 125011928711036928) || (guild?.Id == 263612647579189248)))
                return "This command is only available in the KSH discord.";

            if (message.MentionedUsers.Any() && message.MentionedChannels.Any())
                return "You must specify a channel *or* a user.";

            var channel = message.MentionedChannels.FirstOrDefault();
            if (channel != null)
            {
                var messages = RexBotCore.Instance.DBManager.GetMessages(channel.Id);

                ulong[] topUserIds = messages.GroupBy(m => m.AuthorId).OrderByDescending(n => n.Count()).Select(o => o.Key).Distinct().Take(20).ToArray();

                List<SocketUser> topUsers = new List<SocketUser>();

                foreach (var id in topUserIds)
                {
                    var tUser = await guild.GetUserAsync(id);
                    if (tUser == null || tUser.IsBot || tUser.Username == "PhoBot")
                        continue;

                    topUsers.Add((SocketUser)tUser);
                    if (topUsers.Count >= 5)
                        break;
                }
                var sb = new StringBuilder();
                sb.AppendLine($"{messages.Count} messages in `#{channel.Name}`. Top user is {topUsers[0].NickOrUserName()} with {messages.Count(m => m.AuthorId == topUserIds[0])} messages.");
                sb.AppendLine($"{messages.Count(m => DateTime.Now - m.Timestamp < TimeSpan.FromDays(1))}/{messages.Count(m => DateTime.Now - m.Timestamp < TimeSpan.FromDays(7))}/{messages.Count(m => DateTime.Now - m.Timestamp < TimeSpan.FromDays(30))} messages this D/W/M");
                sb.AppendLine($"Top 5 users: {string.Join(", ", topUsers.Select(u => u.NickOrUserName()))}");
                return sb.ToString();
            }

            var user = message.MentionedUsers.FirstOrDefault();
            if (user != null)
            {
                Dictionary<ulong,int> messages = new Dictionary<ulong, int>();
                foreach (var c in RexBotCore.Instance.KeenGuild.Channels)
                {
                    if (!(c is ISocketMessageChannel))
                        continue;

                    try
                    {
                        messages[c.Id] = RexBotCore.Instance.DBManager.GetMessages(c.Id).Count(m => m.AuthorId == user.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                List<ulong> ids = messages.OrderByDescending(e => e.Value).Select(r => r.Key).ToList();
                IGuildChannel activeChannel = null;

                foreach (var id in ids)
                {
                    if (Utilities.CTGChannels.Contains(id) && !message.CTG())
                        continue;
                    activeChannel = await guild.GetChannelAsync(id);
                    Console.WriteLine(ids.IndexOf(id));
                    break;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"User {user.NickOrUserName()} has sent {messages.Sum(e => e.Value)} messages.");
                if (activeChannel != null)
                    sb.AppendLine($"Most active channel {activeChannel.Name} with {messages[activeChannel.Id]} messages.");
                return sb.ToString();
            }

            return "You must specify a channel or a user.";
        }
    }
}
