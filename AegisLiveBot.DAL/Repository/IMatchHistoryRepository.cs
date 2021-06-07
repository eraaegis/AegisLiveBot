using AegisLiveBot.DAL.Models.Inhouse;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface IMatchHistoryRepository : IRepository<MatchHistoryDb>
    {
        void AddByInhouseGame(InhouseGame inhouseGame, PlayerSide winningSide);
    }
}
