using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandAddCommand : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!addcommand";
        public string HelpText => "Adds info command";

        public string Handle(SocketMessage message)
        {
            var splits = Utilities.ParseCommand( message.Content );
            if (splits.Length > 4 || splits.Length < 2)
            {
                return "Wrong number of arguments! Need 2-4, got " + splits.Length + ". " + string.Join( ", ", splits );
            }

            string newCommand = splits[0];
            string response = splits[1];
            bool isPublic = true;
            bool image = false;

            if (splits.Length > 2 && !bool.TryParse(splits[2], out isPublic))
                return "Couldn't parse IsPublic!";

            if (splits.Length > 3 && !bool.TryParse(splits[3], out image))
                return "Couldn't parse ImageResponse!";

            if ( image )
            {
                if ( message.Attachments.Any() )
                {
                    if ( message.Attachments.Count > 1 )
                        return "Cannot handle multiple attachments!";
                    using ( WebClient client = new WebClient() )
                    {
                        Console.WriteLine( "Downloading file " + response );
                        client.DownloadFile( message.Attachments.First().Url, response );
                        Console.WriteLine( "Done." );
                    }
                }
            }

            RexBotCore.Instance.InfoCommands.Add(new RexBotCore.InfoCommand(newCommand, response, isPublic, image));
            RexBotCore.Instance.SaveCommands();

            return $"Adding command {newCommand} with response {response}. IsPublic: {isPublic} ImageResponse: {image}";
        }
    }
}
