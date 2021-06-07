using AegisLiveBot.DAL.Models.Inhouse;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface IInhousePlayerStatRepository : IRepository<InhousePlayerStatDb>
    {
        void AddByInhouseGame(InhouseGame inhouseGame, PlayerSide winningSide);

        InhousePlayerStatDb GetByPlayerId(ulong playerId);
    }
}
