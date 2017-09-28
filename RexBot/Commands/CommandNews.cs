using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandNews : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!news";
        public string HelpText => "Gets news about Keen games.";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            if(RexBotCore.Instance.Jira.LastSpaceVersion == null)
                RexBotCore.Instance.Jira.GetNews();
            
            var em = new EmbedBuilder();
            em.Author = new EmbedAuthorBuilder()
                        {
                            IconUrl = RexBotCore.Instance.KeenGuild.IconUrl,
                            Name = "Keen Software House News",
                        };
            em.AddField($"Space Engineers {RexBotCore.Instance.Jira.LastSpaceVersion}", RexBotCore.Instance.Jira.SpaceNews);
            em.AddField($"Medieval Engineers {RexBotCore.Instance.Jira.LastMedievalVersion}", RexBotCore.Instance.Jira.MedievalNews);

            em.Color = Utilities.RandomColor();
            await message.Channel.SendMessageAsync(message.Author.Mention, embed: em.Build());
            return null;
        }
    }
}
