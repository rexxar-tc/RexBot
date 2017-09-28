using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandDevCount : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!devcount";
        public string HelpText => "Counts devs.";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            int meCount=0;
            int seCount=0;
            int misCount=0;
            
            var devs = RexBotCore.Instance.KeenGuild.Users.Where(u => u.Roles.Any(r => r.Id == 125014635383357440ul));

            foreach (var u in devs)
            {
                var dev = RexBotCore.Instance.KeenGuild.GetUser(u.Id);
                if (dev.Status == UserStatus.Offline)
                    continue;
                if (dev.NickOrUserName().StartsWith("[SE]"))
                    seCount++;
                else if (dev.NickOrUserName().StartsWith("[ME]"))
                    meCount++;
                else
                {
                    Console.WriteLine(dev.NickOrUserName());
                    misCount++;
                }
            }

            return $"Online Developers:\r\n" +
                   $"<:me:230274466150612992>: {meCount}\r\n" +
                   $"<:se:230274416502767617>: {seCount}\r\n" +
                   $"<:clang:230256527846539264>: {misCount}";
        }
    }
}
