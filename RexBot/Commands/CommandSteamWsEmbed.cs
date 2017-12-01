using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using RestSharp.Extensions.MonoHttp;
using RexBot.SteamWebApi;

namespace RexBot.Commands
{
    public class CommandSteamWsEmbed : IChatCommand
    {
        public const string SteamIconUrl = "https://images.weserv.nl/?url=store.steampowered.com%2Ffavicon.ico";

        private const int SHORT_COLLECTION_ITEMS = 0;
        private const int LONG_COLLECTION_ITEMS = 10;
        private const int SHORT_DESCRIPTION_LENGTH = 0;
        private const int LONG_DESCRIPTION_LENGTH = 300;

        public CommandAccess Access => CommandAccess.Public;
        public string Command => "!ws";
        public string HelpText => "Gets information about a steam workshop item";
        public DiscordEmbed HelpEmbed { get; }

        private readonly string Syntax;

        public CommandSteamWsEmbed()
        {
            Syntax = $"invalid syntax, expected {Command} [workshop id | workshop url]";
        }

        public async Task<string> Handle(DiscordMessage message)
        {
            if (message.Content.Length < Command.Length)
                return Syntax;
            return await HandleInternal(message.Content.Substring(Command.Length).Trim(), message, false, Syntax);
        }

        public static async Task<string> HandleInternal(string content, DiscordMessage message, bool @short,
            string syntaxMessage)
        {
            content = content.Trim();
            // syntax forms:
            // https://steamcommunity.com/sharedfiles/filedetails/?id=[id]
            // <https://steamcommunity.com/sharedfiles/filedetails/?id=[id]>
            // [id]
            ulong wsId;
            if (!ulong.TryParse(content, out wsId))
            {
                if (content.StartsWith("<"))
                {
                    if (!content.EndsWith(">"))
                        return syntaxMessage + " (no closing bracket)";
                    content = content.Substring(1, content.Length - 2).Trim();
                }
                var firstQuest = content.IndexOf('?');
                if (firstQuest == -1)
                    return syntaxMessage + " (no GET parameters)";
                var data = HttpUtility.ParseQueryString(content.Substring(firstQuest));
                var id = data.Get("id")?.Trim();
                if (id == null)
                    return syntaxMessage + " (no id in URL)";
                if (!ulong.TryParse(id, out wsId))
                    return syntaxMessage + " (id isn't a ulong)";
            }
            DiscordEmbed embed;
            try
            {
                embed = await MakeEmbed(wsId, @short);
            }
            catch (NoSuchItemException)
            {
                embed = null;
            }
            catch (ProfaneItemException e)
            {
                return e.Message;
            }
            if (embed == null)
            {
                return $"Unknown workshop item <https://steamcommunity.com/sharedfiles/filedetails/?id={wsId}>";
            }
            await message.Channel.SendMessageAsync(null, false, embed);
            return null;
        }

        public class ProfaneItemException : Exception
        {
            public ProfaneItemException(string s) : base(s)
            {
            }
        }

        public class NoSuchItemException : Exception
        {
        }

