using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandLeaderboard : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!leaderboard";
        public string HelpText => "";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            try
            {
                SocketGuild server = RexBotCore.Instance.KeenGuild;
                var start = DateTime.Now;
                var messageCounts = new ConcurrentDictionary<ulong, int>();
                
                var em = new EmbedBuilder(); long count = 0;

                var channels = new List<ISocketMessageChannel>();

                if (message.MentionedChannels != null && message.MentionedChannels.Count > 0)
                {
                    var c = message.MentionedChannels.First() as ISocketMessageChannel;
                    if (c == null)
                    {
                        return "Error";
                    }
                    channels.Add(c);
                    em.Title = $"#{c.Name}";
                }
                else
                    channels.AddRange(server.TextChannels);
                
                Parallel.ForEach(channels, channel =>
                                           {
                                               try
                                               {
                                                   var lCount = new Dictionary<ulong, int>();
                                                   var reader = RexBotCore.Instance.DBManager.ExecuteQuery($"SELECT authorid ,Count(*) FROM K{channel.Id} GROUP BY authorid");
                                                   while (reader.Read())
                                                   {
                                                       var uid = (ulong)reader.GetInt64(0);
                                                       lCount.AddOrUpdate(uid, reader.GetInt32(1)); 
                                                   }
                                                   foreach (var lc in lCount)
                                                   {
                                                       messageCounts.AddOrUpdate(lc.Key, u => lc.Value, (u, i) => i + lc.Value);
                                                       Interlocked.Add(ref count, lc.Value);
                                                   }
                                               }
                                               catch (Exception ex)
                                               {
                                                   Console.WriteLine(ex);
                                               }
                                           });

                var sort = new List<MessageCount>();

                foreach (var e in messageCounts)
                {
                    sort.Add(new MessageCount() {user = e.Key, count = e.Value});
                }

                sort.Sort((b,a)=>a.count.CompareTo(b.count));
                
                List<SocketUser> users = new List<SocketUser>(25);

                foreach (var c in sort)
                {
                    var user = RexBotCore.Instance.RexbotClient.GetUser(c.user);
                    if (user == null || user.IsBot)
                        continue;

                    users.Add(user);

                    if (users.Count >= 25)
                        break;
                }

                em.Author = new EmbedAuthorBuilder()
                            {
                                IconUrl = RexBotCore.Instance.KeenGuild.IconUrl,
                                Name = "Leaderboard for Keen Software House"
                            };

                for (int index = 0; index < Math.Min(users.Count, 25); index++)
                {
                    var user = users[index];
                    var c = sort.Find(mc => mc.user == user.Id);
                    
                    em.Fields.Add(new EmbedFieldBuilder()
                                  {
                                      IsInline=true,
                                      Name = $"#{index+1}: {user.NickOrUserName()}",
                                      Value = $"{c.count:n0} Messages"
                                  });
                }
                
                em.Footer = new EmbedFooterBuilder()
                            {
                                Text = $"Processed {count:n0} messages in {(DateTime.Now - start).TotalMilliseconds:n0}ms."
                            };
                
               await message.Channel.SendMessageAsync("", embed:em.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        struct MessageCount
        {
            public int count;
            public ulong user;
        }
    }
}
