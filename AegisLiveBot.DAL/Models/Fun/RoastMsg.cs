using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Fun
{
    public class RoastMsg : Entity
    {
        public ulong GuildId { get; set; }
        public string Msg { get; set; }
    }
}
