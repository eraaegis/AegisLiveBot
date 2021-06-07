using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class InhouseQueue
    {
        public ulong ChannelId { get; set; }

        public Dictionary<PlayerRole, string> Emojis { get; set; }

        public List<InhousePlayer> PlayersInQueue { get; set; }

        public InhouseQueue(ulong channelId)
        {
            ChannelId = channelId;
            PlayersInQueue = new List<InhousePlayer>();

            Emojis = new Dictionary<PlayerRole, string>();
            Emojis.Add(PlayerRole.Top, "TOP:");
            Emojis.Add(PlayerRole.Jgl, "JGL:");
            Emojis.Add(PlayerRole.Mid, "MID:");
            Emojis.Add(PlayerRole.Bot, "BOT:");
            Emojis.Add(PlayerRole.Sup, "SUP:");
            Emojis.Add(PlayerRole.Fill, "FILL:");
        }
    }
}
