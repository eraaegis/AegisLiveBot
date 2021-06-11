using AegisLiveBot.Core.Common;
using AegisLiveBot.DAL;
using AegisLiveBot.DAL.Models.Inhouse;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
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

namespace AegisLiveBot.Core.Services.Inhouse
{
    public interface IInhouseService : IStartUpService
    {
        Task SetQueueChannel(DiscordChannel channel);
        Task UnsetQueueChannel(DiscordChannel channel);
        Task ResetQueueChannel(DiscordChannel channel);
        Task QueueUp(DiscordChannel channel, DiscordMember user, PlayerRole role, DiscordMember other = null);
        Task Unqueue(DiscordChannel channel, DiscordMember user);
        Task TeamUp(DiscordChannel channel, DiscordMember player, DiscordMember other);
        Task ShowQueue(DiscordChannel channel, string message = "");
        Task ConfirmWin(DiscordChannel channel, DiscordMember user);
    }
    public class InhouseService : IInhouseService
    {
        private readonly DbService _db;
        private readonly DiscordClient _client;
        private readonly string _prefix;

        private List<InhouseQueue> InhouseQueues;
        private List<InhouseGame> InhouseGames;

        private readonly Dictionary<PlayerStatus, string> _statusEmojis;

        public InhouseService(DbService db, DiscordClient client, ConfigJson configJson)
        {
            _db = db;
            _client = client;
            _prefix = configJson.Prefix;

            _statusEmojis = new Dictionary<PlayerStatus, string>();
            _statusEmojis.Add(PlayerStatus.None, "❔");
            _statusEmojis.Add(PlayerStatus.Ready, "✅");
            _statusEmojis.Add(PlayerStatus.NotReady, "❌");

            SetUpQueues();
        }

        private void SetUpQueues()
        {
            InhouseQueues = new List<InhouseQueue>();
            InhouseGames = new List<InhouseGame>();
            try
            {
                var uow = _db.UnitOfWork();
                var inhouses = uow.Inhouses.GetAll();
                foreach(var inhouse in inhouses)
                {
                    var inhouseQueue = new InhouseQueue(inhouse.ChannelId);
                    inhouseQueue.Emojis[PlayerRole.Top] = inhouse.TopEmoji;
                    inhouseQueue.Emojis[PlayerRole.Jgl] = inhouse.JglEmoji;
                    inhouseQueue.Emojis[PlayerRole.Mid] = inhouse.MidEmoji;
                    inhouseQueue.Emojis[PlayerRole.Bot] = inhouse.BotEmoji;
                    inhouseQueue.Emojis[PlayerRole.Sup] = inhouse.SupEmoji;
                    inhouseQueue.Emojis[PlayerRole.Fill] = inhouse.FillEmoji;
                    InhouseQueues.Add(inhouseQueue);
                }
            }
            catch (Exception ex)
            {
                AegisLog.Log(ex.Message, ex);
            }
        }

        public async Task SetQueueChannel(DiscordChannel channel)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue != null)
            {
                return;
            }

            inhouseQueue = new InhouseQueue(channel.Id);
            inhouseQueue = await SetUpEmoji(inhouseQueue).ConfigureAwait(false);

