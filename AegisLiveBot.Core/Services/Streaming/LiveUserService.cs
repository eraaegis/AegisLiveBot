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
        Task<LiveUser> GetLiveUser(ulong guildId, ulong userId);
        Task<bool> TogglePriorityUser(ulong guildId, ulong userId);
    }
    public class LiveUserService : ILiveUserService
    {
        private readonly Context _context;
        private readonly DiscordClient _client;
        private readonly Timer _twitchPollTimer;
        private string TwitchClientId = "";
        private bool IsPolling = false;
        public LiveUserService(Context context, DiscordClient client = null)
        {
            _context = context;
            if(client != null)
            {
                _client = client;
            }

            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            TwitchClientId = configJson.TwitchClientId;

            _twitchPollTimer = new Timer(async (state) =>
            {
                if (!IsPolling)
                {
                    await TryPollTwitchStreams().ConfigureAwait(false);
                }
            }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        }
        private async Task TryPollTwitchStreams()
        {
            IsPolling = true;
            var hcHandle = new HttpClientHandler();
            var liveUsersGroupByServer = _context.LiveUsers.ToArray().GroupBy(x => x.GuildId);
            foreach(var liveUsersGroup in liveUsersGroupByServer)
            {
                var serverSetting = await _context.ServerSettings.FirstOrDefaultAsync(x => x.GuildId == liveUsersGroup.Key).ConfigureAwait(false);
                if (!serverSetting.PriorityMode)
                {
                    foreach(var liveUser in liveUsersGroup)
                    {
                        await TryPollTwitchStream(hcHandle, liveUser).ConfigureAwait(false);
                    }
                } else
                {
                    var hasPriorityStream = false;
                    var priorityUsers = liveUsersGroup.Where(x => x.PriorityUser == true);
                    foreach(var priorityUser in priorityUsers)
                    {
                        var isStreaming = await TryPollTwitchStream(hcHandle, priorityUser).ConfigureAwait(false);
                        if (isStreaming)
                        {
                            hasPriorityStream = true;
                        }
                    }
                    var nonPriorityUsers = liveUsersGroup.Where(x => x.PriorityUser == false);
                    foreach(var nonPriorityUser in nonPriorityUsers)
                    {
                        if (hasPriorityStream)
                        {
                            var guild = _client.Guilds.FirstOrDefault(x => x.Value.Id == liveUsersGroup.Key).Value;
                            var user = await guild.GetMemberAsync(nonPriorityUser.UserId).ConfigureAwait(false);
                            if (guild == null || user == null)
                            {
                                Console.WriteLine($"Server or User does not exist!");
                                await RemoveLiveUser(nonPriorityUser.GuildId, nonPriorityUser.UserId).ConfigureAwait(false);
                                continue;
                            }
                            var role = guild.GetRole(serverSetting.RoleId);
                            if (role == null)
                            {
                                Console.WriteLine($"Role does not exist!");
                                continue;
                            }
                            await user.RevokeRoleAsync(role);
                        } else
                        {
                            await TryPollTwitchStream(hcHandle, nonPriorityUser).ConfigureAwait(false);
                        }
                    }
                }
            }
            IsPolling = false;
        }
        private async Task<bool> TryPollTwitchStream(HttpClientHandler hcHandle, LiveUser liveUser)
        {
            using (var hc = new HttpClient(hcHandle, false))
            {
                hc.DefaultRequestHeaders.Add("Client-ID", TwitchClientId);
                hc.DefaultRequestHeaders.UserAgent.ParseAdd("AegisLiveBot");
                hc.Timeout = TimeSpan.FromSeconds(5);

                using (var response = await hc.GetAsync($"https://api.twitch.tv/helix/streams?user_login={liveUser.TwitchName}"))
                {
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        var limit = int.Parse(response.Headers.FirstOrDefault(x => x.Key == "ratelimit-remaining").Value.ToList()[0]);
                        if (limit <= 5)
                        {
                            await Task.Delay(5000).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        await Task.Delay(5000).ConfigureAwait(false);
                    }
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var jsonData = jsonObject["data"];
                    JToken jsonType = null;
                    if (jsonData.Count() != 0)
                    {
                        jsonType = jsonData[0]["type"];
                    }

                    var guild = _client.Guilds.FirstOrDefault(x => x.Value.Id == liveUser.GuildId).Value;
                    var user = await guild.GetMemberAsync(liveUser.UserId).ConfigureAwait(false);
                    if (guild == null || user == null)
                    {
                        Console.WriteLine($"Server or User does not exist!");
                        await RemoveLiveUser(liveUser.GuildId, liveUser.UserId).ConfigureAwait(false);
                        return false;
                    }
                    var serverSetting = await _context.ServerSettings.FirstOrDefaultAsync(x => x.GuildId == liveUser.GuildId).ConfigureAwait(false);
                    if (serverSetting == null || serverSetting.RoleId == 0)
                    {
                        Console.WriteLine($"Streamer role not set!");
                    }
                    else
                    {
                        var role = guild.GetRole(serverSetting.RoleId);
                        if (role == null)
                        {
                            Console.WriteLine($"Role does not exist!");
                            return false;
                        }
                        if (jsonType != null && jsonType.ToString() == "live")
                        {
                            await user.GrantRoleAsync(role);
                            return true;
                        }
                        else
                        {
                            await user.RevokeRoleAsync(role);
                            return false;
                        }
                    }
                }
                return false;
            }
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
        public async Task<LiveUser> GetLiveUser(ulong guildId, ulong userId)
        {
            return await _context.LiveUsers.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId).ConfigureAwait(false);
        }
        public async Task<bool> TogglePriorityUser(ulong guildId, ulong userId)
        {
            var liveUser = await _context.LiveUsers.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId).ConfigureAwait(false);
            liveUser.PriorityUser = !liveUser.PriorityUser;
            _context.LiveUsers.Update(liveUser);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            return liveUser.PriorityUser;
        }
    }
}
