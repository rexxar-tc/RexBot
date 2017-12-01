using Newtonsoft.Json;

namespace RexBot.SteamWebApi
{
    public class PlayerDetails
    {
        [JsonProperty("steamid")] public ulong SteamId;

        [JsonProperty("communityvisiblitystate")] public int CommunityVisibilityState;

        [JsonProperty("profilestate")] public int ProfileState;

        [JsonProperty("personastate")] public SteamPersonaState SteamPersonaState;

        [JsonProperty("profileurl")] public string ProfileUrl;

        [JsonProperty("avatar")] public string Avatar;

        [JsonProperty("personaname")] public string PersonaName;

        [JsonProperty("realname")] public string RealName;
    }

    public enum SteamPersonaState
    {
        Offline,
        Online,
        Busy,
        Away,
        Snooze,
        LookingToTrade,
        LookingToPlay
    }
}