using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models
{
    public class ServerSetting : Entity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        public ulong TwitchChannelId { get; set; }
        public bool TwitchAlertMode { get; set; }
        public bool PriorityMode { get; set; }
        public ulong GamesCategory { get; set; }
    }
}
