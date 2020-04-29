using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Streaming
{
    public class LiveUser : Entity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string TwitchName { get; set; }
    }
}
