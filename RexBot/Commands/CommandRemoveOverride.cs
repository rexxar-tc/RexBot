using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandRemoveOverride:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!removeoverride";
        public string HelpText => "Removes command override. `!removeoverride [!command] @user";
        public DiscordEmbed HelpEmbed { get; }
        public async Task<string> Handle(DiscordMessage message)
        {
            var args = Utilities.ParseCommand(message.Content);
            var id = message.MentionedUsers.First().Id;
            string command = args[0];

            Dictionary<ulong, bool> d;
            if (RexBotCore.Instance.PermissionOverrides.TryGetValue(command, out d))
            {
                d.Remove(id);
                RexBotCore.Instance.SaveOverrides();
                return $"Removed pemission override for {message.MentionedUsers.First().NickOrUserName()} from command {command}";
            }
            return $"No overrides found for {command}";
        }
    }
}
