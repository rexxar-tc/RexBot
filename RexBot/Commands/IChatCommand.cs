using System.Threading.Tasks;
using Discord.WebSocket;

namespace RexBot.Commands
{
    public enum CommandAccess
    {
        None,
        Public,
        Modder,
        Developer,
        Rexxar,
    }
    public interface IChatCommand
    {
        CommandAccess Access { get; }
        string Command { get; }
        string HelpText { get; }
        Task<string> Handle(SocketMessage message);
    }
}