using AegisLiveBot.Core.Common;
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
using System.Timers;

namespace AegisLiveBot.Core.Services.Streaming
{
    public interface ITwitchPollService : IStartUpService
    {
        public Task AddOrUpdateTwitchName(ulong guildId, ulong userId, string twitchName);
        public Task RemoveLiveUser(ulong guildId, ulong userId);
        public List<LiveUser> GetLiveUsersByGuildId(ulong guildId);
        public Task<bool> TogglePriorityUser(ulong guildId, ulong userId);
        public Task<bool> ToggleAlertUser(ulong guildId, ulong userId);
    }
    public class TwitchPollService : ITwitchPollService
    {
        private readonly DbService _db;
        private readonly DiscordClient _client;
        private System.Timers.Timer _twitchPollTimer;
        private string TwitchClientId = "";
        private string TwitchClientSecret = "";
        private string AccessToken = "";
        private bool IsPolling = false;
        private const int STREAM_ALERT_COOLDOWN_HOUR = 1;

        // initial set up
        private bool HasInit = false;
        private bool Initing = false;

        // sync with database timer
        private System.Timers.Timer _databaseSyncTimer;
        private const int DATABASE_SYNC_TIMER_COOLDOWN_MINUTES = 5;

        // keep a list of streamers on memory
        private List<IGrouping<ulong, LiveUser>> LiveUsers;

        private readonly System.Timers.Timer _accessTokenTimer;
        public TwitchPollService(DbService db, DiscordClient client, ConfigJson configJson)
        {
            _db = db;
            _client = client;
            TwitchClientId = configJson.TwitchClientId;
            TwitchClientSecret = configJson.TwitchClientSecret;
            _accessTokenTimer = new System.Timers.Timer();
            _accessTokenTimer.Elapsed += OnTimedEvent;
            _accessTokenTimer.Enabled = false;
            _twitchPollTimer = new System.Timers.Timer();
            _twitchPollTimer.Elapsed += PollTwitchStreams;
            _twitchPollTimer.Interval = 60000;
            _twitchPollTimer.AutoReset = true;
            _twitchPollTimer.Start();

            _databaseSyncTimer = new System.Timers.Timer();
            _databaseSyncTimer.Elapsed += SyncDatabase;
            _databaseSyncTimer.Interval = 60000 * DATABASE_SYNC_TIMER_COOLDOWN_MINUTES;
            _databaseSyncTimer.AutoReset = true;
            _databaseSyncTimer.Start();
        }

        private async void PollTwitchStreams(object Sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // we always init first
                if (!HasInit)
                {
                    if (!Initing)
                    {
                        try
                        {
                            Initing = true;
                            SetUpAllStreamers();
                        }
                        catch (Exception ex)
                        {
                            AegisLog.Log(ex.Message, ex);
                            return;
                        }
                        finally
                        {
                            Initing = false;
                        }
                    }
                }
                
                if (HasInit && !IsPolling)
                {
                    try
                    {
                        IsPolling = true;
                        if (AccessToken == "")
                        {
                            await GetNewToken().ConfigureAwait(false);
                        }
                        await TryPollTwitchStreams().ConfigureAwait(false);
                        await UpdateAllRoles().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        AegisLog.Log(ex.Message, ex);
                    }
                    finally
                    {
                        IsPolling = false;
                    }
                }
            }
            finally
            {
                if (!_twitchPollTimer.Enabled)
                {
                    _twitchPollTimer.Start();
                }
            }
        }

