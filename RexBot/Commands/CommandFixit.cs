//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.WebSocket;

//namespace RexBot.Commands
//{
//    class CommandFixit : IChatCommand
//    {
//        public bool IsPublic => true;
//        public string Command => "!fixit";
//        public string HelpText => "Adds an item to the log for rexxar to fix. `!fixit list` lists current items.";
//        public async Task<string> Handle(SocketMessage message)
//        {
//            return "Fixit has been superceeded. Use !bugreport instead!";
//            var arg = Utilities.StripCommand(this, message.Content);
//            if (arg == null)
//            {
//                return $"Command not understood. Help for `!fixit`: ```{HelpText}```";
//            }

//            if (arg.Equals("list", StringComparison.CurrentCultureIgnoreCase))
//            {
//                string log;
//                try
//                {
//                    log = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fixit.log"));
//                }
//                catch
//                {
//                    return "Log not found :(";
//                }
//                return $"```{log}```";
//            }


//            if (arg.Length < 20)
//                return "No.";

//            string nick;
//            var chan = message.Channel as SocketGuildChannel;
//            if (chan != null)
//            {
//                nick = chan.Guild.GetUser(message.Author.Id).Nickname ?? message.Author.Username;
//            }
//            else
//                nick = message.Author.Username;

//            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fixit.log"), $"\r\n{DateTime.Now} @{nick}: {arg}");

//            return "Added to the fixit log.";
//        }
//    }
//}

