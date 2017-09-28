using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Discord;
using Discord.WebSocket;

namespace RexBot.Commands
{
    class CommandPlayers : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!players";
        public string HelpText => "Gets the count of people playing our games!";
        public Embed HelpEmbed { get; }

        public async Task<string> Handle(SocketMessage message)
        {
            if (RexBotCore.Instance.Jira.LastSpaceVersion == null)
                RexBotCore.Instance.Jira.GetNews();

            var em = new EmbedBuilder();
            em.Author = new EmbedAuthorBuilder()
            {
                IconUrl = RexBotCore.Instance.KeenGuild.IconUrl,
                Name = "Keen Software House: Online Players",
            };
            long se;
            long me;
            using (var client = new WebClient())
            {

                var data = client.DownloadString("https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?format=xml&appid=244850");
                var document = XDocument.Parse(data);
                var str = (string)((IEnumerable<object>)document.XPathEvaluate("/response/player_count"))
                    .Cast<XElement>()
                    .FirstOrDefault();
                long.TryParse(str, out se);
                data = client.DownloadString("https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?format=xml&appid=333950");
                document = XDocument.Parse(data);
                str = (string)((IEnumerable<object>)document.XPathEvaluate("/response/player_count"))
                    .Cast<XElement>()
                    .FirstOrDefault();
                long.TryParse(str, out me);
            }
            em.AddField($"Space Engineers", se);
            em.AddField($"Medieval Engineers", me);

            em.Color = Utilities.RandomColor();
            await message.Channel.SendMessageAsync(message.Author.Mention, embed: em.Build());
            return null;
        }
    }
}
