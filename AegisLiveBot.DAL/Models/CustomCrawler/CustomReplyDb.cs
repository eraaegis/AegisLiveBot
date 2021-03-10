using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.CustomCrawler
{
    public class CustomReplyDb : Entity
    {
        public ulong GuildId { get; set; }

        public string ChannelIds { get; set; }

        public string Message { get; set; }

        public string Triggers { get; set; }

        public int Cooldown { get; set; }
    }
}
