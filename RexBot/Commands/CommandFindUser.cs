using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandFindUser:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Developer;
        public string Command => "!finduser";
        public string HelpText => "Finds the user who opened the given Jira ticket";
        public Embed HelpEmbed { get; }
        public async Task<string> Handle(SocketMessage message)
        {
            var key = Utilities.StripCommand(this, message.Content);
            if (string.IsNullOrEmpty(key))
                return "You must specify a key!";

            foreach (var issue in RexBotCore.Instance.Jira.CachedIssues)
            {
                if (issue.Key != key)
                    continue;
                return $"Reporter for {key} is <@{issue.Metadata.ReporterId}>";
            }

            return $"Couldn't find {key}";
        }
    }
}
