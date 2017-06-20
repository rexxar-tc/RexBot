using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandLeaveGuild:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!leaveguild";
        public string HelpText => "Removes rexbot from the target guild";
        public async Task<string> Handle(SocketMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            ulong id = ulong.Parse(arg);

            var guild = RexBotCore.Instance.RexbotClient.GetGuild(id);
            await guild.LeaveAsync();
            return "Left " + guild.Name;
        }
    }
}
