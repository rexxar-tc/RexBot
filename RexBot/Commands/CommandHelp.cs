using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandHelp : IChatCommand
    {
        public bool IsPublic => true;
        public string Command => "!help";
        public string HelpText => "Shows help. !help with no argument lists commands. !help (command) shows help for that command.";
        public async Task<string> Handle( SocketMessage message )
        {
            StringBuilder sb = new StringBuilder();

            bool isRexxar = message.Author.Id == RexBotCore.REXXAR_ID;

            if ( message.Content.Length == Command.Length )
            {
                sb.Append( message.Author.Mention );
                sb.AppendLine( "Available commands:```" );
                foreach ( var info in RexBotCore.Instance.InfoCommands )
                {
                    if ( info.IsPublic || isRexxar )
                        sb.Append( $"{info.Command}, " );
                }

                foreach ( var command in RexBotCore.Instance.ChatCommands )
                {
                    if ( command.IsPublic || isRexxar)
                        sb.Append( $"{command.Command}, " );
                }
                sb.Remove( sb.Length - 2, 2 );
                sb.AppendLine( "```" );
                sb.Append("Use `!help [command] for more info`");
            }
            else
            {
                string search = message.Content.Substring( Command.Length + 1 );
                var command = RexBotCore.Instance.ChatCommands.FirstOrDefault( c => c.Command.Equals( search, StringComparison.CurrentCultureIgnoreCase ) );
                if (command != null)
                {
                    if (command.IsPublic || isRexxar)
                        return command.HelpText;
                    else
                        return "You aren't allowed to use that command!";
                }

                foreach(var info in RexBotCore.Instance.InfoCommands)
                {
                    if (!info.Command.Equals(search, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    if ( info.IsPublic || isRexxar )
                        return "Responds with text or an image";
                    else
                        return "You aren't allowed to use that command!";
                }

                return $"Couldn't find command `{search}`";
            }

            return sb.ToString();
        }
    }
}

