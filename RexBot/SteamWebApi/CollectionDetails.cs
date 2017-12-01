using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RexBot.SteamWebApi
{
    public class CollectionDetails
    {
        [JsonProperty("publishedfileid")] public ulong FileId;

        [JsonProperty("result")] public SteamWebResult Result;

        [JsonProperty("children")] public List<CollectionItem> RawItems;

        public IEnumerable<CollectionItem> Items => RawItems?.OrderBy(x => x.SortOrder) ??
                                                    Enumerable.Empty<CollectionItem>();

        public class CollectionItem
        {
            [JsonProperty("publishedfileid")] public ulong FileId;

            [JsonProperty("sortorder")] public int SortOrder;

            [JsonProperty("filetype")] public WorkshopFileType FileType;
        }
    }
}