            var uow = _db.UnitOfWork();
            uow.Inhouses.SetByInhouseQueue(inhouseQueue);
            await uow.SaveAsync().ConfigureAwait(false);
            await channel.SendMessageAsync($"Inhouse Queue added to {channel.Mention}").ConfigureAwait(false);
            InhouseQueues.Add(inhouseQueue);
        }

        public async Task UnsetQueueChannel(DiscordChannel channel)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            var uow = _db.UnitOfWork();
            uow.Inhouses.UnsetByChannelId(channel.Id);
            await uow.SaveAsync().ConfigureAwait(false);
            await channel.SendMessageAsync($"Inhouse Queue removed from {channel.Mention}").ConfigureAwait(false);
            InhouseQueues.Remove(inhouseQueue);
        }

        public async Task ResetQueueChannel(DiscordChannel channel)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            InhouseQueues.Remove(inhouseQueue);
            inhouseQueue = await SetUpEmoji(inhouseQueue).ConfigureAwait(false);

            var uow = _db.UnitOfWork();
            uow.Inhouses.SetByInhouseQueue(inhouseQueue);
            await uow.SaveAsync().ConfigureAwait(false);
            await channel.SendMessageAsync($"Inhouse Queue is reset from {channel.Mention}").ConfigureAwait(false);
            InhouseQueues.Add(inhouseQueue);
        }

        private async Task<InhouseQueue> SetUpEmoji(InhouseQueue inhouseQueue)
        {
            var channel = await _client.GetChannelAsync(inhouseQueue.ChannelId).ConfigureAwait(false);
            var emojis = await channel.Guild.GetEmojisAsync().ConfigureAwait(false);
            inhouseQueue.Emojis[PlayerRole.Top] = emojis.FirstOrDefault(x => x.Name == "TOP") ?? "TOP:";
            inhouseQueue.Emojis[PlayerRole.Jgl] = emojis.FirstOrDefault(x => x.Name == "JGL") ?? "JGL:";
            inhouseQueue.Emojis[PlayerRole.Mid] = emojis.FirstOrDefault(x => x.Name == "MID") ?? "MID:";
            inhouseQueue.Emojis[PlayerRole.Bot] = emojis.FirstOrDefault(x => x.Name == "BOT") ?? "BOT:";
            inhouseQueue.Emojis[PlayerRole.Sup] = emojis.FirstOrDefault(x => x.Name == "SUP") ?? "SUP:";
            inhouseQueue.Emojis[PlayerRole.Fill] = emojis.FirstOrDefault(x => x.Name == "FILL") ?? "FILL:";
            return inhouseQueue;
        }

        public async Task QueueUp(DiscordChannel channel, DiscordMember user, PlayerRole role, DiscordMember other = null)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            var queueGroup = inhouseQueue.PlayersInQueue.FirstOrDefault(x => x.Players.Any(y => y.Player.Id == user.Id));
            InhousePlayer inhousePlayer = null;
            if (queueGroup == null)
            {
                queueGroup = new QueueGroup();
                inhousePlayer = new InhousePlayer(user);
                queueGroup.Players.Add(inhousePlayer);
                inhouseQueue.PlayersInQueue.Add(queueGroup);
            } else
            {
                inhousePlayer = queueGroup.Players.FirstOrDefault(x => x.Player.Id == user.Id);
            }

            // if the player in question is already a fill player, do nothing
            if (!inhousePlayer.QueuedRoles[PlayerRole.Fill])
            {
                switch (role)
                {
                    case PlayerRole.Top:
                        inhousePlayer.QueuedRoles[PlayerRole.Top] = true;
                        break;
                    case PlayerRole.Jgl:
                        inhousePlayer.QueuedRoles[PlayerRole.Jgl] = true;
                        break;
                    case PlayerRole.Mid:
                        inhousePlayer.QueuedRoles[PlayerRole.Mid] = true;
                        break;
                    case PlayerRole.Bot:
                        inhousePlayer.QueuedRoles[PlayerRole.Bot] = true;
                        break;
                    case PlayerRole.Sup:
                        inhousePlayer.QueuedRoles[PlayerRole.Sup] = true;
                        break;
                    case PlayerRole.Fill:
                        inhousePlayer.QueuedRoles[PlayerRole.Top] = false;
                        inhousePlayer.QueuedRoles[PlayerRole.Jgl] = false;
                        inhousePlayer.QueuedRoles[PlayerRole.Mid] = false;
                        inhousePlayer.QueuedRoles[PlayerRole.Bot] = false;
                        inhousePlayer.QueuedRoles[PlayerRole.Sup] = false;
                        inhousePlayer.QueuedRoles[PlayerRole.Fill] = true;
                        break;
                }

                if (inhousePlayer.QueuedRoles[PlayerRole.Top] && inhousePlayer.QueuedRoles[PlayerRole.Jgl] &&
                    inhousePlayer.QueuedRoles[PlayerRole.Mid] && inhousePlayer.QueuedRoles[PlayerRole.Bot] && inhousePlayer.QueuedRoles[PlayerRole.Sup])
                {
                    inhousePlayer.QueuedRoles[PlayerRole.Top] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Jgl] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Mid] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Bot] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Sup] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Fill] = true;
                }
            }

            // attempt to join team if provided
            if (other != null)
            {
                await TeamUp(channel, user, other).ConfigureAwait(false);
            } else
            {
                await ShowQueue(channel).ConfigureAwait(false);
            }

            await AttemptMatchmake(channel).ConfigureAwait(false);
        }

        public async Task Unqueue(DiscordChannel channel, DiscordMember user)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            RemovePlayerFromQueueGroup(inhouseQueue, user);

            await ShowQueue(channel).ConfigureAwait(false);
        }

        public async Task TeamUp(DiscordChannel channel, DiscordMember player, DiscordMember other)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            var queueGroup = inhouseQueue.PlayersInQueue.FirstOrDefault(x => x.Players.Any(y => y.Player.Id == player.Id));
            if (queueGroup == null)
            {
                return;
            }
            var queueGroupIndex = inhouseQueue.PlayersInQueue.IndexOf(queueGroup);

            var inhousePlayer = queueGroup.Players.FirstOrDefault(x => x.Player.Id == player.Id);
            var otherQueueGroup = inhouseQueue.PlayersInQueue.FirstOrDefault(x => x.Players.Any(y => y.Player.Id == other.Id));
            if (otherQueueGroup == null)
            {
                return;
            }
            var otherQueueGroupIndex = inhouseQueue.PlayersInQueue.IndexOf(otherQueueGroup);

            var msg = "";
            if (queueGroup == otherQueueGroup)
            {
                msg = "**WARNING**: Cannot join your own team.";
            }
            else if (otherQueueGroup.CanAddPlayer(inhousePlayer))
            {
                // dirty code, remove from queue group first, then add player
                RemovePlayerFromQueueGroup(inhouseQueue, player);
                otherQueueGroup.Players.Add(inhousePlayer);
                // if otherQueueGroup is earlier, then move it back to the caller's location in list to avoid skipping queue
                if (otherQueueGroupIndex < queueGroupIndex)
                {
                    inhouseQueue.PlayersInQueue.Remove(otherQueueGroup);
                    inhouseQueue.PlayersInQueue.Insert(queueGroupIndex - 1, otherQueueGroup);
                }
            }
            else
            {
                msg = "**WARNING**: Unable to join team: No possible role combination found for group on the same team.";
            }
            await ShowQueue(channel, msg).ConfigureAwait(false);
        }

        private void RemovePlayerFromQueueGroup(InhouseQueue inhouseQueue, DiscordMember user)
        {
            var queueGroup = inhouseQueue.PlayersInQueue.FirstOrDefault(x => x.Players.Any(y => y.Player.Id == user.Id));
            if (queueGroup == null)
            {
                return;
            }

            queueGroup.Players.RemoveAll(x => x.Player.Id == user.Id);
            if (queueGroup.Players.Count() == 0)
            {
                inhouseQueue.PlayersInQueue.Remove(queueGroup);
            }
        }

        public async Task ShowQueue(DiscordChannel channel, string message = "")
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            var embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.Title = "Queue";
            var emojiWarning = "";
            if (inhouseQueue.Emojis[PlayerRole.Top] == "TOP:" ||
                inhouseQueue.Emojis[PlayerRole.Jgl] == "JGL:" ||
                inhouseQueue.Emojis[PlayerRole.Mid] == "MID:" ||
                inhouseQueue.Emojis[PlayerRole.Bot] == "BOT:" ||
                inhouseQueue.Emojis[PlayerRole.Sup] == "SUP:" ||
                inhouseQueue.Emojis[PlayerRole.Fill] == "FILL:")
            {
                emojiWarning = $"\nAdd the corresponding emojis with name :TOP:, :JGL:, :MID:, :BOT:, :SUP:, :FILL:, and use {_prefix}resetqueuech [channel] to use emojis.";
            }

            var currentGroup = 0;
            var playersStrings = new Dictionary<PlayerRole, List<string>>();
            playersStrings.Add(PlayerRole.Top, new List<string>());
            playersStrings.Add(PlayerRole.Jgl, new List<string>());
            playersStrings.Add(PlayerRole.Mid, new List<string>());
            playersStrings.Add(PlayerRole.Bot, new List<string>());
            playersStrings.Add(PlayerRole.Sup, new List<string>());
            playersStrings.Add(PlayerRole.Fill, new List<string>());
            foreach (var queueGroup in inhouseQueue.PlayersInQueue)
            {
                var currentGroupString = "";
                if (queueGroup.Players.Count() > 1)
                {
                    currentGroupString = $"**[{currentGroup}]**";
                    currentGroup += 1;
                }

                foreach(var player in queueGroup.Players)
                {
                    foreach(var playerRole in player.QueuedRoles.Where(x => x.Value).Select(x => x.Key))
                    {
                        playersStrings[playerRole].Add($"{currentGroupString}{player.Player.DisplayName}");
                    }
                }
            }

            embedBuilder.Description = $"{inhouseQueue.Emojis[PlayerRole.Top]} {string.Join(", ", playersStrings[PlayerRole.Top])}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Jgl]} {string.Join(", ", playersStrings[PlayerRole.Jgl])}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Mid]} {string.Join(", ", playersStrings[PlayerRole.Mid])}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Bot]} {string.Join(", ", playersStrings[PlayerRole.Bot])}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Sup]} {string.Join(", ", playersStrings[PlayerRole.Sup])}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Fill]} {string.Join(", ", playersStrings[PlayerRole.Fill])}\n\n" +
                "Commands:\n" +
                $"{_prefix}queue [role] - Queue up with role\n" +
                $"{_prefix}queue [role] [player] - Join player's team with role\n" +
                $"{_prefix}teamup [player] - Join player's team\n" +
                $"{_prefix}leave - Leave the queue\n" +
                emojiWarning;

            await channel.SendMessageAsync(message, embed: embedBuilder.Build()).ConfigureAwait(false);
        }

        private async Task AttemptMatchmake(DiscordChannel channel)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            
            // if less than 10 players in queue, no game can be made
            if (inhouseQueue.PlayersInQueue.Sum(x => x.Players.Count()) < 10)
            {
                return;
            }

            // brute force check for matchmake with buckets
            var buckets = new List<MatchmakeBucket>();
            buckets.Add(new MatchmakeBucket());

            // we reserve SOLO fill players for last
            MatchmakeBucket filledBucket = null;
            var fillPlayers = new List<QueueGroup>();

            foreach(var queueGroup in inhouseQueue.PlayersInQueue)
            {
                var tempBuckets = new List<MatchmakeBucket>();

                // check for SOLO fill players
                if (queueGroup.Players.Count() == 1 && queueGroup.Players[0].QueuedRoles[PlayerRole.Fill])
                {
                    fillPlayers.Add(queueGroup);
                    var maxBucket = buckets.FirstOrDefault(x => x.PlayerCount + fillPlayers.Count() >= 10);
                    if (maxBucket != null)
                    {
                        filledBucket = maxBucket;
                        break;
                    }
                    continue;
                }

                // get each role combination from each queueGroup
                foreach (var bucket in buckets)
                {
                    // check to see if a copy of bucket without roleCombination is already in
                    var hasOldBucket = false;
                    var roleCombinations = queueGroup.PossibleRoleCombinations();
                    while (roleCombinations.Count() > 0)
                    {
                        var scramble = AegisRandom.RandomNumber(0, roleCombinations.Count());
                        var roleCombination = roleCombinations[scramble];
                        roleCombinations.RemoveAt(scramble);
                        var playerSides = AegisRandom.RandomBool() ? new List<PlayerSide>() { PlayerSide.Blue, PlayerSide.Red } : new List<PlayerSide>() { PlayerSide.Red, PlayerSide.Blue };
                        foreach (var i in playerSides)
                        {
                            var tempBucket = new MatchmakeBucket(bucket);
                            var canPutIntoBucket = tempBucket.TryAddRoleCombination(roleCombination, i);
                            if (canPutIntoBucket)
                            {
                                tempBucket.QueueGroups.Add(queueGroup);
                                tempBuckets.Add(tempBucket);
                            }
                            else if (!hasOldBucket)
                            {
                                tempBuckets.Add(tempBucket);
                                hasOldBucket = true;
                            }

                            if (tempBucket.PlayerCount + fillPlayers.Count() >= 10)
                            {
                                filledBucket = tempBucket;
                                break;
                            }
                        }
                        if (filledBucket != null)
                        {
                            break;
                        }
                    }
                    if (filledBucket != null)
                    {
                        break;
                    }
                }
                buckets = tempBuckets;
                if (filledBucket != null)
                {
                    break;
                }
            }

            // if no bucket is filled, then no game is found
            if (filledBucket == null)
            {
                return;
            }

            // now we put all fill players in
            var missingBlueRoles = new List<PlayerRole>();
            var missingRedRoles = new List<PlayerRole>();
            foreach (PlayerRole role in Enum.GetValues(typeof(PlayerRole)))
            {
                if (role == PlayerRole.Fill)
                {
                    continue;
                }
                missingBlueRoles.Add(role);
                missingRedRoles.Add(role);
            }
            foreach (var role in filledBucket.BluePlayers)
            {
                missingBlueRoles.Remove(role.Key);
            }
            foreach (var role in filledBucket.RedPlayers)
            {
                missingRedRoles.Remove(role.Key);
            }

            foreach (var fillPlayer in fillPlayers)
            {
                var scramble = AegisRandom.RandomNumber(0, missingBlueRoles.Count() + missingRedRoles.Count());
                if (scramble < missingBlueRoles.Count())
                {
                    var role = missingBlueRoles[scramble];
                    missingBlueRoles.RemoveAt(scramble);
                    filledBucket.BluePlayers.Add(role, fillPlayer.Players[0]);
                    filledBucket.QueueGroups.Add(fillPlayer);
                    filledBucket.PlayerCount += 1;
                } else
                {
                    var role = missingRedRoles[scramble - missingBlueRoles.Count()];
                    missingRedRoles.RemoveAt(scramble - missingBlueRoles.Count());
                    filledBucket.RedPlayers.Add(role, fillPlayer.Players[0]);
                    filledBucket.QueueGroups.Add(fillPlayer);
                    filledBucket.PlayerCount += 1;
                }
            }

            // insert players into game and randomize side
            var inhouseGame = new InhouseGame(channel.Id);

            foreach(var player in filledBucket.BluePlayers)
            {
                player.Value.PlayerSide = PlayerSide.Blue;
                player.Value.PlayerRole = player.Key;
                inhouseGame.InhousePlayers.Add(player.Value);
            }

            foreach (var player in filledBucket.RedPlayers)
            {
                player.Value.PlayerSide = PlayerSide.Red;
                player.Value.PlayerRole = player.Key;
                inhouseGame.InhousePlayers.Add(player.Value);
            }

            InhouseGames.Add(inhouseGame);
            inhouseGame.QueueGroups = filledBucket.QueueGroups;

            // remove everybody in the game from queue
            var inhousePlayers = inhouseGame.InhousePlayers.Select(x => x.Player.Id);
            inhouseQueue.PlayersInQueue.RemoveAll(x => filledBucket.QueueGroups.Any(y => y == x));

            await ReadyCheck(channel, inhouseGame).ConfigureAwait(false);
        }

        private async Task ReadyCheck(DiscordChannel channel, InhouseGame inhouseGame)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            var mentionMsg = "";
            foreach(var player in inhouseGame.InhousePlayers)
            {
                mentionMsg += $"{player.Player.Mention} ";
            }
            var embedBuilder = BuildGameFoundMessage(inhouseGame, inhouseQueue);
            var channelMessage = await channel.SendMessageAsync(mentionMsg, embed: embedBuilder.Build()).ConfigureAwait(false);

            var checkEmoji = DiscordEmoji.FromName(_client, ":white_check_mark:");
            var xEmoji = DiscordEmoji.FromName(_client, ":x:");
            await channelMessage.CreateReactionAsync(checkEmoji).ConfigureAwait(false);
            await channelMessage.CreateReactionAsync(xEmoji).ConfigureAwait(false);

            await Task.Run(async () =>
            {
                while (true)
                {
                    var interactivity = _client.GetInteractivity();
                    var response = await interactivity.WaitForReactionAsync(x => x.Message.Id == channelMessage.Id && x.User.Id != _client.CurrentUser.Id).ConfigureAwait(false);
                    if (response.TimedOut)
                    {
                        // put everyone who was ready back to queue
                        foreach (var queueGroup in inhouseGame.QueueGroups)
                        {
                            foreach (var inhousePlayer in queueGroup.Players)
                            {
                                if (inhousePlayer.PlayerStatus == PlayerStatus.Ready)
                                {
                                    inhousePlayer.PlayerStatus = PlayerStatus.None;
                                } else
                                {
                                    queueGroup.Players.Remove(inhousePlayer);
                                }
                            }
                            inhouseQueue.PlayersInQueue.Insert(0, queueGroup);
                        }
                        await ShowQueue(channel, "Game timed out, readied players will now be put back in queue.").ConfigureAwait(false);

                        InhouseGames.Remove(inhouseGame);
                        return;
                    }
                    if (response.Result.Emoji == checkEmoji)
                    {
                        // check if it is one of the players
                        var player = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == response.Result.User.Id);
                        if (player == null)
                        {
                            continue;
                        }
                        var readyPlayer = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == response.Result.User.Id);
                        readyPlayer.PlayerStatus = PlayerStatus.Ready;
                        var embedBuilder = BuildGameFoundMessage(inhouseGame, inhouseQueue);
                        await channelMessage.ModifyAsync(embed: embedBuilder.Build()).ConfigureAwait(false);

                        if (inhouseGame.InhousePlayers.All(x => x.PlayerStatus == PlayerStatus.Ready))
                        {
                            await channel.SendMessageAsync($"Everybody has readied, game will now start\nUse {_prefix}won to score the game.");
                            return;
                        }
                    }
                    else if (response.Result.Emoji == xEmoji)
                    {
                        // check if it is one of the players
                        var player = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == response.Result.User.Id);
                        if (player == null)
                        {
                            continue;
                        }
                        // put everyone back to queue except the author
                        foreach (var queueGroup in inhouseGame.QueueGroups)
                        {
                            foreach (var inhousePlayer in queueGroup.Players)
                            {
                                if (inhousePlayer.Player.Id == response.Result.User.Id)
                                {
                                    queueGroup.Players.Remove(inhousePlayer);
                                } else
                                {
                                    inhousePlayer.PlayerStatus = PlayerStatus.None;
                                }
                            }
                            inhouseQueue.PlayersInQueue.Insert(0, queueGroup);
                        }

                        InhouseGames.Remove(inhouseGame);
                        await ShowQueue(channel, "A player cancelled the game and was removed from the queue\nAll other players have been put back into the queue").ConfigureAwait(false);
                        return;
                    }
                }
            });
        }

        private DiscordEmbedBuilder BuildGameFoundMessage(InhouseGame inhouseGame, InhouseQueue inhouseQueue)
        {
            var blueWinrate = CalculateBlueSideWinrate(inhouseGame);
            var embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.Title = "📢Game found📢";
            embedBuilder.Description = $"Blue side expected winrate is {blueWinrate.ToString("0.0")}%\n" +
                "If you are ready to play, press ✅\n" +
                "If you cannot play, press ❌\n" +
                "Recheck if it didn't work the first time";
            var bluePlayers = inhouseGame.InhousePlayers.Where(x => x.PlayerSide == PlayerSide.Blue).OrderBy(x => x.PlayerRole);
            var bluePlayersString = "";
            foreach (var bluePlayer in bluePlayers)
            {
                bluePlayersString += $"{inhouseQueue.Emojis[bluePlayer.PlayerRole]}{_statusEmojis[bluePlayer.PlayerStatus]}{bluePlayer.Player.DisplayName}\n";
            }
            var redPlayers = inhouseGame.InhousePlayers.Where(x => x.PlayerSide == PlayerSide.Red).OrderBy(x => x.PlayerRole);
            var redPlayersString = "";
            foreach (var redPlayer in redPlayers)
            {
                redPlayersString += $"{inhouseQueue.Emojis[redPlayer.PlayerRole]}{_statusEmojis[redPlayer.PlayerStatus]}{redPlayer.Player.DisplayName}\n";
            }
            embedBuilder.AddField("BLUE", bluePlayersString, true);
            embedBuilder.AddField("RED", redPlayersString, true);

            return embedBuilder;
        }

        private double CalculateBlueSideWinrate(InhouseGame inhouseGame)
        {
            var overallBlueWinrate = 0.0;
            var overallRedWinrate = 0.0;
            var uow = _db.UnitOfWork();
            foreach (var player in inhouseGame.InhousePlayers)
            {
                var playerStat = uow.InhousePlayerStats.GetByPlayerId(player.Player.Id);
                if (playerStat == null)
                {
                    if (player.PlayerSide == PlayerSide.Blue)
                    {
                        overallBlueWinrate += 50.0;
                    }
                    else
                    {
                        overallRedWinrate += 50.0;
                    }
                    continue;
                }
                var totalGames = playerStat.Wins + playerStat.Loses;
                var wins = playerStat.Wins + 10 - Math.Min(10, totalGames);
                var loses = playerStat.Loses + 10 - Math.Min(10, totalGames);
                var winrate = 100.0 * wins / (wins + loses);
                if (player.PlayerSide == PlayerSide.Blue)
                {
                    overallBlueWinrate += winrate;
                } else
                {
                    overallRedWinrate += winrate;
                }
            }

            if (overallBlueWinrate == 0.0 && overallRedWinrate == 0.0)
            {
                return 50.0;
            }

            return 100 * overallBlueWinrate / (overallBlueWinrate + overallRedWinrate);
        }

        public async Task ConfirmWin(DiscordChannel channel, DiscordMember user)
        {
            var gamesSkipped = 0;
            var gamesSkippedWarning = "";
            var inhouseGame = InhouseGames.FirstOrDefault(x => x.ChannelId == channel.Id && x.InhousePlayers.Any(y => y.Player.Id == user.Id));
            while (inhouseGame != null && inhouseGame.StartTime.AddHours(2) < DateTime.UtcNow)
            {
                ++gamesSkipped;
                InhouseGames.Remove(inhouseGame);
                inhouseGame = InhouseGames.FirstOrDefault(x => x.ChannelId == channel.Id && x.InhousePlayers.Any(y => y.Player.Id == user.Id));
            }
            if (gamesSkipped > 0)
            {
                gamesSkippedWarning = $"*WARNING*: {gamesSkipped} game(s) were skipped since they were not recorded within two hours\n\n";
            }
            if (inhouseGame == null || inhouseGame.CheckingWin)
            {
                return;
            }
            inhouseGame.CheckingWin = true;
            var initPlayer = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == user.Id);
            initPlayer.PlayerConfirm = PlayerConfirm.Accept;
            var playerSide = initPlayer.PlayerSide == PlayerSide.Blue? "Blue": "Red";
            var channelMessage = await channel.SendMessageAsync(gamesSkippedWarning + $"{user.DisplayName} is confirming a win on their side. ({playerSide})\nAt least 6 players must use the checkmark to confirm the win\nRecheck if it didn't work the first time").ConfigureAwait(false);
            var checkEmoji = DiscordEmoji.FromName(_client, ":white_check_mark:");
            var xEmoji = DiscordEmoji.FromName(_client, ":x:");
            await channelMessage.CreateReactionAsync(checkEmoji).ConfigureAwait(false);
            await channelMessage.CreateReactionAsync(xEmoji).ConfigureAwait(false);
            var interactivity = _client.GetInteractivity();
            while (true)
            {
                var response = await interactivity.WaitForReactionAsync(x => x.Message.Id == channelMessage.Id && x.User.Id != _client.CurrentUser.Id).ConfigureAwait(false);
                if (response.TimedOut)
                {
                    inhouseGame.CheckingWin = false;
                    return;
                }
                if (response.Result.Emoji == checkEmoji)
                {
                    // check if it is one of the players
                    var player = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == response.Result.User.Id);
                    if (player == null)
                    {
                        continue;
                    }
                    player.PlayerConfirm = PlayerConfirm.Accept;

                    if (inhouseGame.InhousePlayers.Where(x => x.PlayerConfirm == PlayerConfirm.Accept).Count() >= 6)
                    {
                        await channel.SendMessageAsync($"The game has been recorded as a win for the {playerSide} side").ConfigureAwait(false);
                        var uow = _db.UnitOfWork();
                        uow.MatchHistories.AddByInhouseGame(inhouseGame, initPlayer.PlayerSide);
                        uow.InhousePlayerStats.AddByInhouseGame(inhouseGame, initPlayer.PlayerSide);
                        await uow.SaveAsync().ConfigureAwait(false);
                        InhouseGames.Remove(inhouseGame);
                        return;
                    }
                } else if (response.Result.Emoji == xEmoji)
                {
                    // check if it is one of the players
                    var player = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == response.Result.User.Id);
                    if (player == null)
                    {
                        continue;
                    }
                    player.PlayerConfirm = PlayerConfirm.Deny;

                    if (inhouseGame.InhousePlayers.Where(x => x.PlayerConfirm == PlayerConfirm.Deny).Count() >= 6)
                    {
                        await channel.SendMessageAsync($"The win confirmation has been denied.").ConfigureAwait(false);
                        foreach(var inhousePlayer in inhouseGame.InhousePlayers)
                        {
                            inhousePlayer.PlayerConfirm = PlayerConfirm.None;
                        }
                        inhouseGame.CheckingWin = false;
                        return;
                    }
                }
            }
        }
    }
}