        private async void SyncDatabase(object Sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var uow = _db.UnitOfWork();
                foreach (var liveUserGroup in LiveUsers)
                {
                    foreach (var liveUser in liveUserGroup)
                    {
                        uow.LiveUsers.AddOrUpdateByLiveUser(liveUser);
                    }
                }
                await uow.SaveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AegisLog.Log(ex.Message, ex);
            }
            finally
            {
                if (!_twitchPollTimer.Enabled)
                {
                    _twitchPollTimer.Start();
                }
            }
        }

        private void SetUpAllStreamers()
        {
            try
            {
                var liveUsers = new List<LiveUser>();
                var uow = _db.UnitOfWork();
                var liveUsersDb = uow.LiveUsers.GetAll();
                foreach (var liveUserDb in liveUsersDb)
                {
                    var liveUser = new LiveUser
                    {
                        GuildId = liveUserDb.GuildId,
                        UserId = liveUserDb.UserId,
                        TwitchName = liveUserDb.TwitchName,
                        PriorityUser = liveUserDb.PriorityUser,
                        TwitchAlert = liveUserDb.TwitchAlert,
                        IsStreaming = liveUserDb.IsStreaming,
                        LastStreamed = liveUserDb.LastStreamed,
                        StreamStateChanged = false,
                        HasRole = liveUserDb.HasRole
                    };
                    liveUsers.Add(liveUser);
                }
                LiveUsers = liveUsers.GroupBy(x => x.GuildId).ToList();
            }
            catch (Exception ex)
            {
                AegisLog.Log(ex.Message, ex);
                return;
            }
            HasInit = true;
        }

        public async Task AddOrUpdateTwitchName(ulong guildId, ulong userId, string twitchName)
        {
            var uow = _db.UnitOfWork();
            uow.LiveUsers.UpdateTwitchName(guildId, userId, twitchName);
            await uow.SaveAsync().ConfigureAwait(false);

            var oldLiveUser = LiveUsers.FirstOrDefault(x => x.Key == guildId)?.FirstOrDefault(x => x.UserId == userId);

            LiveUser liveUser;
            if (oldLiveUser == null)
            {
                liveUser = new LiveUser
                {
                    GuildId = guildId,
                    UserId = userId,
                    TwitchName = twitchName
                };
                var liveUsers = LiveUsers.FirstOrDefault(x => x.Key == liveUser.GuildId).ToList();
                LiveUsers.RemoveAll(x => x.Key == liveUser.GuildId);
                liveUsers.Add(liveUser);
                var liveUserGroup = liveUsers.GroupBy(x => x.GuildId).First();
                LiveUsers.Add(liveUserGroup);
            } else
            {
                liveUser = oldLiveUser;
                liveUser.TwitchName = twitchName;
            }
        }

        public async Task RemoveLiveUser(ulong guildId, ulong userId)
        {
            var uow = _db.UnitOfWork();
            uow.LiveUsers.RemoveByGuildIdUserId(guildId, userId);
            await uow.SaveAsync().ConfigureAwait(false);

            // remove from LiveUsers
            var liveUsers = LiveUsers.FirstOrDefault(x => x.Key == guildId);
            LiveUsers.RemoveAll(x => x.Key == guildId);
            var liveUserGroup = liveUsers.Where(x => x.UserId != userId).GroupBy(x => x.GuildId).First();
            LiveUsers.Add(liveUserGroup);
        }

        public List<LiveUser> GetLiveUsersByGuildId(ulong guildId)
        {
            return LiveUsers.FirstOrDefault(x => x.Key == guildId)?.ToList();
        }

        public async Task<bool> TogglePriorityUser(ulong guildId, ulong userId)
        {
            var uow = _db.UnitOfWork();
            uow.LiveUsers.TogglePriorityUser(guildId, userId);
            await uow.SaveAsync().ConfigureAwait(false);

            var liveUser = LiveUsers.FirstOrDefault(x => x.Key == guildId)?.FirstOrDefault(x => x.UserId == userId);
            liveUser.PriorityUser = !liveUser.PriorityUser;
            return liveUser.PriorityUser;
        }

        public async Task<bool> ToggleAlertUser(ulong guildId, ulong userId)
        {
            var uow = _db.UnitOfWork();
            uow.LiveUsers.ToggleAlertUser(guildId, userId);
            await uow.SaveAsync().ConfigureAwait(false);

            var liveUser = LiveUsers.FirstOrDefault(x => x.Key == guildId)?.FirstOrDefault(x => x.UserId == userId);
            liveUser.TwitchAlert = !liveUser.TwitchAlert;
            return liveUser.TwitchAlert;
        }

        private async Task UpdateAllRoles()
        {
            foreach (var liveUsersGroup in LiveUsers)
            {
                var hasGuild = _client.Guilds.TryGetValue(liveUsersGroup.Key, out var guild);
                if (!hasGuild)
                {
                    AegisLog.Log($"Bot is not in guild #{liveUsersGroup.Key}");
                    continue;
                }

                var uow = _db.UnitOfWork();
                var serverSetting = uow.ServerSettings.GetOrAddByGuildId(liveUsersGroup.Key);
                await uow.SaveAsync().ConfigureAwait(false);

                var role = guild.GetRole(serverSetting.RoleId);
                if (role == null)
                {
                    AegisLog.Log($"Role not set for guild #{liveUsersGroup.Key}");
                    continue;
                }

                var twitchAlertChannel = guild.GetChannel(serverSetting.TwitchChannelId);
                if (twitchAlertChannel == null && serverSetting.TwitchAlertMode)
                {
                    AegisLog.Log($"Twitch alert channel not set for guild #{liveUsersGroup.Key}");
                }

                foreach (var liveUser in liveUsersGroup)
                {
                    var user = await guild.GetMemberAsync(liveUser.UserId).ConfigureAwait(false);
                    if (user == null)
                    {
                        AegisLog.Log($"User does not exist!");
                        await RemoveLiveUser(liveUser.GuildId, liveUser.UserId).ConfigureAwait(false);
                        continue;
                    }

                    if (liveUser.StreamStateChanged)
                    {
                        if (liveUser.HasRole)
                        {
                            await user.RevokeRoleAsync(role).ConfigureAwait(false);
                        } else
                        {
                            if (serverSetting.TwitchAlertMode && liveUser.TwitchAlert
                                && liveUser.LastStreamed.AddHours(STREAM_ALERT_COOLDOWN_HOUR) < DateTime.UtcNow)
                            {
                                await twitchAlertChannel.SendMessageAsync($"@everyone streamer is LIVE YO https://www.twitch.tv/{liveUser.TwitchName}").ConfigureAwait(false);
                            }
                            liveUser.LastStreamed = DateTime.UtcNow;
                            await user.GrantRoleAsync(role).ConfigureAwait(false);
                        }
                        liveUser.StreamStateChanged = false;
                        liveUser.HasRole = !liveUser.HasRole;
                    }
                }
            }
        }

        private async Task TryPollTwitchStreams()
        {
            var uow = _db.UnitOfWork();
            var hcHandle = new HttpClientHandler();
            foreach (var liveUsersGroup in LiveUsers)
            {
                var serverSetting = uow.ServerSettings.GetOrAddByGuildId(liveUsersGroup.Key);
                await uow.SaveAsync().ConfigureAwait(false);
                if (!serverSetting.PriorityMode)
                {
                    await TryPollTwitchStreamBulk(hcHandle, liveUsersGroup).ConfigureAwait(false);
                }
                else
                {
                    var hasPriorityStream = false;

                    // check if at least one priority user is streaming
                    var priorityUsers = liveUsersGroup.Where(x => x.PriorityUser == true);
                    var isStreaming = await TryPollTwitchStreamBulk(hcHandle, priorityUsers).ConfigureAwait(false);
                    if (isStreaming)
                    {
                        hasPriorityStream = true;
                    }

                    var nonPriorityUsers = liveUsersGroup.Where(x => x.PriorityUser == false);
                    if (hasPriorityStream)
                    {
                        // all that were LIVE here will change to not streaming and marked
                        foreach (var liveUser in nonPriorityUsers)
                        {
                            if (liveUser.HasRole)
                            {
                                liveUser.StreamStateChanged = true;
                            }
                        }
                    }
                    else
                    {
                        await TryPollTwitchStreamBulk(hcHandle, nonPriorityUsers).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<bool> TryPollTwitchStreamBulk(HttpClientHandler hcHandle, IEnumerable<LiveUser> liveUsers)
        {
            // return value
            var hasLive = false;

            // error checking here
            if (liveUsers.Count() == 0)
            {
                return false;
            }
            
            var uow = _db.UnitOfWork();
            var hasGuild = _client.Guilds.TryGetValue(liveUsers.First().GuildId, out var guild);
            if (!hasGuild)
            {
                AegisLog.Log($"Server does not exist!");
                return false;
            }

            var hc = new HttpClient(hcHandle, false);
            hc.DefaultRequestHeaders.Add("Client-ID", TwitchClientId);
            hc.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            hc.DefaultRequestHeaders.UserAgent.ParseAdd("AegisLiveBot");
            hc.Timeout = TimeSpan.FromSeconds(15);

            // separate them into bulks of 100 users (most likely wont hit 100 users)
            var bulkLiveUsers = new List<List<LiveUser>>();
            var currentBulk = new List<LiveUser>();
            foreach (var liveUser in liveUsers)
            {
                currentBulk.Add(liveUser);
                if (currentBulk.Count >= 100)
                {
                    bulkLiveUsers.Add(currentBulk);
                    currentBulk = new List<LiveUser>();
                }
            }
            if (currentBulk.Count() != 0)
            {
                bulkLiveUsers.Add(currentBulk);
            }

            // call twitch api for each bulk
            foreach (var liveUserBulk in bulkLiveUsers)
            {
                var userLoginString = "user_login=";
                userLoginString += string.Join("&user_login=", liveUserBulk.Select(x => x.TwitchName).ToArray());
                CancellationToken cancellationToken = default;
                var response = await hc.GetAsync($"https://api.twitch.tv/helix/streams?{userLoginString}", cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        // if not OK
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            await GetNewToken().ConfigureAwait(false);
                            continue;
                        }

                        // if ratelimit reaching limits, wait a bit
                        var limit = int.Parse(response.Headers.FirstOrDefault(x => x.Key == "Ratelimit-Remaining").Value.ToList()[0]);
                        if (limit <= 5)
                        {
                            await Task.Delay(5000).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        AegisLog.Log(ex.Message);
                        await Task.Delay(5000).ConfigureAwait(false);
                    }

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var jsonData = jsonObject["data"];
                    // jsonData has the live streams returned
                    foreach (var liveUser in liveUserBulk)
                    {
                        var streamData = jsonData.FirstOrDefault(x => x["user_name"].ToString().ToLower() == liveUser.TwitchName.ToLower());
                        var jsonType = streamData != null ? streamData["type"] : null;
                        if (streamData == null || jsonType == null || jsonType.ToString() != "live")
                        {
                            liveUser.IsStreaming = false;
                            if (liveUser.HasRole)
                            {
                                liveUser.StreamStateChanged = true;
                            }
                        } else
                        {
                            hasLive = true;
                            liveUser.IsStreaming = true;
                            if (!liveUser.HasRole)
                            {
                                liveUser.StreamStateChanged = true;
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    AegisLog.Log(ex.Message, ex);
                }
            }
            return hasLive;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            _accessTokenTimer.Enabled = false;
        }

        private async Task GetNewToken()
        {
            if (_accessTokenTimer.Enabled)
            {
                return;
            }
            _accessTokenTimer.Enabled = true;
            try
            {
                var hcHandle = new HttpClientHandler();
                var hc = new HttpClient(hcHandle, false);
                hc.Timeout = TimeSpan.FromSeconds(5);
                var stringContent = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("client_id", TwitchClientId),
                new KeyValuePair<string, string>("client_secret", TwitchClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await hc.PostAsync("https://id.twitch.tv/oauth2/token", stringContent);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(jsonString);
                var jsonData = jsonObject["access_token"];
                AccessToken = jsonData.ToString();
            } catch(Exception e)
            {
                AegisLog.Log(e.Message, e);
            }
        }
    }
}
