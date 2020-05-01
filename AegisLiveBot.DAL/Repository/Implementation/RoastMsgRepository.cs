using AegisLiveBot.DAL.Models.Fun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class RoastMsgRepository : Repository<RoastMsg>, IRoastMsgRepository
    {
        public RoastMsgRepository(Context context) : base(context) { }
        public IEnumerable<RoastMsg> GetAllByGuildId(ulong guildId)
        {
            var roastMsgs = _dbset.Where(x => x.GuildId == guildId);
            if(roastMsgs.Count() == 0)
            {
                throw new NoRoastMsgException();
            }
            return roastMsgs;
        }
        public RoastMsg GetRandomByGuildId(ulong guildId)
        {
            var roastMsgs = GetAllByGuildId(guildId);
            if(roastMsgs.Count() == 0)
            {
                throw new NoRoastMsgException();
            }
            return roastMsgs.ElementAt(AegisLiveBotRandom.RandomNumber(0, roastMsgs.Count()));
        }
        public void AddByGuildId(ulong guildId, string msg)
        {
            _dbset.Add(new RoastMsg { GuildId = guildId, Msg = msg });
        }
        public void RemoveByGuildIdMsgId(ulong guildId, int msgId)
        {
            var roastMsgs = GetAllByGuildId(guildId);
            if(msgId <= 0)
            {
                throw new ZeroOrNegativeRoastException();
            }
            if(roastMsgs.Count() < msgId)
            {
                throw new OutOfRangeRoastException(roastMsgs.Count());
            }
            Delete(roastMsgs.ElementAt(msgId - 1).Id);
        }
    }
}
