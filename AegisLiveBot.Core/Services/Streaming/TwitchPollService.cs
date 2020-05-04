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
    public interface ITwitchPollService : IStartUpService
    {

    }
    public class TwitchPollService : ITwitchPollService
    {
        private readonly DbService _db;
        private readonly DiscordClient _client;
        private readonly Timer _twitchPollTimer;
        private string TwitchClientId = "";
        private string TwitchClientSecret = "";
        private string AccessToken = "";
        private bool IsPolling = false;
        public TwitchPollService(DbService db, DiscordClient client, ConfigJson configJson)
        {
            _db = db;
            _client = client;
            TwitchClientId = configJson.TwitchClientId;
            TwitchClientSecret = configJson.TwitchClientSecret;

        _twitchPollTimer = new Timer(async (state) =>
            {
                if (!IsPolling)
                {
                    if (AccessToken == "")
                    {
                        GetNewToken();
                    }
                        await TryPollTwitchStreams().ConfigureAwait(false);
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }
        private async Task TryPollTwitchStreams()
        {
            using (var uow = _db.UnitOfWork())
            {
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
                                    Console.WriteLine($"Server or User does not exist!");
                                    uow.LiveUsers.RemoveByGuildIdUserId(nonPriorityUser.GuildId, nonPriorityUser.UserId);
                                    await uow.SaveAsync().ConfigureAwait(false);
                                    continue;
                                }
                                var role = guild.GetRole(serverSetting.RoleId);
                                if (role == null)
                                {
                                    Console.WriteLine($"Role does not exist!");
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
        }
        private async Task<bool> TryPollTwitchStream(HttpClientHandler hcHandle, LiveUser liveUser)
        {
            using (var uow = _db.UnitOfWork())
            {
                using (var hc = new HttpClient(hcHandle, false))
                {
                    hc.DefaultRequestHeaders.Add("Client-ID", TwitchClientId);
                    hc.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
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
                        try
                        {
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
                                uow.LiveUsers.RemoveByGuildIdUserId(liveUser.GuildId, liveUser.UserId);
                                await uow.SaveAsync().ConfigureAwait(false);
                                return false;
                            }
                            var serverSetting = uow.ServerSettings.GetOrAddByGuildId(liveUser.GuildId);
                            await uow.SaveAsync().ConfigureAwait(false);
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
                        } catch(Exception e)
                        {
                            await GetNewToken().ConfigureAwait(false);
                            Console.WriteLine(e.Message);
                        }
                    }
                    return false;
                }
            }
        }
        private async Task GetNewToken()
        {
            var hcHandle = new HttpClientHandler();
            using (var hc = new HttpClient(hcHandle, false))
            {
                hc.Timeout = TimeSpan.FromSeconds(5);
                var stringContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", TwitchClientId),
                    new KeyValuePair<string, string>("client_secret", TwitchClientSecret),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                using (var response = await hc.PostAsync("https://id.twitch.tv/oauth2/token", stringContent))
                {
                    response.EnsureSuccessStatusCode();
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonString);
                    var jsonData = jsonObject["access_token"];
                    AccessToken = jsonData.ToString();
                }
            }
        }
    }
}
