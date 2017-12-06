//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DSharpPlus.Entities;

//namespace RexBot.Commands
//{
//    public class CommandSteamWsAutoEmbed : IChatCommand
//    {
//        public const string SteamIconUrl = "https://images.weserv.nl/?url=store.steampowered.com%2Ffavicon.ico";

//        public CommandAccess Access => CommandAccess.Public;
//        public string Command => "https://steamcommunity.com/sharedfiles/filedetails";
//        public string HelpText => "Automatically embeds workshop information after workshop links";
//        public DiscordEmbed HelpEmbed { get; }

//        public async Task<string> Handle(DiscordMessage message)
//        {
//            await CommandSteamWsEmbed.HandleInternal(message.Content, message, true, "syntax error");
//            return null;
//        }
//    }
//}