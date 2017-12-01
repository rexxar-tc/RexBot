using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using RestSharp.Extensions.MonoHttp;

namespace RexBot.SteamWebApi
{
    public class SteamWebApi
    {
        private const string HOST = "https://api.steampowered.com/";
        private const string REMOTE_STORAGE = HOST + "ISteamRemoteStorage";
        private const string STEAM_USER = HOST + "ISteamUser";
        private const string STEAM_USER_STATS = HOST + "ISteamUserStats";

        private readonly string _key;
        private readonly HttpClient _client;

        public SteamWebApi(string key)
        {
            _key = key;
            _client = new HttpClient();
        }

        private async Task<HttpResponseMessage> DoPost(string path, string method, int version,
            IDictionary<string, string> kv)
        {
            kv["key"] = _key;
            kv["format"] = "json";
            var url = $"{path}/{method}/v{version}/";
            var content = new FormUrlEncodedContent(kv);
            return await _client.PostAsync(url, content);
        }

        private async Task<HttpResponseMessage> DoGet(string path, string method, int version,
            IDictionary<string, string> kv)
        {
            kv["key"] = _key;
            kv["format"] = "json";
            var queryString = string.Join("&", kv.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            var url = $"{path}/{method}/v{version}/?{queryString}";
            return await _client.GetAsync(url);
        }

        private async Task<string> Unwrap(Task<HttpResponseMessage> msg)
        {
            var res = await msg;
            if (res.StatusCode != HttpStatusCode.OK)
                throw new HttpResponseException(res.StatusCode);
            return await res.Content.ReadAsStringAsync();
        }

        private readonly MemoryCache _steamPlayerCache = new MemoryCache("steamwebapi_playerdetails");

        private class BoxCustom<T>
        {
            public readonly T Value;

            public BoxCustom(T val)
            {
                Value = val;
            }
        }

        public async Task<PlayerDetails> QueryPlayerDetails(ulong id)
        {
            var result = _steamPlayerCache.Get(id.ToString()) as BoxCustom<PlayerDetails>;
            if (result != null)
                return result.Value;
            var kv = new Dictionary<string, string>() {["steamids"] = id.ToString()};
            var res = await Unwrap(DoGet(STEAM_USER, "GetPlayerSummaries", 2, kv));
            var obj = JsonConvert.DeserializeObject<ResponseContainer>(res);
            result = new BoxCustom<PlayerDetails>(obj.Response?.Players?.FirstOrDefault(x => x.SteamId == id));
            _steamPlayerCache.Add(id.ToString(), result, DateTimeOffset.Now + TimeSpan.FromMinutes(5));
            return result.Value;
        }

        private readonly MemoryCache _publishedFileCache = new MemoryCache("steamwebapi_publishedfile");

        public async Task<PublishedFileDetails> QueryPublishedFile(ulong id)
        {
            var result = _publishedFileCache.Get(id.ToString()) as BoxCustom<PublishedFileDetails>;
            if (result != null)
                return result.Value;

            var kv = new Dictionary<string, string>() {["itemcount"] = "1", ["publishedfileids[0]"] = id.ToString()};
            var res = await Unwrap(DoPost(REMOTE_STORAGE, "GetPublishedFileDetails", 1, kv));
            var obj = JsonConvert.DeserializeObject<ResponseContainer>(res);
            result = new BoxCustom<PublishedFileDetails>(obj.Response?.PublishedFileDetails?.FirstOrDefault(
                x => x.FileId == id && x.Result == SteamWebResult.Ok));
            _publishedFileCache.Add(id.ToString(), result, DateTimeOffset.Now + TimeSpan.FromMinutes(15));
            return result.Value;
        }

        private readonly MemoryCache _publishedCollectionCache = new MemoryCache("steamwebapi_publishedcollection");

        public async Task<CollectionDetails> QueryCollection(ulong id)
        {
            var result = _publishedCollectionCache.Get(id.ToString()) as BoxCustom<CollectionDetails>;
            if (result != null)
                return result.Value;

            var kv = new Dictionary<string, string>()
            {
                ["collectioncount"] = "1",
                ["publishedfileids[0]"] = id.ToString()
            };
            var res = await Unwrap(DoPost(REMOTE_STORAGE, "GetCollectionDetails", 1, kv));
            var obj = JsonConvert.DeserializeObject<ResponseContainer>(res);
            result = new BoxCustom<CollectionDetails>(
                obj.Response?.CollectionDetails?.FirstOrDefault(x => x.FileId == id && x.Result == SteamWebResult.Ok));
            _publishedFileCache.Add(id.ToString(), result, DateTimeOffset.Now + TimeSpan.FromMinutes(15));
            return result.Value;
        }

        private readonly MemoryCache _steamGameCache = new MemoryCache("steamwebapi_gameschema");

        public async Task<SteamGameSchema> QueryGame(ulong id)
        {
            var result = _steamGameCache.Get(id.ToString()) as BoxCustom<SteamGameSchema>;
            if (result != null)
                return result.Value;
            var kv = new Dictionary<string, string>() {["appid"] = id.ToString()};
            var res = await Unwrap(DoGet(STEAM_USER_STATS, "GetSchemaForGame", 2, kv));
            var obj = JsonConvert.DeserializeObject<GameSchemaResponseContainer>(res);
            _steamGameCache.Add(id.ToString(), new BoxCustom<SteamGameSchema>(obj.Game),
                DateTimeOffset.Now + TimeSpan.FromHours(1));
            return obj.Game;
        }

        private class GameSchemaResponseContainer
        {
            [JsonProperty("game")] public SteamGameSchema Game;
        }

        private class ResponseContainer
        {
            [JsonProperty("response")] public ResponseData Response;

            public class ResponseData
            {
                [JsonProperty("result")] public SteamWebResult Result;
                [JsonProperty("publishedfiledetails")] public List<PublishedFileDetails> PublishedFileDetails;
                [JsonProperty("collectiondetails")] public List<CollectionDetails> CollectionDetails;
                [JsonProperty("players")] public List<PlayerDetails> Players;
            }
        }
    }
}