        public static async Task<DiscordEmbed> MakeEmbed(ulong wsId, bool @short = false)
        {
            var fileInfo = await RexBotCore.Instance.SteamWebApi.QueryPublishedFile(wsId);
            if (fileInfo == null)
                throw new NoSuchItemException();

            var collectionInfo = await RexBotCore.Instance.SteamWebApi.QueryCollection(wsId);
            var creatorInfo = await RexBotCore.Instance.SteamWebApi.QueryPlayerDetails(fileInfo.Creator);
            var gameInfo = await RexBotCore.Instance.SteamWebApi.QueryGame(fileInfo.ConsumerAppId);

            if (Utilities.HasProfanity(fileInfo.Title))
                throw new ProfaneItemException("Profanity in workshop item title");
            var creatorName = creatorInfo?.PersonaName ?? creatorInfo?.RealName;
            if (creatorName != null && Utilities.HasProfanity(creatorName))
                throw new ProfaneItemException("Profanity in creator name");
            if (fileInfo.Banned != 0)
                throw new ProfaneItemException("Workshop item is banned");

            var builder = new DiscordEmbedBuilder();
            var wsUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + wsId;
            builder.Title = fileInfo.Title;
            builder.Color = DiscordColor.NotQuiteBlack;
            builder.Url = wsUrl;
            builder.Footer = new DiscordEmbedBuilder.EmbedFooter() {IconUrl = SteamIconUrl, Text = "Steam Workshop"};
            var urlSane = fileInfo.PreviewUrl;
            if (urlSane != null && urlSane.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                urlSane = urlSane.Substring(8);
            else if (urlSane != null && urlSane.StartsWith("http://"))
                urlSane = urlSane.Substring(7);
            else
                urlSane = null;
            if (urlSane != null)
            {
                var url = "https://images.weserv.nl/?url=" + HttpUtility.UrlEncode(urlSane);
                if (@short)
                    builder.ThumbnailUrl = url;
                else
                    builder.ImageUrl = url;
            }
            builder.AddField("Creator", creatorName ?? $"user {fileInfo.Creator}", true);
            builder.AddField("Game", gameInfo?.Name ?? $"game {fileInfo.ConsumerAppId}", true);
            if (collectionInfo != null)
            {
                var count = @short ? SHORT_COLLECTION_ITEMS : LONG_COLLECTION_ITEMS;
                var total = collectionInfo.Items.Count();
                if (count == 0 || total == 0)
                {
                    builder.AddField("Collection Size", total == 0 ? "Empty" : total.ToString(), true);
                }
                else
                {
                    var written = 0;
                    var items = collectionInfo.Items.ToList();
                    var mods =new StringBuilder();
                    foreach (var item in items)
                    {
                        if (written > count)
                            break;
                        var details = await RexBotCore.Instance.SteamWebApi.QueryPublishedFile(item.FileId);
                        if (details == null)
                            continue;
                        if (Utilities.HasProfanity(details.Title))
                            throw new ProfaneItemException("Profanity in workshop collection item title");
                        if (details.Banned != 0)
                            throw new ProfaneItemException("Workshop collection item is banned");
                        var content =
                            $"[{details.Title}](https://steamcommunity.com/sharedfiles/filedetails/?id={item.FileId})";
                        if (mods.Length + 1 + content.Length > 900)
                            break;
                        if (written > 0)
                            mods.Append("\n");
                        written++;
                        mods.Append(content);
                    }
                    if (items.Count > written)
                    {
                        if (mods.Length > 0)
                            mods.Append("\n");
                        mods.Append($"...and {items.Count - written} more");
                    }
                    builder.AddField("Items", mods.ToString());
                }
            }
            else
            {
                var tagsAll = fileInfo.Tags.ToList();
                if (tagsAll.Count > 0 && !@short)
                    builder.AddField("Tags",
                        string.Join(", ", tagsAll.Select(x => char.ToUpper(x[0]) + x.Substring(1))), true);
                builder.AddField("Created", fileInfo.TimeCreated.ToString("MMMM dd, yyyy"), true);
                if (fileInfo.TimeUpdated - fileInfo.TimeCreated > TimeSpan.FromDays(1) && !@short)
                    builder.AddField("Updated", fileInfo.TimeUpdated.ToString("MMMM dd, yyyy"), true);
                if (fileInfo.Views > 0 && !@short)
                    builder.AddField("Views", fileInfo.Views.ToString(), true);
                if (fileInfo.Subscriptions > 0)
                    builder.AddField("Subcribers", $"{fileInfo.Subscriptions}", true);
                if (fileInfo.Favorites > 0 && !@short)
                    builder.AddField("Favorites", $"{fileInfo.Favorites}", true);
                if (fileInfo.FileSize > 0 && !@short)
                    builder.AddField("Size", BytesToString(fileInfo.FileSize), true);

                var descLength = @short ? SHORT_DESCRIPTION_LENGTH : LONG_DESCRIPTION_LENGTH;
                if (fileInfo.Description != null && descLength > 0)
                {
                    var sb = new StringBuilder(fileInfo.Description.Length);
                    {
                        int prevWord = 0;
                        for (var i = 0; i < fileInfo.Description.Length; i++)
                        {
                            if (char.IsWhiteSpace(fileInfo.Description[i]))
                            {
                                if (sb.Length + i - prevWord > descLength)
                                    break;
                                sb.Append(fileInfo.Description.Substring(prevWord, i - prevWord));
                                prevWord = i;
                            }
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (sb.Length + fileInfo.Description.Length - prevWord <= descLength)
                            sb.Append(fileInfo.Description.Substring(prevWord, fileInfo.Description.Length - prevWord));
                        else
                            sb.Append("...");
                    }
                    sb.Replace("[b]", "**").Replace("[/b]", "**").Replace("[B]", "**").Replace("[/B]", "**");
                    sb.Replace("[i]", "*").Replace("[/i]", "*").Replace("[I]", "*").Replace("[/I]", "*");
                    builder.AddField("Description", _bbCodeMatcher.Replace(sb.ToString(), ""));
                }
            }
            return builder.Build();
        }

        private static readonly Regex _bbCodeMatcher = new Regex(@"\[(/|)[A-Z0-9]+\]", RegexOptions.IgnoreCase);

        // https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
        private static string BytesToString(ulong bytes)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if (bytes == 0)
                return "0" + suf[0];
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return num + suf[place];
        }
    }
}