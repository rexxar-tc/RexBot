using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class CommandHelp : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!help";
        public string HelpText => "Shows help. !help with no argument lists commands. !help (command) shows help for that command.";

        public async Task<string> Handle(SocketMessage message)
        {
            var sb = new StringBuilder();

            bool isRexxar = message.Author.Id == RexBotCore.REXXAR_ID;

            if (message.Content.Length == Command.Length)
            {
                sb.AppendLine("Available commands:```");
                foreach (RexBotCore.InfoCommand info in RexBotCore.Instance.InfoCommands)
                    if (info.IsPublic || isRexxar)
                        sb.Append($"{info.Command}, ");

                foreach (IChatCommand command in RexBotCore.Instance.ChatCommands)
                    if (command.HasAccess(message.Author))
                        sb.Append($"{command.Command}, ");
                sb.Remove(sb.Length - 2, 2);
                sb.AppendLine("```");
                sb.Append("Use `!help [command] for more info`");
            }
            else
            {
                string search = message.Content.Substring(Command.Length + 1);
                if (!search.StartsWith("!"))
                    search = "!" + search;
                IChatCommand command = RexBotCore.Instance.ChatCommands.FirstOrDefault(c => c.Command.Equals(search, StringComparison.CurrentCultureIgnoreCase));
                if (command != null)
                    if (command.HasAccess(message.Author))
                        return command.HelpText;
                    else
                        return "You aren't allowed to use that command!";

                foreach (RexBotCore.InfoCommand info in RexBotCore.Instance.InfoCommands)
                {
                    if (!info.Command.Equals(search, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    if (info.IsPublic || isRexxar)
                        return "Responds with text or an image";
                    return "You aren't allowed to use that command!";
                }

                return $"Couldn't find command `{search}`";
            }

            return sb.ToString();
        }
    }
}