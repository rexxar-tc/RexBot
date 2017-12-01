using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandRActivity : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!ractivity";
        public string HelpText { get; }
        public DiscordEmbed HelpEmbed => _help.Value;
        private Lazy<DiscordEmbed> _help = new Lazy<DiscordEmbed>(CreateLazy);

        private static DiscordEmbed CreateLazy()
        {
            var em = new DiscordEmbedBuilder();
            em.Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                IconUrl = RexBotCore.Instance.RexbotClient.CurrentUser.AvatarUrl,
                Name = "!activity"
            };
            em.Description = "Sets RexBot's activity. Available subcommands:";

            em.AddField("playing",
                        "Set's the bot's status to **Playing**");
            em.AddField("listening",
                        "Set's the bot's status to **Listening To**");
            em.AddField("watching",
                        "Set's the bot's status to **Watching**");
            //em.AddField("streaming",
            //            "Set's the bot's status to **Streaming**");

            return em.Build();
        }

        public async Task<string> Handle(DiscordMessage message)
        {
            string r = Utilities.StripCommand(this, message.Content);
            var s = r.Split(new[] { ' ' }, 2);
            string c = s[0].ToLower();
            string arg = s[1];

            ActivityType type;

            switch (c)
            {
                case "playing":
                    type = ActivityType.Playing;
                    break;
                case "listening":
                    type = ActivityType.ListeningTo;
                    break;
                case "watching":
                    type = ActivityType.Watching;
                    break;
                //case "streaming":
                //    type = ActivityType.Streaming;
                //    break;
                default:
                    return $"Could not parse activity {c}";
            }

            await RexBotCore.Instance.RexxarClient.UpdateStatusAsync(new DiscordActivity(arg, type));

            return $"Set activity to `{type} {arg}`";
        }
    }
}
