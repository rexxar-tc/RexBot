using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using RexBot.Commands;

namespace RexBot.AutoCommands
{
    public interface IAutoCommand
    {
        //CommandAccess Access { get; }
        Regex Pattern { get; }
        Task<string> Handle(DiscordMessage message);
    }
}
