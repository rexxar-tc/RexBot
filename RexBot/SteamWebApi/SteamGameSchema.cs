using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RexBot.SteamWebApi
{
    public class SteamGameSchema
    {
        [JsonProperty("gameName")]
        public string Name;
    }
}
