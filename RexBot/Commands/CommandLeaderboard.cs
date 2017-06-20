using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandLeaderboard : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Developer;
        public string Command => "!leaderboard";
        public string HelpText => "";

        public async Task<string> Handle(SocketMessage message)
        {
            Task.Run(() => RunInternal(message));
            return "Processing. Please wait.";
        }

        private void RunInternal(SocketMessage message)
        {
            try
            {
                SocketGuild server;
                var id = Utilities.StripCommand(this, message.Content);
                if (id == null)
                    server = (message.Channel as SocketGuildChannel)?.Guild;
                else
                    server = RexBotCore.Instance.RexbotClient.GetGuild(ulong.Parse(id));

                if (server == null)
                    throw new Exception("fuck you and your cow");

                var messageCounts = new Dictionary<ulong, int>();

                var messages = new HashSet<IMessage>();

                long count = 0;

                foreach (ISocketMessageChannel channel in server.TextChannels)
                {
                    try
                    {
                        var reader = RexBotCore.Instance.DBManager.ExecuteQuery($"SELECT authorid FROM K{channel.Id}");
                        while (reader.Read())
                        {
                            var uid = ulong.Parse(((long)reader.GetValue(0)).ToString());
                            messageCounts.AddOrUpdate(uid, 1);
                            count++;
                        }
                        //var tmp = channel.GetMessagesAsync().Flatten().Result;
                        //bool inDate = true;
                        //while (inDate)
                        //{
                        //    Console.WriteLine($"Got {tmp.Count()} messages.");
                        //    foreach (var msg in tmp)
                        //    {
                        //        messageCounts.AddOrUpdate(msg.Author.Id, 1);
                        //        if (msg.Timestamp.Date.Day == DateTime.Now.Day+1)
                        //        {
                        //            inDate = false;
                        //            break;
                        //        }
                        //    }

                        //    tmp = channel.GetMessagesAsync(tmp.Last(), Direction.Before).Flatten().Result;
                        //    if (tmp.Count() == 0)
                        //        break;
                        //}
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                var sort = new List<MessageCount>();

                foreach (var e in messageCounts)
                {
                    sort.Add(new MessageCount() {user = e.Key, count = e.Value});
                }

                sort.Sort((b,a)=>a.count.CompareTo(b.count));

                var sb = new StringBuilder();

                sb.AppendLine($"Leaderboard for {server.Name} over all history:");

                List<SocketGuildUser> users = new List<SocketGuildUser>(25);

                foreach (var c in sort)
                {
                    var user = server.GetUser(c.user);
                    if (user == null || user.IsBot)
                        continue;

                    users.Add(user);

                    if (users.Count >= 25)
                        break;
                }

                for (int index = 0; index < Math.Min(users.Count, 25); index++)
                {
                    var user = users[index];
                    var c = sort.Find(mc => mc.user == user.Id);
                    
                    sb.Append($"#{index+1} ");
                    sb.Append(user.NickOrUserName()).Append(": ").Append(c.count);
                    sb.AppendLine();
                }

                sb.AppendLine($"Processed {count} messages.");

                var str = sb.ToString();
                Console.WriteLine(str);
                message.Channel.SendMessageAsync(str.Length > 2000 ? str.Substring(0, 2000) : str);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        struct MessageCount
        {
            public int count;
            public ulong user;
        }
    }
}
