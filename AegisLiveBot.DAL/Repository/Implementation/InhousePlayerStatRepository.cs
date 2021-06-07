using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Inhouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class InhousePlayerStatRepository : Repository<InhousePlayerStatDb>, IInhousePlayerStatRepository
    {
        public InhousePlayerStatRepository(Context context) : base(context) { }

        public void AddByInhouseGame(InhouseGame inhouseGame, PlayerSide winningSide)
        {
            foreach(var player in inhouseGame.InhousePlayers)
            {
                var playerDb = GetByPlayerId(player.Player.Id);
                if (playerDb == null)
                {
                    playerDb = new InhousePlayerStatDb(player.Player.Id);
                }
                if (player.PlayerSide == winningSide)
                {
                    playerDb.Wins += 1;
                } else
                {
                    playerDb.Loses += 1;
                }
                _dbset.Update(playerDb);
            }
        }

        public InhousePlayerStatDb GetByPlayerId(ulong playerId)
        {
            return _dbset.FirstOrDefault(x => x.PlayerId == playerId);
        }
    }
}
