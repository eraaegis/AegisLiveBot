using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class InhouseDb : Entity
    {
        public ulong ChannelId { get; set; }

        public string TopEmoji { get; set; }

        public string JglEmoji { get; set; }

        public string MidEmoji { get; set; }

        public string BotEmoji { get; set; }

        public string SupEmoji { get; set; }
    }
}
