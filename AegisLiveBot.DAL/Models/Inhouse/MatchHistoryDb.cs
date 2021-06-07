using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class MatchHistoryDb : Entity
    {
        public string PlayersWon { get; set; }
        public string PlayersLost { get; set; }
    }
}
