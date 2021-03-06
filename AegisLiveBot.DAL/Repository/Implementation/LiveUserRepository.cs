﻿using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Streaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class LiveUserRepository : Repository<LiveUserDb>, ILiveUserRepository
    {
        public LiveUserRepository(Context context) : base(context) { }
        public LiveUserDb GetByGuildIdUserId(ulong guildId, ulong userId)
        {
            return _dbset.FirstOrDefault(x => x.GuildId == guildId && x.UserId == userId);
        }
        public IEnumerable<LiveUserDb> GetAllByGuildId(ulong guildId)
        {
            return _dbset.Where(x => x.GuildId == guildId);
        }
        public LiveUserDb GetOrAddByGuildIdUserId(ulong guildId, ulong userId)
        {
            var liveUser = GetByGuildIdUserId(guildId, userId);
            if(liveUser == null)
            {
                liveUser = new LiveUserDb { GuildId = guildId, UserId = userId };
                _dbset.Add(liveUser);
            }
            return liveUser;
        }
        public void UpdateTwitchName(ulong guildId, ulong userId, string twitchName)
        {
            var liveUser = GetOrAddByGuildIdUserId(guildId, userId);
            liveUser.TwitchName = twitchName;
            AddOrUpdate(liveUser);
        }
        public void RemoveByGuildIdUserId(ulong guildId, ulong userId)
        {
            var liveUser = GetByGuildIdUserId(guildId, userId);
            if(liveUser == null)
            {
                throw new UserNotRegisteredException();
            }
            _dbset.Remove(liveUser);
        }
        public bool TogglePriorityUser(ulong guildId, ulong userId)
        {
            var liveUser = GetByGuildIdUserId(guildId, userId);
            if(liveUser == null)
            {
                throw new UserNotFoundException();
            }
            liveUser.PriorityUser = !liveUser.PriorityUser;
            _dbset.Update(liveUser);
            return liveUser.PriorityUser;
        }
        public bool ToggleAlertUser(ulong guildId, ulong userId)
        {
            var liveUser = GetByGuildIdUserId(guildId, userId);
            if (liveUser == null)
            {
                throw new UserNotFoundException();
            }
            liveUser.TwitchAlert = !liveUser.TwitchAlert;
            _dbset.Update(liveUser);
            return liveUser.TwitchAlert;
        }
        public bool SetStreaming(ulong guildId, ulong userId, bool isStreaming)
        {
            var liveUser = GetByGuildIdUserId(guildId, userId);
            if(liveUser == null)
            {
                throw new UserNotFoundException();
            }
            liveUser.IsStreaming = isStreaming;
            _dbset.Update(liveUser);
            return liveUser.IsStreaming;
        }
        public void SetLastStreamed(ulong guildId, ulong userId)
        {
            var liveUser = GetByGuildIdUserId(guildId, userId);
            if(liveUser == null)
            {
                throw new UserNotFoundException();
            }
            liveUser.LastStreamed = DateTime.UtcNow;
            _dbset.Update(liveUser);
        }
        public void AddOrUpdateByLiveUser(LiveUser liveUser)
        {
            var liveUserDb = GetByGuildIdUserId(liveUser.GuildId, liveUser.UserId);
            liveUserDb.TwitchName = liveUser.TwitchName;
            liveUserDb.PriorityUser = liveUser.PriorityUser;
            liveUserDb.TwitchAlert = liveUser.TwitchAlert;
            liveUserDb.IsStreaming = liveUser.IsStreaming;
            liveUserDb.LastStreamed = liveUser.LastStreamed;
            liveUserDb.HasRole = liveUser.HasRole;
            _dbset.Update(liveUserDb);
        }
    }
}
