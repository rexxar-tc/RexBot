using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    internal class CommandAddCommand : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!addcommand";
        public string HelpText => "Adds info command. `!addcommand [!commandKey] \"[response]\" (imageResponse)`\r\n" +
                                  "ImageResponse embeds the given link as an image. This is optional and defaults to false.";

        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            string[] splits = Utilities.ParseCommand(message.Content);
            if ((splits.Length > 4) || (splits.Length < 2))
                return "Wrong number of arguments! Need 2-4, got " + splits.Length + ". " + string.Join(", ", splits);

            string newCommand = splits[0];
            string response = splits[1].Trim('"');
            bool isPublic = true;
            bool image = false;

            if(!message.Author.IsRexxar() && !newCommand.StartsWith("!"))
                return "All info commands must start with `!`, please try again.";
            
            if ((splits.Length > 2) && !bool.TryParse(splits[2], out image))
                return "Couldn't parse ImageResponse!";

            if (Utilities.HasAccess(CommandAccess.Rexxar, message.Author) && splits.Length > 3 && !bool.TryParse(splits[3], out isPublic))
                return "Couldn't parse IsPublic!";

            if (image)
            {
                if (message.Attachments.Any())
                {
                    if (message.Attachments.Count > 1)
                        return "Cannot handle multiple attachments!";
                    response = message.Attachments.First().Url;
                    //using (var client = new WebClient())
                    //{
                    //    Console.WriteLine("Downloading file " + response);
                    //    client.DownloadFile(message.Attachments.First().Url, response);
                    //    Console.WriteLine("Done.");
                    //}
                }
            }

            if (RexBotCore.Instance.InfoCommands.Any(c => c.Command.Equals(newCommand, StringComparison.CurrentCultureIgnoreCase)))
                return $"There is already a command with the key {newCommand}. Please try again!";

            if (RexBotCore.Instance.ChatCommands.Any(c => c.Command.Equals(newCommand, StringComparison.CurrentCultureIgnoreCase)))
                return $"There is already a system command with the key {newCommand}.";

            RexBotCore.Instance.InfoCommands.Add(new RexBotCore.InfoCommand(newCommand, response, message.Author.Id, isPublic, image));
            RexBotCore.Instance.SaveCommands();

            return $"Adding command {newCommand} with response {response}. ImageResponse: {image} IsPublic: {isPublic}";
        }
    }
}