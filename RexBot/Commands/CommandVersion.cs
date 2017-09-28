using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandVersion:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!version";
        public string HelpText => "Gets the latest version of Space and Medieval Engineers";
        public Embed HelpEmbed { get; }
        public async Task<string> Handle(SocketMessage message)
        {
            var em = new EmbedBuilder();
            em.Color = Utilities.RandomColor();
            em.Author = new EmbedAuthorBuilder()
                        {
                            IconUrl = RexBotCore.Instance.KeenGuild.IconUrl,
                            Name = "Latest versions as reported by Keen news."
                        };

            em.AddInlineField("Space Engineers", RexBotCore.Instance.Jira.LastSpaceVersion);
            em.AddInlineField("Medieval Engineers", RexBotCore.Instance.Jira.LastMedievalVersion);

            em.Footer = new EmbedFooterBuilder()
                        {
                            Text = "Version numbers may not include hotfixes or minor releases."
                        };

            await message.Channel.SendMessageAsync("", embed: em.Build());
            return null;
        }
    }
}
