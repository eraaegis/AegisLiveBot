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
        Task ShowQueue(DiscordChannel channel);
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
            return inhouseQueue;
        }

        public async Task QueueUp(DiscordChannel channel, DiscordMember user, PlayerRole role)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);
            if (inhouseQueue == null)
            {
                return;
            }

            inhouseQueue.Top.Remove(user);
            inhouseQueue.Jgl.Remove(user);
            inhouseQueue.Mid.Remove(user);
            inhouseQueue.Bot.Remove(user);
            inhouseQueue.Sup.Remove(user);

            switch (role)
            {
                case PlayerRole.Top:
                    inhouseQueue.Top.Add(user);
                    break;
                case PlayerRole.Jgl:
                    inhouseQueue.Jgl.Add(user);
                    break;
                case PlayerRole.Mid:
                    inhouseQueue.Mid.Add(user);
                    break;
                case PlayerRole.Bot:
                    inhouseQueue.Bot.Add(user);
                    break;
                case PlayerRole.Sup:
                    inhouseQueue.Sup.Add(user);
                    break;
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

            inhouseQueue.Top.Remove(user);
            inhouseQueue.Jgl.Remove(user);
            inhouseQueue.Mid.Remove(user);
            inhouseQueue.Bot.Remove(user);
            inhouseQueue.Sup.Remove(user);

            await ShowQueue(channel).ConfigureAwait(false);
        }

        public async Task ShowQueue(DiscordChannel channel)
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
                inhouseQueue.Emojis[PlayerRole.Sup] == "SUP:")
            {
                emojiWarning = $"\nAdd the corresponding emojis with name :TOP:, :JGL:, :MID:, :BOT:, :SUP:, and use {_prefix}resetqueuech [channel] to use emojis.";
            }

            embedBuilder.Description = $"{inhouseQueue.Emojis[PlayerRole.Top]} {string.Join(", ", inhouseQueue.Top.Select(x => x.DisplayName))}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Jgl]} {string.Join(", ", inhouseQueue.Jgl.Select(x => x.DisplayName))}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Mid]} {string.Join(", ", inhouseQueue.Mid.Select(x => x.DisplayName))}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Bot]} {string.Join(", ", inhouseQueue.Bot.Select(x => x.DisplayName))}\n" +
                $"{inhouseQueue.Emojis[PlayerRole.Sup]} {string.Join(", ", inhouseQueue.Sup.Select(x => x.DisplayName))}\n\n" +
                $"Use {_prefix}queue [role] to join or {_prefix}leave to leave" + emojiWarning;

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private async Task AttemptMatchmake(DiscordChannel channel)
        {
            var inhouseQueue = InhouseQueues.FirstOrDefault(x => x.ChannelId == channel.Id);

            var inhouseGame = new InhouseGame(channel.Id);
            // in case someone leaves queue while making game, put this in try block and exit
            try
            {
                if (inhouseQueue.Top.Count >= 2 &&
                    inhouseQueue.Jgl.Count >= 2 &&
                    inhouseQueue.Mid.Count >= 2 &&
                    inhouseQueue.Bot.Count >= 2 &&
                    inhouseQueue.Sup.Count >= 2)
                {
                    var scramble = AegisRandom.RandomNumber(0, 2);
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Top[scramble], PlayerSide.Blue, PlayerRole.Top));
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Top[1 - scramble], PlayerSide.Red, PlayerRole.Top));
                    scramble = AegisRandom.RandomNumber(0, 2);
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Jgl[scramble], PlayerSide.Blue, PlayerRole.Jgl));
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Jgl[1 - scramble], PlayerSide.Red, PlayerRole.Jgl));
                    scramble = AegisRandom.RandomNumber(0, 2);
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Mid[scramble], PlayerSide.Blue, PlayerRole.Mid));
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Mid[1 - scramble], PlayerSide.Red, PlayerRole.Mid));
                    scramble = AegisRandom.RandomNumber(0, 2);
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Bot[scramble], PlayerSide.Blue, PlayerRole.Bot));
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Bot[1 - scramble], PlayerSide.Red, PlayerRole.Bot));
                    scramble = AegisRandom.RandomNumber(0, 2);
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Sup[scramble], PlayerSide.Blue, PlayerRole.Sup));
                    inhouseGame.InhousePlayers.Add(new InhousePlayer(inhouseQueue.Sup[1 - scramble], PlayerSide.Red, PlayerRole.Sup));
                    InhouseGames.Add(inhouseGame);
                } else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                AegisLog.Log(ex.Message, ex);
                return;
            }

            // remove everybody in the game from queue
            var inhousePlayers = inhouseGame.InhousePlayers.Select(x => x.Player);
            inhouseQueue.Top.RemoveAll(x => inhousePlayers.Any(y => x == y));
            inhouseQueue.Jgl.RemoveAll(x => inhousePlayers.Any(y => x == y));
            inhouseQueue.Mid.RemoveAll(x => inhousePlayers.Any(y => x == y));
            inhouseQueue.Bot.RemoveAll(x => inhousePlayers.Any(y => x == y));
            inhouseQueue.Sup.RemoveAll(x => inhousePlayers.Any(y => x == y));

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
            await channel.SendMessageAsync(mentionMsg).ConfigureAwait(false);
            var embedBuilder = BuildGameFoundMessage(inhouseGame, inhouseQueue);
            var channelMessage = await channel.SendMessageAsync(embed: embedBuilder.Build());

            var checkEmoji = DiscordEmoji.FromName(_client, ":white_check_mark:");
            var xEmoji = DiscordEmoji.FromName(_client, ":x:");
            await channelMessage.CreateReactionAsync(checkEmoji);
            await channelMessage.CreateReactionAsync(xEmoji);

            await Task.Run(async () =>
            {
                while (true)
                {
                    var interactivity = _client.GetInteractivity();
                    var response = await interactivity.WaitForReactionAsync(x => x.Message.Id == channelMessage.Id && x.User.Id != _client.CurrentUser.Id).ConfigureAwait(false);
                    if (response.TimedOut)
                    {
                        await channel.SendMessageAsync("Game timed out, readied players will now be put back in queue.").ConfigureAwait(false);
                        // put everyone who was ready back to queue
                        foreach (var inhousePlayer in inhouseGame.InhousePlayers)
                        {
                            if (inhousePlayer.PlayerStatus == PlayerStatus.Ready)
                            {
                                switch (inhousePlayer.PlayerRole)
                                {
                                    case PlayerRole.Top:
                                        inhouseQueue.Top.Insert(0, inhousePlayer.Player);
                                        break;
                                    case PlayerRole.Jgl:
                                        inhouseQueue.Jgl.Insert(0, inhousePlayer.Player);
                                        break;
                                    case PlayerRole.Mid:
                                        inhouseQueue.Mid.Insert(0, inhousePlayer.Player);
                                        break;
                                    case PlayerRole.Bot:
                                        inhouseQueue.Bot.Insert(0, inhousePlayer.Player);
                                        break;
                                    case PlayerRole.Sup:
                                        inhouseQueue.Sup.Insert(0, inhousePlayer.Player);
                                        break;
                                }
                            }
                        }

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
                        await channelMessage.ModifyAsync(embed: embedBuilder.Build());

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
                            switch (inhousePlayer.PlayerRole)
                            {
                                case PlayerRole.Top:
                                    inhouseQueue.Top.Insert(0, inhousePlayer.Player);
                                    break;
                                case PlayerRole.Jgl:
                                    inhouseQueue.Jgl.Insert(0, inhousePlayer.Player);
                                    break;
                                case PlayerRole.Mid:
                                    inhouseQueue.Mid.Insert(0, inhousePlayer.Player);
                                    break;
                                case PlayerRole.Bot:
                                    inhouseQueue.Bot.Insert(0, inhousePlayer.Player);
                                    break;
                                case PlayerRole.Sup:
                                    inhouseQueue.Sup.Insert(0, inhousePlayer.Player);
                                    break;
                            }
                        }

                        InhouseGames.Remove(inhouseGame);
                        await channel.SendMessageAsync("A player cancelled the game and was removed from the queue\nAll other players have been put back into the queue").ConfigureAwait(false);
                        return;
                    }
                }
            });
        }

        private DiscordEmbedBuilder BuildGameFoundMessage(InhouseGame inhouseGame, InhouseQueue inhouseQueue)
        {
            var embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.Title = "📢Game found📢";
            embedBuilder.Description = "If you are ready to play, press ✅\n" +
                "If you cannot play, press ❌";
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
            embedBuilder.AddField("", "Recheck if it didn't work the first time");

            return embedBuilder;
        }

        public async Task ConfirmWin(DiscordChannel channel, DiscordMember user)
        {
            var inhouseGame = InhouseGames.FirstOrDefault(x => x.ChannelId == channel.Id && x.InhousePlayers.Any(y => y.Player.Id == user.Id));
            if (inhouseGame == null || inhouseGame.CheckingWin)
            {
                return;
            }
            inhouseGame.CheckingWin = true;
            var initPlayer = inhouseGame.InhousePlayers.FirstOrDefault(x => x.Player.Id == user.Id);
            initPlayer.PlayerConfirm = PlayerConfirm.Accept;
            var playerSide = initPlayer.PlayerSide == PlayerSide.Blue? "Blue": "Red";
            var channelMessage = await channel.SendMessageAsync($"{user.DisplayName} is confirming a win on their side. ({playerSide})\nAt least 6 players must use the checkmark to confirm the win\nRecheck if it didn't work the first time").ConfigureAwait(false);
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
                        await channel.SendMessageAsync($"The game has been recorded as a win for the {playerSide} side");
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
                        await channel.SendMessageAsync($"The win confirmation has been denied.");
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
