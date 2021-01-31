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

        private readonly System.Timers.Timer _accessTokenTimer;
        public TwitchPollService(DbService db, DiscordClient client, ConfigJson configJson)
        {
            _db = db;
            _client = client;
            TwitchClientId = configJson.TwitchClientId;
            TwitchClientSecret = configJson.TwitchClientSecret;
            _accessTokenTimer = new System.Timers.Timer(60000);
            _accessTokenTimer.Elapsed += OnTimedEvent;
            _accessTokenTimer.Enabled = false;
            _twitchPollTimer = new System.Timers.Timer();
            _twitchPollTimer.Elapsed += PollTwitchStreams;
            _twitchPollTimer.Interval = 60000;
            _twitchPollTimer.AutoReset = true;
            _twitchPollTimer.Start();
        }
        private async void PollTwitchStreams(object Sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!IsPolling)
                {
                    if (AccessToken == "")
                    {
                        await GetNewToken().ConfigureAwait(false);
                    }
                    await TryPollTwitchStreams().ConfigureAwait(false);
                }
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
        private async Task TryPollTwitchStreams()
        {
            var uow = _db.UnitOfWork();
            IsPolling = true;
            var hcHandle = new HttpClientHandler();
            var liveUsersGroupByServer = uow.LiveUsers.GetAll().GroupBy(x => x.GuildId);
            foreach (var liveUsersGroup in liveUsersGroupByServer)
            {
                var serverSetting = uow.ServerSettings.GetOrAddByGuildId(liveUsersGroup.Key);
                await uow.SaveAsync().ConfigureAwait(false);
                if (!serverSetting.PriorityMode)
                {
                    foreach (var liveUser in liveUsersGroup)
                    {
                        await TryPollTwitchStream(hcHandle, liveUser).ConfigureAwait(false);
                    }
                }
                else
                {
                    var hasPriorityStream = false;
                    var priorityUsers = liveUsersGroup.Where(x => x.PriorityUser == true);
                    foreach (var priorityUser in priorityUsers)
                    {
                        var isStreaming = await TryPollTwitchStream(hcHandle, priorityUser).ConfigureAwait(false);
                        if (isStreaming)
                        {
                            hasPriorityStream = true;
                        }
                    }
                    var nonPriorityUsers = liveUsersGroup.Where(x => x.PriorityUser == false);
                    foreach (var nonPriorityUser in nonPriorityUsers)
                    {
                        if (hasPriorityStream)
                        {
                            var guild = _client.Guilds.FirstOrDefault(x => x.Value.Id == liveUsersGroup.Key).Value;
                            var user = await guild.GetMemberAsync(nonPriorityUser.UserId).ConfigureAwait(false);
                            if (guild == null || user == null)
                            {
                                AegisLog.Log($"Server or User does not exist!");
                                uow.LiveUsers.RemoveByGuildIdUserId(nonPriorityUser.GuildId, nonPriorityUser.UserId);
                                await uow.SaveAsync().ConfigureAwait(false);
                                continue;
                            }
                            var role = guild.GetRole(serverSetting.RoleId);
                            if (role == null)
                            {
                                AegisLog.Log($"Role does not exist!");
                                continue;
                            }
                            await user.RevokeRoleAsync(role);
                        }
                        else
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
            var uow = _db.UnitOfWork();
            var hc = new HttpClient(hcHandle, false);
            hc.DefaultRequestHeaders.Add("Client-ID", TwitchClientId);
            hc.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            hc.DefaultRequestHeaders.UserAgent.ParseAdd("AegisLiveBot");
            hc.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                CancellationToken cancellationToken = default;
                var response = await hc.GetAsync($"https://api.twitch.tv/helix/streams?user_login={liveUser.TwitchName}", cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        response.EnsureSuccessStatusCode();
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

                    var responseError = "";
                    try
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var jsonObject = JObject.Parse(jsonString);
                        var jsonError = jsonObject["status"];
                        if (jsonError != null)
                        {
                            responseError = jsonObject["status"].ToString();
                        }
                        var jsonData = jsonObject["data"];
                        JToken jsonType = null;
                        if (jsonData.Count() != 0)
                        {
                            jsonType = jsonData[0]["type"];
                        }

                        var guild = _client.Guilds.FirstOrDefault(x => x.Value.Id == liveUser.GuildId).Value;
                        if (guild == null)
                        {
                            AegisLog.Log($"Server does not exist!");
                            return false;
                        }
                        var user = await guild.GetMemberAsync(liveUser.UserId).ConfigureAwait(false);
                        if (guild == null || user == null)
                        {
                            AegisLog.Log($"Server or User does not exist!");
                            uow.LiveUsers.RemoveByGuildIdUserId(liveUser.GuildId, liveUser.UserId);
                            await uow.SaveAsync().ConfigureAwait(false);
                            return false;
                        }
                        var serverSetting = uow.ServerSettings.GetOrAddByGuildId(liveUser.GuildId);
                        await uow.SaveAsync().ConfigureAwait(false);
                        if (serverSetting == null || serverSetting.RoleId == 0)
                        {
                            AegisLog.Log($"Streamer role not set!");
                        }
                        else
                        {
                            var role = guild.GetRole(serverSetting.RoleId);
                            if (role == null)
                            {
                                AegisLog.Log($"Role does not exist!");
                                return false;
                            }
                            if (jsonType != null && jsonType.ToString() == "live")
                            {
                                if(user.Roles.FirstOrDefault(x => x == role) == null)
                                {
                                    await user.GrantRoleAsync(role);
                                    if (serverSetting.TwitchAlertMode)
                                    {
                                        var msg = $"@everyone streamer live yo https://www.twitch.tv/{liveUser.TwitchName}";
                                        var ch = guild.Channels.FirstOrDefault(x => x.Value.Id == serverSetting.TwitchChannelId).Value;
                                        if(ch != null)
                                        {
                                            await ch.SendMessageAsync(msg).ConfigureAwait(false);
                                        } else
                                        {
                                            AegisLog.Log($"Twitch alert channel not set!");
                                        }
                                    }
                                }
                                return true;
                            }
                            else
                            {
                                await user.RevokeRoleAsync(role);
                                return false;
                            }
                        }
                        return false;
                    }
                    catch (Exception e)
                    {
                        if (responseError == "401")
                        {
                            await GetNewToken().ConfigureAwait(false);
                        }
                        AegisLog.Log(e.Message, e);
                    }

                } catch(Exception ex)
                {
                    AegisLog.Log(ex.Message, ex);
                }
                return false;
            } catch(Exception e)
            {
                AegisLog.Log(e.Message, e);
            }
            return false;
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
