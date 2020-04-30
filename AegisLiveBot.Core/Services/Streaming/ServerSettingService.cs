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
        Task<ServerSetting> GetOrCreateServerSetting(ulong guildId);
        Task SetOrReplaceRole(ulong guildId, ulong roleId);
        Task SetOrReplaceTwitchChannel(ulong guildId, ulong chId);
        Task<bool> TogglePriorityMode(ulong guildId);
    }
    public class ServerSettingService : IServerSettingService
    {
        private readonly Context _context;
        public ServerSettingService(Context context)
        {
            _context = context;
        }
        public async Task<ServerSetting> GetOrCreateServerSetting(ulong guildId)
        {
            var serverSettings = await _context.ServerSettings.FirstOrDefaultAsync(x => x.GuildId == guildId).ConfigureAwait(false);
            if(serverSettings == null)
            {
                await _context.ServerSettings.AddAsync(new ServerSetting { GuildId = guildId }).ConfigureAwait(false);
                await _context.SaveChangesAsync().ConfigureAwait(false);
                serverSettings = await _context.ServerSettings.FirstOrDefaultAsync(x => x.GuildId == guildId).ConfigureAwait(false);
            }
            return serverSettings;
        }
        public async Task SetOrReplaceRole(ulong guildId, ulong roleId)
        {
            var serverSetting = await GetOrCreateServerSetting(guildId);
            if (serverSetting != null) {
                serverSetting.RoleId = roleId;
                _context.ServerSettings.Update(serverSetting);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task SetOrReplaceTwitchChannel(ulong guildId, ulong chId)
        {
            var serverSetting = await GetOrCreateServerSetting(guildId);
            if (serverSetting != null)
            {
                serverSetting.TwitchChannelId = chId;
                _context.ServerSettings.Update(serverSetting);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public async Task<bool> TogglePriorityMode(ulong guildId)
        {
            Console.WriteLine("wtfa");
            var serverSetting = await GetOrCreateServerSetting(guildId);
            Console.WriteLine("wtfb");
            serverSetting.PriorityMode = !serverSetting.PriorityMode;
            Console.WriteLine("wtfc");
            _context.ServerSettings.Update(serverSetting);
            Console.WriteLine("wtfd");
            await _context.SaveChangesAsync().ConfigureAwait(false);
            Console.WriteLine("wtfe");
            return serverSetting.PriorityMode;
        }
    }
}
