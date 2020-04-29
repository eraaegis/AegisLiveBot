using AegisLiveBot.DAL;
using AegisLiveBot.DAL.Models.Streaming;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Services.Streaming
{
    public interface ILiveUserService
    {
        Task AddOrReplaceLiveUser(ulong guildId, ulong userId, string twitchName);
        Task RemoveLiveUser(ulong guildId, ulong userId);
        IEnumerable<LiveUser> ListLiveUser(ulong guildId);
    }
    public class LiveUserService : ILiveUserService
    {
        private readonly Context _context;
        private readonly DiscordClient _client;
        private readonly Timer _twitchPollTimer;
        private string TwitchClientId = "";
        public LiveUserService(Context context, DiscordClient client = null)
        {
            _context = context;
            if(client != null)
            {
                _client = client;
            }

            var json = string.Empty;
            using (var fs = File.OpenRead("../AegisLiveBot.DAL/config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            TwitchClientId = configJson.TwitchClientId;

            _twitchPollTimer = new Timer(async (state) =>
            {
                var liveUsers = _context.LiveUsers.ToArray();
                var hcHandle = new HttpClientHandler();
                foreach (var liveUser in liveUsers)
                {
                    using (var hc = new HttpClient(hcHandle, false))
                    {
                        hc.DefaultRequestHeaders.Add("Client-ID", TwitchClientId);
                        hc.DefaultRequestHeaders.UserAgent.ParseAdd("AegisLiveBot");
                        hc.Timeout = TimeSpan.FromSeconds(5);

                        using (var response = await hc.GetAsync($"https://api.twitch.tv/helix/streams?user_login={liveUser.TwitchName}"))
                        {
                            response.EnsureSuccessStatusCode();
                            var jsonString = await response.Content.ReadAsStringAsync();
                            var jsonObject = JObject.Parse(jsonString);
                            var jsonData = jsonObject["data"];
                            JToken jsonType = null;
                            if(jsonData.Count() != 0)
                            {
                                jsonType = jsonData[0]["type"];
                            }
                            
                            var guild = _client.Guilds.FirstOrDefault(x => x.Value.Id == liveUser.GuildId).Value;
                            var user = await guild.GetMemberAsync(liveUser.UserId).ConfigureAwait(false);
                            var serverSetting = await _context.ServerSettings.FirstOrDefaultAsync(x => x.GuildId == liveUser.GuildId).ConfigureAwait(false);
                            if(serverSetting == null)
                            {
                                Console.WriteLine($"Streamer role not set!");
                            } else
                            {
                                var role = guild.GetRole(serverSetting.RoleId);
                                if (jsonType != null && jsonType.ToString() == "live")
                                {
                                    await user.GrantRoleAsync(role);
                                }
                                else
                                {
                                    await user.RevokeRoleAsync(role);
                                }
                            }
                        }
                    }
                }
            }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        }
        public async Task AddOrReplaceLiveUser(ulong guildId, ulong userId, string twitchName)
        {
            var liveUser = await _context.LiveUsers.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId).ConfigureAwait(false);
            if (liveUser == null)
            {
                await _context.LiveUsers.AddAsync(new LiveUser { GuildId = guildId, UserId = userId, TwitchName = twitchName }).ConfigureAwait(false);
            } else
            {
                liveUser.TwitchName = twitchName;
                _context.LiveUsers.Update(liveUser);
            }
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        public async Task RemoveLiveUser(ulong guildId, ulong userId)
        {
            var liveUser = await _context.LiveUsers.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId).ConfigureAwait(false);
            if (liveUser != null)
            {
                _context.LiveUsers.Remove(liveUser);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public IEnumerable<LiveUser> ListLiveUser(ulong guildId)
        {
            return _context.LiveUsers.Where(x => x.GuildId == guildId).ToArray();
        }
    }
}
