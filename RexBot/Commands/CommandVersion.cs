using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandVersion:IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!version";
        public string HelpText => "Gets the latest version of Space and Medieval Engineers";
        public DiscordEmbed HelpEmbed { get; }
        public async Task<string> Handle(DiscordMessage message)
        {
            var em = new DiscordEmbedBuilder();
            em.Color = Utilities.RandomColor();
            em.Author = new DiscordEmbedBuilder.EmbedAuthor()
                        {
                            IconUrl = RexBotCore.Instance.KeenGuild.IconUrl,
                            Name = "Latest versions as reported by Keen news."
                        };

            em.AddInlineField("Space Engineers", RexBotCore.Instance.Jira.LastSpaceVersion);
            em.AddInlineField("Medieval Engineers", RexBotCore.Instance.Jira.LastMedievalVersion);

            em.Footer = new DiscordEmbedBuilder.EmbedFooter()
                        {
                            Text = "Version numbers may not include hotfixes or minor releases."
                        };

            await message.Channel.SendMessageAsync("", embed: em.Build());
            return null;
        }
    }
}
