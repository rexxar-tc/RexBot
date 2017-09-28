using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

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
        Embed HelpEmbed { get; }
        Task<string> Handle(SocketMessage message);
    }
}