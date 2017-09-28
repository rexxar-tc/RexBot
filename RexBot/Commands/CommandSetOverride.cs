using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandSetOverride:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!setoverride";
        public string HelpText => "Sets command permission override. `!setoverride [!command] [true/false] @user";
        public Embed HelpEmbed { get; }
        public async Task<string> Handle(SocketMessage message)
        {
            var args = Utilities.ParseCommand(message.Content);

            var id = message.MentionedUsers.First().Id;
            string command = args[0];
            bool val = bool.Parse(args[1]);

            if(!RexBotCore.Instance.PermissionOverrides.ContainsKey(command))
                RexBotCore.Instance.PermissionOverrides.Add(command,new Dictionary<ulong, bool>() { {id, val} });
            RexBotCore.Instance.PermissionOverrides[command][id] = val;

            RexBotCore.Instance.SaveOverrides();

            return $"Set override for {message.MentionedUsers.First().NickOrUserName()} on command {command} to {val}.";
        }
    }
}
