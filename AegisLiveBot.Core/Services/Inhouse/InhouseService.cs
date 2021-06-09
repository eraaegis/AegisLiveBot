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
        Task QueueUp(DiscordChannel channel, DiscordMember user, PlayerRole role);
        Task Unqueue(DiscordChannel channel, DiscordMember user);
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

        public async Task QueueUp(DiscordChannel channel, DiscordMember user, PlayerRole role)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            var inhousePlayer = inhouseQueue.PlayersInQueue.FirstOrDefault(x => x.Player.Id == user.Id);
            if (inhousePlayer == null)
            {
                inhousePlayer = new InhousePlayer(user);
                inhouseQueue.PlayersInQueue.Add(inhousePlayer);
            }

            switch (role)
            {
                case PlayerRole.Top:
                    inhousePlayer.QueuedRoles[PlayerRole.Fill] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Top] = true;
                    break;
                case PlayerRole.Jgl:
                    inhousePlayer.QueuedRoles[PlayerRole.Fill] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Jgl] = true;
                    break;
                case PlayerRole.Mid:
                    inhousePlayer.QueuedRoles[PlayerRole.Fill] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Mid] = true;
                    break;
                case PlayerRole.Bot:
                    inhousePlayer.QueuedRoles[PlayerRole.Fill] = false;
                    inhousePlayer.QueuedRoles[PlayerRole.Bot] = true;
                    break;
                case PlayerRole.Sup:
                    inhousePlayer.QueuedRoles[PlayerRole.Fill] = false;
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

            await ShowQueue(channel).ConfigureAwait(false);

            await AttemptMatchmake(channel).ConfigureAwait(false);
        }

        public async Task Unqueue(DiscordChannel channel, DiscordMember user)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            inhouseQueue.PlayersInQueue.RemoveAll(x => x.Player.Id == user.Id);

            await ShowQueue(channel).ConfigureAwait(false);
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

            var topPlayers = inhouseQueue.PlayersInQueue.Where(x => x.QueuedRoles[PlayerRole.Top]).Select(x => x.Player.DisplayName);
            var jglPlayers = inhouseQueue.PlayersInQueue.Where(x => x.QueuedRoles[PlayerRole.Jgl]).Select(x => x.Player.DisplayName);
            var midPlayers = inhouseQueue.PlayersInQueue.Where(x => x.QueuedRoles[PlayerRole.Mid]).Select(x => x.Player.DisplayName);
            var botPlayers = inhouseQueue.PlayersInQueue.Where(x => x.QueuedRoles[PlayerRole.Bot]).Select(x => x.Player.DisplayName);
            var supPlayers = inhouseQueue.PlayersInQueue.Where(x => x.QueuedRoles[PlayerRole.Sup]).Select(x => x.Player.DisplayName);
            var fillPlayers = inhouseQueue.PlayersInQueue.Where(x => x.QueuedRoles[PlayerRole.Fill]).Select(x => x.Player.DisplayName);

            embedBuilder.Description = $"{inhouseQueue.Emojis[PlayerRole.Top]} {string.Join(", ", topPlayers)}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Jgl]} {string.Join(", ", jglPlayers)}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Mid]} {string.Join(", ", midPlayers)}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Bot]} {string.Join(", ", botPlayers)}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Sup]} {string.Join(", ", supPlayers)}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Fill]} {string.Join(", ", fillPlayers)}\n\n" +
                $"Use {_prefix}queue [role] to join or {_prefix}leave to leave" + emojiWarning;

            await channel.SendMessageAsync(message, embed: embedBuilder.Build()).ConfigureAwait(false);
        }

        private async Task AttemptMatchmake(DiscordChannel channel)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            
            // if less than 10 players in queue, no game can be made
            if (inhouseQueue.PlayersInQueue.Count() < 10)
            {
                return;
            }

            // brute force check for matchmake with buckets
            var buckets = new List<MatchmakeBucket>();
            buckets.Add(new MatchmakeBucket());

            // we reserve the fill players for last
            MatchmakeBucket filledBucket = null;
            var fillPlayers = new List<InhousePlayer>();

            foreach (var inhousePlayer in inhouseQueue.PlayersInQueue)
            {
                var tempBuckets = new List<MatchmakeBucket>();

                var roles = new List<PlayerRole>();
                foreach (var queuedRole in inhousePlayer.QueuedRoles)
                {
                    if (queuedRole.Value && queuedRole.Key != PlayerRole.Fill)
                    {
                        roles.Add(queuedRole.Key);
                    }
                }

                // if no roles, means fill
                if (roles.Count() == 0)
                {
                    fillPlayers.Add(inhousePlayer);
                    if (buckets[0].PlayerCount + fillPlayers.Count() >= 10)
                    {
                        filledBucket = buckets[0];
                    }
                }
                else
                {
                    while (roles.Count() > 0)
                    {
                        var scrambleRole = AegisRandom.RandomNumber(0, roles.Count());
                        var role = roles[scrambleRole];
                        roles.RemoveAt(scrambleRole);
                        foreach (var bucket in buckets)
                        {
                            var tempBucket = new MatchmakeBucket(bucket);
                            if (tempBucket.Players[role].Count() < 2)
                            {
                                tempBucket.Players[role].Add(inhousePlayer);
                                tempBucket.PlayerCount += 1;
                                tempBuckets.Add(tempBucket);
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
                    }
                    buckets = tempBuckets;
                }
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
            var missingRoles = new List<PlayerRole>();
            foreach (var role in filledBucket.Players)
            {
                if (role.Value.Count() == 0)
                {
                    missingRoles.Add(role.Key);
                    missingRoles.Add(role.Key);
                }
                else if (role.Value.Count() == 1)
                {
                    missingRoles.Add(role.Key);
                }
            }

            foreach (var fillPlayer in fillPlayers)
            {
                var scramble = AegisRandom.RandomNumber(0, missingRoles.Count());
                var role = missingRoles[scramble];
                missingRoles.RemoveAt(scramble);
                filledBucket.Players[role].Add(fillPlayer);
                filledBucket.PlayerCount += 1;
            }

            // insert players into game and randomize side
            var inhouseGame = new InhouseGame(channel.Id);

            foreach(var players in filledBucket.Players)
            {
                var scramble = AegisRandom.RandomNumber(0, 2);
                var player = players.Value[scramble];
                player.PlayerSide = PlayerSide.Blue;
                player.PlayerRole = players.Key;
                inhouseGame.InhousePlayers.Add(player);
                player = players.Value[1 - scramble];
                player.PlayerSide = PlayerSide.Red;
                player.PlayerRole = players.Key;
                inhouseGame.InhousePlayers.Add(player);
            }

            InhouseGames.Add(inhouseGame);

            // remove everybody in the game from queue
            var inhousePlayers = inhouseGame.InhousePlayers.Select(x => x.Player.Id);
            inhouseQueue.PlayersInQueue.RemoveAll(x => inhousePlayers.Any(y => x.Player.Id == y));

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
                        foreach (var inhousePlayer in inhouseGame.InhousePlayers)
                        {
                            if (inhousePlayer.PlayerStatus == PlayerStatus.Ready)
                            {
                                inhousePlayer.PlayerStatus = PlayerStatus.None;
                                inhouseQueue.PlayersInQueue.Insert(0, inhousePlayer);
                            }
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
                        foreach (var inhousePlayer in inhouseGame.InhousePlayers)
                        {
                            if (response.Result.User.Id == inhousePlayer.Player.Id)
                            {
                                continue;
                            }
                            inhousePlayer.PlayerStatus = PlayerStatus.None;
                            inhouseQueue.PlayersInQueue.Insert(0, inhousePlayer);
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
            var bluePlayers = inhouseGame.InhousePlayers.Where(x => x.PlayerSide == PlayerSide.Blue);
            var bluePlayersString = "";
            foreach (var bluePlayer in bluePlayers)
            {
                bluePlayersString += $"{inhouseQueue.Emojis[bluePlayer.PlayerRole]}{_statusEmojis[bluePlayer.PlayerStatus]}{bluePlayer.Player.DisplayName}\n";
            }
            var redPlayers = inhouseGame.InhousePlayers.Where(x => x.PlayerSide == PlayerSide.Red);
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
