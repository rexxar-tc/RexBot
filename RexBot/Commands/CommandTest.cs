﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.EmojiTools;
using Discord.Rest;
using Discord.WebSocket;

namespace RexBot.Commands
{
    public class CommandTest : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!test";
        public string HelpText => "";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in RexBotCore.Instance.KeenGuild.TextChannels)
            {
                sb.AppendLine($"{c.Id} : {c.Name}");
            }

            return "Tested.";
            //await RexBotCore.Instance.Jira.Update();
            //return "Okay.";
        }
    }
}