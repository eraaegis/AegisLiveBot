﻿using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class InhouseGame
    {
        public ulong ChannelId { get; set; }
        public List<InhousePlayer> InhousePlayers { get; set; }
        public List<QueueGroup> QueueGroups { get; set; }
        public bool CheckingWin { get; set; }
        public DateTime StartTime { get; set; }

        public InhouseGame(ulong channelId)
        {
            ChannelId = channelId;
            InhousePlayers = new List<InhousePlayer>();
            CheckingWin = false;
            StartTime = DateTime.UtcNow;
        }
    }
}
