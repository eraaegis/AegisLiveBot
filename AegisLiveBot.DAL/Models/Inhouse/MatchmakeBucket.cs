using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class MatchmakeBucket
    {
        public Dictionary<PlayerRole, InhousePlayer> BluePlayers { get; set; }
        public Dictionary<PlayerRole, InhousePlayer> RedPlayers { get; set; }
        public List<QueueGroup> QueueGroups { get; set; }
        public int PlayerCount { get; set; }

        public MatchmakeBucket()
        {
            BluePlayers = new Dictionary<PlayerRole, InhousePlayer>();
            RedPlayers = new Dictionary<PlayerRole, InhousePlayer>();
            QueueGroups = new List<QueueGroup>();
            PlayerCount = 0;
        }

        public MatchmakeBucket(MatchmakeBucket other): this()
        {
            BluePlayers = other.BluePlayers.ToDictionary(x => x.Key, x => x.Value);
            RedPlayers = other.RedPlayers.ToDictionary(x => x.Key, x => x.Value);
            QueueGroups = other.QueueGroups.ToList();
            PlayerCount = other.PlayerCount;
        }

        public bool TryAddRoleCombination(Dictionary<PlayerRole, InhousePlayer> roleCombination, PlayerSide playerSide)
        {
            var team = playerSide == PlayerSide.Blue ? BluePlayers : RedPlayers;
            if (roleCombination.Any(x => team.ContainsKey(x.Key)))
            {
                return false;
            }
            
            foreach (var role in roleCombination)
            {
                team.Add(role.Key, role.Value);
                PlayerCount += 1;
            }

            return true;
        }
    }
}
