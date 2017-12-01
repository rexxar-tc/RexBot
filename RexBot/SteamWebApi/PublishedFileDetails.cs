using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RexBot.SteamWebApi
{
    public class PublishedFileDetails
    {
        [JsonProperty("publishedfileid")] public ulong FileId;

        [JsonProperty("result")] public SteamWebResult Result;

        [JsonProperty("creator")] public ulong Creator;

        [JsonProperty("consumer_app_id")] public ulong ConsumerAppId;

        [JsonProperty("file_size")] public ulong FileSize;

        [JsonProperty("preview_url")] public string PreviewUrl;

        [JsonProperty("title")] public string Title;

        [JsonProperty("description")] public string Description;

        [JsonProperty("visibility")] public int Visibility;

        [JsonProperty("banned")] public int Banned;

        [JsonProperty("time_created")] public long TimeCreatedUnix;

        public DateTime TimeCreated => DateTimeOffset.FromUnixTimeSeconds(TimeCreatedUnix).UtcDateTime;

        [JsonProperty("time_updated")] public long TimeUpdatedUnix;

        public DateTime TimeUpdated => DateTimeOffset.FromUnixTimeSeconds(TimeUpdatedUnix).UtcDateTime;

        [JsonProperty("subscriptions")] public int Subscriptions;

        [JsonProperty("favorited")] public int Favorites;

        [JsonProperty("lifetime_subscriptions")] public int LifetimeSubscribers;

        [JsonProperty("lifetime_favorited")] public int LifetimeFavorites;

        [JsonProperty("views")] public int Views;

        [JsonProperty("tags")] public List<TagData> TagDataArray;

        public IEnumerable<string> Tags => TagDataArray?.Select(x => x.Tag).OrderBy(x => x).Distinct() ??
                                           Enumerable.Empty<string>();

        public struct TagData
        {
            [JsonProperty("tag")] public string Tag;
        }
    }
}