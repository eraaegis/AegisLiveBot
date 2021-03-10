using AegisLiveBot.DAL.Models.CustomCrawler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class CustomReplyRepository : Repository<CustomReplyDb>, ICustomReplyRepository
    {
        public CustomReplyRepository(Context context) : base(context) { }

        public CustomReplyDb GetByGuildIdId(ulong guildId, int id)
        {
            return _dbset.FirstOrDefault(x => x.GuildId == guildId && x.Id == id);
        }

        public IEnumerable<CustomReplyDb> GetAllByGuildId(ulong guildId)
        {
            return _dbset.Where(x => x.GuildId == guildId);
        }

        public void AddByGuildId(ulong guildId, CustomReply customReply)
        {
            _dbset.Add(new CustomReplyDb
            {
                GuildId = guildId,
                ChannelIds = string.Join(",", customReply.Channels.Select(x => x.Id).ToList()),
                Message = customReply.Message,
                Triggers = string.Join(";", customReply.Triggers.Select(x => string.Join(",", x))),
                Cooldown = customReply.Cooldown
            });
        }

        public void UpdateByGuildId(ulong guildId, CustomReply customReply)
        {
            _dbset.Update(new CustomReplyDb
            {
                Id = customReply.Id,
                GuildId = guildId,
                ChannelIds = string.Join(",", customReply.Channels.Select(x => x.Id).ToList()),
                Message = customReply.Message,
                Triggers = string.Join(";", customReply.Triggers.Select(x => string.Join(",", x))),
                Cooldown = customReply.Cooldown
            });
        }

        public void RemoveById(int Id)
        {
            Delete(Id);
        }
    }
}
