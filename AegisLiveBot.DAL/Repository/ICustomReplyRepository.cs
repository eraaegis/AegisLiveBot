using AegisLiveBot.DAL.Models.CustomCrawler;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface ICustomReplyRepository : IRepository<CustomReplyDb>
    {
        CustomReplyDb GetByGuildIdId(ulong guildId, int id);

        IEnumerable<CustomReplyDb> GetAllByGuildId(ulong guildId);

        void AddByGuildId(ulong guildId, CustomReply customReply);

        void UpdateByGuildId(ulong guildId, CustomReply customReply);

        void RemoveById(int Id);
    }
}
