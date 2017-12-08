using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandListAutos:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Modder;
        public string Command => "!listautos";
        public string HelpText => "Lists autocommands so they can be removed";
        public DiscordEmbed HelpEmbed { get; }
        public async Task<string> Handle(DiscordMessage message)
        {
            var em = new DiscordEmbedBuilder();
            em.Color = Utilities.RandomColor();
            em.Title = "Auto Commands";
            for (var i = 0; i < RexBotCore.Instance.AutoCommands.Count; i++)
            {
                var auto = RexBotCore.Instance.AutoCommands[i];
                em.AddField($"{i}: {auto.Pattern}", $"{(await RexBotCore.Instance.RexbotClient.GetUserAsync(auto.Author)).NickOrUserName()}: {auto.Response}");
            }
            
            await message.Channel.SendMessageAsync(embed:em.Build());
            return null;
        }
    }
}
