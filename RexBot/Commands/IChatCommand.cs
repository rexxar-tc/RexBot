using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RexBot
{
    public interface IChatCommand
    {
        bool IsPublic { get; }
        string Command { get; }
        string HelpText { get; }
        string Handle(string args);
    }
}
