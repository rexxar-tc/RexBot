using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandDevCount : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!devcount";
        public string HelpText => "Counts devs.";
        public DiscordEmbed HelpEmbed { get; }
        private const ulong DEV_ROLE = 125014635383357440ul;

        public async Task<string> Handle(DiscordMessage message)
        {
            int meCount=0;
            int seCount=0;
            int misCount=0;
            var users = await RexBotCore.Instance.KeenGuild.GetAllMembersAsync();

            foreach (var u in users)
            {
                if (!u.Roles.Any(r => r.Id == DEV_ROLE))
                    continue;
                var dev = u;
                //var dev = await RexBotCore.Instance.KeenGuild.GetMemberAsyncSafe(u.Id);
                if (dev == null)
                    continue;
                if (dev.Presence.Status == UserStatus.Offline)
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
