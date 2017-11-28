//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.WebSocket;

//namespace RexBot.Commands
//{
//    class CommandUpdate:IChatCommand
//    {
//        public CommandAccess Access => CommandAccess.Rexxar;
//        public string Command => "!update";
//        public string HelpText => "Updates RexBot";
//        public DiscordEmbed HelpEmbed { get; }

//        private const string BAT = @"@ECHO OFF
//SLEEP 20
//DEL RexBot.exe
//REN RexBot.new RexBot.exe
//RexBot.exe";

//        public async Task<string> Handle(DiscordMessage message)
//        {
//            var file = new FileInfo(@"\\192.168.1.141\e\GitHub\RexBot\RexBot\bin\Debug\RexBot.exe");
//                var newFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"RexBot.new"));

//            if (!file.Exists)
//                return "Source doesn't exist! :(";
            
//            if(newFile.Exists)
//                newFile.Delete();

//            File.Copy(@"\\192.168.1.141\e\GitHub\RexBot\RexBot\bin\Debug\RexBot.exe", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RexBot.new"));

//            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");

//            if (!File.Exists(path))
//                File.WriteAllText(path, BAT);

//            Process.Start(path);
//            Environment.Exit(1);
//            return "Goodbye.";
//        }
//    }
//}
