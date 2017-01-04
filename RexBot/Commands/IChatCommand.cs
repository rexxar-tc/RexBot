using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RexBot
{
    public interface IChatCommand
    {
        bool IsPublic { get; }
        string Command { get; }
        string HelpText { get; }
        Task<string> Handle(SocketMessage message);
    }
}
