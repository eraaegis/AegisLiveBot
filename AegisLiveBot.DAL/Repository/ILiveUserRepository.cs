using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Streaming;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface ILiveUserRepository : IRepository<LiveUserDb>
    {
        LiveUserDb GetByGuildIdUserId(ulong guildId, ulong userId);
        IEnumerable<LiveUserDb> GetAllByGuildId(ulong guildId);
        LiveUserDb GetOrAddByGuildIdUserId(ulong guildId, ulong userId);
        void UpdateTwitchName(ulong guildId, ulong userId, string twitchName);
        void RemoveByGuildIdUserId(ulong guildId, ulong userId);
        bool TogglePriorityUser(ulong guildId, ulong userId);
        bool ToggleAlertUser(ulong guildId, ulong userId);
        bool SetStreaming(ulong guildId, ulong userId, bool isStreaming);
        void SetLastStreamed(ulong guildId, ulong userId);
        void AddOrUpdateByLiveUser(LiveUser liveUser);
    }
}
