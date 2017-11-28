using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandNews : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!news";
        public string HelpText => "Gets news about Keen games.";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            if(RexBotCore.Instance.Jira.LastSpaceVersion == null)
                RexBotCore.Instance.Jira.GetNews();
            
            var em = new DiscordEmbedBuilder();
            em.Author = new DiscordEmbedBuilder.EmbedAuthor()
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
