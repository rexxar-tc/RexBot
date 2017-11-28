using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandLeaveGuild:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!leaveguild";
        public string HelpText => "Removes rexbot from the target guild";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            var arg = Utilities.StripCommand(this, message.Content);
            ulong id = ulong.Parse(arg);

            var guild = await RexBotCore.Instance.RexbotClient.GetGuildAsync(id);
            await guild.LeaveAsync();
            return "Left " + guild.Name;
        }
    }
}
