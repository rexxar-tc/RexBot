using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    public enum CommandAccess
    {
        None,
        Public,
        Modder,
        Moderator,
        Developer,
        Rexxar,
    }
    public interface IChatCommand
    {
        CommandAccess Access { get; }
        string Command { get; }
        string HelpText { get; }
        DiscordEmbed HelpEmbed { get; }
        Task<string> Handle(DiscordMessage message);
    }
}