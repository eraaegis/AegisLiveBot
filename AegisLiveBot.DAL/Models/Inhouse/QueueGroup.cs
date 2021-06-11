using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class QueueGroup
    {
        public List<InhousePlayer> Players { get; set; }

        public QueueGroup()
        {
            Players = new List<InhousePlayer>();
        }

        // is it possible to add this player while having a valid role combination
        public bool CanAddPlayer(InhousePlayer player)
        {
            if (Players.Count() > 5)
            {
                return false;
            }

            var possibleRoleCombinations = PossibleRoleCombinations(player);
            if (possibleRoleCombinations.Count() == 0)
            {
                return false;
            }
            return true;
        }

        public List<Dictionary<PlayerRole, InhousePlayer>> PossibleRoleCombinations(InhousePlayer incomingPlayer = null)
        {
            var possibleRoleCombinations = new List<Dictionary<PlayerRole, InhousePlayer>>();
            possibleRoleCombinations.Add(new Dictionary<PlayerRole, InhousePlayer>());

            var fillPlayers = new List<InhousePlayer>();
            var playersToCheck = Players.ToList();
            if (incomingPlayer != null)
            {
                playersToCheck.Add(incomingPlayer);
            }
            foreach (var player in playersToCheck)
            {
                var tempPossibleRoleCombinations = new List<Dictionary<PlayerRole, InhousePlayer>>();
                var isFill = false;
                foreach (var queuedRole in player.QueuedRoles.Where(x => x.Value).Select(x => x.Key))
                {
                    if (queuedRole == PlayerRole.Fill)
                    {
                        fillPlayers.Add(player);
                        isFill = true;
                        break;
                    } else
                    {
                        foreach (var possibleRoleCombination in possibleRoleCombinations)
                        {
                            var tempPossibleRoleCombination = possibleRoleCombination.ToDictionary(x => x.Key, x => x.Value);
                            if (tempPossibleRoleCombination.ContainsKey(queuedRole))
                            {
                                continue;
                            } else
                            {
                                tempPossibleRoleCombination.Add(queuedRole, player);
                                tempPossibleRoleCombinations.Add(tempPossibleRoleCombination);
                            }
                        }
                    }
                }

                if (isFill)
                {
                    continue;
                }

                possibleRoleCombinations = tempPossibleRoleCombinations;
            }

            // if no possible role combinations then just return empty list
            if (possibleRoleCombinations.Count() == 0)
            {
                return possibleRoleCombinations;
            }

            var rnd = new Random();

            // if 5 players in this team, then randomly fill players and return one item in list
            if (possibleRoleCombinations.Count() == 5)
            {
                var rand = rnd.Next(0, possibleRoleCombinations.Count());
                var roleCombination = possibleRoleCombinations[rand];

                foreach (PlayerRole i in Enum.GetValues(typeof(PlayerRole)))
                {
                    if (!roleCombination.ContainsKey(i))
                    {
                        rand = rnd.Next(0, fillPlayers.Count());
                        roleCombination.Add(i, fillPlayers[rand]);
                    }
                }

                return new List<Dictionary<PlayerRole, InhousePlayer>>() { roleCombination };
            }

            // otherwise fill in players, then pick one permutation from each unique role combinations
            foreach (var player in fillPlayers)
            {
                var tempPossibleRoleCombinations = new List<Dictionary<PlayerRole, InhousePlayer>>();
                foreach (PlayerRole role in Enum.GetValues(typeof(PlayerRole)))
                {
                    foreach (var possibleRoleCombination in possibleRoleCombinations)
                    {
                        var tempPossibleRoleCombination = possibleRoleCombination.ToDictionary(x => x.Key, x => x.Value);
                        if (tempPossibleRoleCombination.ContainsKey(role))
                        {
                            continue;
                        }
                        else
                        {
                            tempPossibleRoleCombination.Add(role, player);
                            tempPossibleRoleCombinations.Add(tempPossibleRoleCombination);
                        }
                    }
                }
                possibleRoleCombinations = tempPossibleRoleCombinations;
            }

            var roleCombinationGroups = possibleRoleCombinations.GroupBy(x => x.Sum(y => (int)y.Key));
            var roleCombinations = new List<Dictionary<PlayerRole, InhousePlayer>>();
            foreach (var combination in roleCombinationGroups)
            {
                var rand = rnd.Next(0, combination.Count());
                roleCombinations.Add(combination.ElementAt(rand));
            }

            return roleCombinations;
        }
    }
}
