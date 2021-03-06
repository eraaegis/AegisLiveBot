﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Streaming
{
    public class LiveUser
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string TwitchName { get; set; }
        public bool PriorityUser { get; set; }
        public bool TwitchAlert { get; set; }
        public bool IsStreaming { get; set; }
        public DateTime LastStreamed { get; set; }

        public bool StreamStateChanged { get; set; }
        public bool HasRole { get; set; }
    }
}
