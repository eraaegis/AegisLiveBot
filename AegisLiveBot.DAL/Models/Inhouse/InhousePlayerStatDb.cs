using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class InhousePlayerStatDb : Entity
    {
        public ulong PlayerId { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }

        public InhousePlayerStatDb(ulong playerId)
        {
            PlayerId = playerId;
            Wins = 0;
            Loses = 0;
        }
    }
}
