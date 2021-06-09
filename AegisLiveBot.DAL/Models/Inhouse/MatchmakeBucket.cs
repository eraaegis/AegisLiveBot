using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class MatchmakeBucket
    {
        public Dictionary<PlayerRole, List<InhousePlayer>> Players { get; set; }

        public MatchmakeBucket()
        {
            Players = new Dictionary<PlayerRole, List<InhousePlayer>>();
            Players.Add(PlayerRole.Top, new List<InhousePlayer>());
            Players.Add(PlayerRole.Jgl, new List<InhousePlayer>());
            Players.Add(PlayerRole.Mid, new List<InhousePlayer>());
            Players.Add(PlayerRole.Bot, new List<InhousePlayer>());
            Players.Add(PlayerRole.Sup, new List<InhousePlayer>());
        }

        public MatchmakeBucket(MatchmakeBucket other): this()
        {
            Players[PlayerRole.Top] = other.Players[PlayerRole.Top].ToList();
            Players[PlayerRole.Jgl] = other.Players[PlayerRole.Jgl].ToList();
            Players[PlayerRole.Mid] = other.Players[PlayerRole.Mid].ToList();
            Players[PlayerRole.Bot] = other.Players[PlayerRole.Bot].ToList();
            Players[PlayerRole.Sup] = other.Players[PlayerRole.Sup].ToList();
        }

        public bool Filled()
        {
            if (Players[PlayerRole.Top].Count() >= 2 &&
                Players[PlayerRole.Jgl].Count() >= 2 &&
                Players[PlayerRole.Mid].Count() >= 2 &&
                Players[PlayerRole.Bot].Count() >= 2 &&
                Players[PlayerRole.Sup].Count() >= 2)
            {
                return true;
            }
            return false;
        }
    }
}
