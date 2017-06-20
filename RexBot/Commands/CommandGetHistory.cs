using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.API;
using Discord.WebSocket;
using Timer = System.Timers.Timer;

namespace RexBot.Commands
{
    class CommandGetHistory:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!gethistory";
        public string HelpText => "";
        public async Task<string> Handle(SocketMessage message)
        {
            if (message.Content.Contains("status"))
                return $"Downloaded {count} messages total. Current channel: {_currentChannel?.Name ?? "UNKN"} Oldest message local: {new DateTime(minDateLocal)} Oldest message global: {new DateTime(minDate)}";

            if (message.Content.Contains("update"))
            {
                var splits = message.Content.Split(' ');
                timer.Interval = double.Parse(splits.Last());
                return "Done.";
            }

            if (message.Content.Contains("stop"))
            {
                
            }

            if (updating)
                return "Already running.";

            updating = true;
            Task.Run(()=>RunInternal(message));
            return "Processing started, see you in a few days.";
        }

        private static bool cancel;
        private static long count;
        private static bool updating;
        private static long minDate = long.MaxValue;
        private static long minDateLocal = long.MaxValue;
        private List<IMessage> _messages = new List<IMessage>();
        private ISocketMessageChannel _currentChannel;

            Timer timer = new Timer(60 * 1000);
        public async Task RunInternal(SocketMessage message)
        {
            SocketGuild server = RexBotCore.Instance.RexbotClient.GetGuild(125011928711036928);

            if (server == null)
                throw new Exception("fuck you and your cow");
            
            timer.AutoReset = true;
            timer.Elapsed += (sender, args) => Console.WriteLine($"Downloaded {count} messages total. Current channel: {_currentChannel?.Name ?? "UNKN"} Oldest message global: {new DateTime(minDate)} Oldest message local: {new DateTime(minDateLocal)} UTC Queue: {_messages.Count}");
            timer.Start();

            var DBTimer = new Timer(120000);
            DBTimer.AutoReset = true;
            DBTimer.Elapsed += (sender, args) =>
                               {
                                   lock (_messages)
                                   {
                                       Console.WriteLine($"[{DateTime.Now.ToString("hh:mm:ss:ffff tt")}] starting DB dump");
                                       RexBotCore.Instance.DBManager.AddMessages(_messages);
                                       Console.WriteLine($"[{DateTime.Now.ToString("hh:mm:ss:ffff tt")}] dumped {_messages.Count} to DB");

                                       _messages.Clear();
                                   }
                                   if (!updating)
                                       DBTimer.Stop();
                               };
            DBTimer.Start();

            var channels = server.TextChannels;

            foreach (var c in channels)
            {
                try
                {
                    var channel = c as ISocketMessageChannel;
                    if (channel == null)
                        continue;

                    Console.WriteLine($"Switching to {channel.Name}");

                    var users = await channel.GetUsersAsync().Flatten();
                    if (!users.Any(u => u.Id == RexBotCore.REXBOT_ID))
                    {
                        Console.WriteLine("No permission here :(");
                        continue;
                    }

                    _currentChannel = channel;
                    var oldest = RexBotCore.Instance.DBManager.GetOldestMessageID(c.Id);
                    IMessage oldestMsg = null;
                    if (oldest > 0)
                        oldestMsg = await c.GetMessageAsync(oldest);
                    IEnumerable<IMessage> tmp;
                    if (oldestMsg != null)
                        tmp = await channel.GetMessagesAsync(oldestMsg.Id, Direction.Before).Flatten();
                    else
                        tmp = await channel.GetMessagesAsync().Flatten();
                    minDateLocal = long.MaxValue;
                    while (true)
                    {
                        if (!tmp.Any())
                            break;
                        foreach (var msg in tmp)
                        {
                            //RexBotCore.Instance.DBManager.AddMessage(msg);
                            lock(_messages)
                                _messages.Add(msg);
                            count++;
                            if (msg.Timestamp.UtcTicks < minDate)
                                minDate = msg.Timestamp.UtcTicks;
                            if (msg.Timestamp.UtcTicks < minDateLocal)
                                minDateLocal = msg.Timestamp.UtcTicks;
                        }

                        tmp = await channel.GetMessagesAsync(tmp.First(m=>m.Timestamp.UtcTicks == tmp.Min(n => n.Timestamp.UtcTicks)).Id, Direction.Before).Flatten();
                        if (!tmp.Any())
                            break;
                        //Thread.Sleep(500);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            await message.Channel.SendMessageAsync($"{message.Author.Mention} Done! Finally!");
            updating = false;
        }
        
    }
}
