using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Models.CustomCrawler
{
    public class CustomReply
    {
        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public List<DiscordChannel> Channels { get; set; }

        public string Message { get; set; }

        public List<List<string>> Triggers { get; set; }

        public int Cooldown { get; set; }

        public DateTime LastTriggered { get; set; }
    }

    public static class CustomReplyHelper
    {
        public static string ToString(CustomReply customReply)
        {
            var msg = "```Message:\n";
            msg += $"{(string.IsNullOrEmpty(customReply.Message) ? "" : customReply.Message + "\n")}\n";
            msg += "Channels:\n";
            msg += customReply.Channels.Count == 0 ? "" : string.Join(", ", customReply.Channels.Select(x => x.Name).ToList()) + "\n";
            msg += "\n";
            msg += "Triggers:\n";
            var index = 1;
            foreach (var trigger in customReply.Triggers)
            {
                msg += $"{index}. {string.Join(",", trigger)}\n";
                ++index;
            }
            msg += "\nCooldown:\n";
            msg += $"{customReply.Cooldown} minutes```";
            return msg;
        }
    }
}
