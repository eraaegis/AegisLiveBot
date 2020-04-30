using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Streaming
{
    public class ServerSetting : Entity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        public ulong TwitchChannelId { get; set; }
        public bool PriorityMode { get; set; }
    }
}
