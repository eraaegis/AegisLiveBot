using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class InhouseQueue
    {
        public ulong ChannelId { get; set; }

        public List<DiscordMember> Top { get; set; }

        public List<DiscordMember> Jgl { get; set; }

        public List<DiscordMember> Mid { get; set; }

        public List<DiscordMember> Bot { get; set; }

        public List<DiscordMember> Sup { get; set; }

        public Dictionary<PlayerRole, string> Emojis { get; set; }

        public InhouseQueue(ulong channelId)
        {
            ChannelId = channelId;
            Top = new List<DiscordMember>();
            Jgl = new List<DiscordMember>();
            Mid = new List<DiscordMember>();
            Bot = new List<DiscordMember>();
            Sup = new List<DiscordMember>();

            Emojis = new Dictionary<PlayerRole, string>();
            Emojis.Add(PlayerRole.Top, "TOP:");
            Emojis.Add(PlayerRole.Jgl, "JGL:");
            Emojis.Add(PlayerRole.Mid, "MID:");
            Emojis.Add(PlayerRole.Bot, "BOT:");
            Emojis.Add(PlayerRole.Sup, "SUP:");
        }
    }
}
