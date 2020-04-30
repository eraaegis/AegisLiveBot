﻿using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Streaming;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface ILiveUserRepository : IRepository<LiveUser>
    {
        LiveUser GetByGuildIdUserId(ulong guildId, ulong userId);
        IEnumerable<LiveUser> GetAllByGuildId(ulong guildId);
        LiveUser GetOrAddByGuildIdUserId(ulong guildId, ulong userId);
        void UpdateTwitchName(ulong guildId, ulong userId, string twitchName);
        void RemoveByGuildIdUserId(ulong guildId, ulong userId);
        bool TogglePriorityUser(ulong guildId, ulong userId);
    }
}