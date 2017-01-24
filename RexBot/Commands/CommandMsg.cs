using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandMsg : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!msg";
        public string HelpText => "Sends a message to the given channel";
        public async Task<string> Handle( SocketMessage message )
        {
            var args = Utilities.ParseCommand( message.Content );

            ulong id;
            if ( !ulong.TryParse( args[0], out id ) )
                return "Can't parse channel ID!";

            foreach ( var guild in RexBotCore.Instance.RexbotClient.Guilds )
            {
                var channel = guild.GetChannel( id );
                if (channel == null)
                    continue;

                await ((ISocketMessageChannel)channel).SendMessageAsync( string.Join( " ", args, 1, args.Length - 1 ) );
                break;
            }

            return "Ok";
        }
    }
}
