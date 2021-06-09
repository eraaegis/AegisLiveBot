using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Inhouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class MatchHistoryRepository : Repository<MatchHistoryDb>, IMatchHistoryRepository
    {
        public MatchHistoryRepository(Context context) : base(context) { }
        public void AddByInhouseGame(InhouseGame inhouseGame, PlayerSide winningSide)
        {
            var matchHistoryDb = new MatchHistoryDb
            {
                PlayersWon = string.Join(",", inhouseGame.InhousePlayers.Where(x => x.PlayerSide == winningSide).Select(x => x.Player.Id)),
                PlayersLost = string.Join(",", inhouseGame.InhousePlayers.Where(x => x.PlayerSide != winningSide).Select(x => x.Player.Id))
            };

            _dbset.Add(matchHistoryDb);
        }
    }
}
