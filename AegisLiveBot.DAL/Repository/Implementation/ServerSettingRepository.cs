using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Streaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class ServerSettingRepository : Repository<ServerSetting>, IServerSettingRepository
    {
        public ServerSettingRepository(Context context) : base(context) { }
        public ServerSetting GetOrAddByGuildId(ulong guildId)
        {
            var serverSetting =_dbset.FirstOrDefault(x => x.GuildId == guildId);
            if(serverSetting == null)
            {
                serverSetting = new ServerSetting { GuildId = guildId };
            }
            return serverSetting;
        }
        public void SetStreamingRole(ulong guildId, ulong roleId)
        {
            var serverSetting = GetOrAddByGuildId(guildId);
            serverSetting.RoleId = roleId;
            AddOrUpdate(serverSetting);
        }
        public void SetTwitchChannel(ulong guildId, ulong chId)
        {
            var serverSetting = GetOrAddByGuildId(guildId);
            serverSetting.TwitchChannelId = chId;
            AddOrUpdate(serverSetting);
        }
        public bool TogglePriorityMode(ulong guildId)
        {
            var serverSetting = GetOrAddByGuildId(guildId);
            serverSetting.PriorityMode = !serverSetting.PriorityMode;
            AddOrUpdate(serverSetting);
            return serverSetting.PriorityMode;
        }
        public void SetGamesCategory(ulong guildId, ulong catId)
        {
            var serverSetting = GetOrAddByGuildId(guildId);
            serverSetting.GamesCategory = catId;
            AddOrUpdate(serverSetting);
        }
    }
}
