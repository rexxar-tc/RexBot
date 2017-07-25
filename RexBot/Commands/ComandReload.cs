﻿using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    internal class ComandReload : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!reloadcommands";
        public string HelpText => "Reloads commands from disk";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            RexBotCore.Instance.InfoCommands.Clear();
            RexBotCore.Instance.LoadCommands();
            return "Done.";
        }
    }
}