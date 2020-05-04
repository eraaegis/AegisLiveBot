using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL
{
    public class ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }
        [JsonProperty("twitchclientid")]
        public string TwitchClientId { get; private set; }
        [JsonProperty("twitchclientsecret")]
        public string TwitchClientSecret { get; private set; }
    }
}
