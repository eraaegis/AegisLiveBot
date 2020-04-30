using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Streaming;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface IServerSettingRepository : IRepository<ServerSetting>
    {
        ServerSetting GetOrAddByGuildId(ulong guildId);
        void SetStreamingRole(ulong guildId, ulong roleId);
        void SetTwitchChannel(ulong guildId, ulong chId);
        bool TogglePriorityMode(ulong guildId);
    }
}
