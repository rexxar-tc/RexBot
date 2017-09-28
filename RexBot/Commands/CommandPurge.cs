using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandPurge : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!purge";
        public string HelpText => "Purges all chat history in a channel.";
        public Embed HelpEmbed => null;

        public async Task<string> Handle(SocketMessage message)
        {
#pragma warning disable 4014
            Task.Run(async() =>
                     {
                         var c = message.MentionedChannels.First() as ISocketMessageChannel;

                         var messages = await c.GetMessagesAsync().Flatten();
                         int count = 0;
                         while (messages.Any())
                         {
                             try
                             {
                                 foreach (var m in messages)
                                 {
                                     await m.DeleteAsync();
                                     count++;
                                 }
                                 messages = await c.GetMessagesAsync().Flatten();
                                 Thread.Sleep(1000);
                             }
                             catch (RateLimitedException)
                             {
                                 Console.WriteLine("Rate limited. Trying again in 60s");
                                 Thread.Sleep(60000);
                                 continue;
                             }
                         }
                         await message.Channel.SendMessageAsync($"Deleted {count} messages");
                     });
#pragma warning restore 4014
            return null;
        }
    }
}
