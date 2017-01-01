using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord1Test;

namespace RexBot.Commands
{
    class CommandAddCommand : IChatCommand
    {
        public bool IsPublic => false;
        public string Command => "!addcommand";
        public string HelpText => "";

        public string Handle(string args)
        {
            var splits = args.Substring(Command.Length+1).Split(',');
            if (splits.Length != 4)
            {
                return "Wrong number of arguments! Need 4, got " + splits.Length;
            }

            string newCommand = splits[0];
            string response = splits[1];
            bool isPublic;
            bool image;

            if (!bool.TryParse(splits[2], out isPublic))
                return "Couldn't parse IsPublic!";

            if (!bool.TryParse(splits[3], out image))
                return "Couldn't parse ImageResponse!";

            RexBotCore.Instance.InfoCommands.Add(new RexBotCore.InfoCommand(newCommand, response, isPublic, image));
            RexBotCore.Instance.SaveCommands();

            return $"Adding command {newCommand} with response {response}. IsPublic: {isPublic} ImageResponse: {image}";
        }
    }
}
