using AegisLiveBot.DAL.Models.Fun;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface IRoastMsgRepository : IRepository<RoastMsg>
    {
        IEnumerable<RoastMsg> GetAllByGuildId(ulong guildId);
        RoastMsg GetRandomByGuildId(ulong guildId);
        void AddByGuildId(ulong guildId, string msg);
        void RemoveByGuildIdMsgId(ulong guildId, int msgId);
    }
}
