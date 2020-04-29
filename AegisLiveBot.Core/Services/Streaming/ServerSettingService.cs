using AegisLiveBot.DAL;
using AegisLiveBot.DAL.Models.Streaming;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Services.Streaming
{
    public interface IServerSettingService
    {
        Task<ServerSetting> GetServerSetting(ulong guildId);
        Task SetOrReplaceRole(ulong guildId, ulong roleId);
    }
    public class ServerSettingService : IServerSettingService
    {
        private readonly Context _context;
        public ServerSettingService(Context context)
        {
            _context = context;
        }
        public async Task<ServerSetting> GetServerSetting(ulong guildId)
        {
            return await _context.ServerSettings.FirstOrDefaultAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        }
        public async Task SetOrReplaceRole(ulong guildId, ulong roleId)
        {
            var serverSetting = await GetServerSetting(guildId);
            if (serverSetting == null)
            {
                await _context.ServerSettings.AddAsync(new ServerSetting { GuildId = guildId, RoleId = roleId }).ConfigureAwait(false);
            }
            else
            {
                serverSetting.RoleId = roleId;
                _context.ServerSettings.Update(serverSetting);
            }
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
