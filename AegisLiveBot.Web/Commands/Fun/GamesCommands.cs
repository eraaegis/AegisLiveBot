using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Fun;
using AegisLiveBot.DAL;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands.Fun
{
    public class GamesCommands : BaseCommandModule
    {
        private readonly DbService _db;
        private readonly string _prefix;

        public GamesCommands(DbService db, ConfigJson configJson)
        {
            _db = db;
            _prefix = configJson.Prefix;
        }
        [Command("setgamescategory")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetGamesCategory(CommandContext ctx, DiscordChannel cat = null)
        {
            ctx.Message.DeleteAfter(3);
            if (!cat.IsCategory)
            {
                await ctx.Channel.SendMessageAsync($"{cat.Mention} is not a category!").ConfigureAwait(false);
                return;
            }
            using (var uow = _db.UnitOfWork())
            {
                var catId = cat != null ? cat.Id : 0;
                uow.ServerSettings.SetGamesCategory(ctx.Guild.Id, catId);
                await uow.SaveAsync().ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"Games category has been set to {cat.Mention}").ConfigureAwait(false);
            }
        }

        [Command("games")]
        public async Task Games(CommandContext ctx)
        {
            var msg = "The following games are available:\n";
            msg += "'tafl': <http://aagenielsen.dk/tafl_rules.php> \n";
            msg += "'chess': <https://en.wikipedia.org/wiki/Chess> \n";
            msg += "'zoengkei': <https://en.wikipedia.org/wiki/Xiangqi> \n";
            msg += "'reversi': <https://en.wikipedia.org/wiki/Reversi> \n";
            msg += $"Challenge any players with command above like so: {_prefix}tafl @victim";
            await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
        }

        [Command("tafl")]
        public async Task Tafl(CommandContext ctx, DiscordMember otherUser = null)
        {
            var gameType = typeof(TaflService);
            var gameName = "Tafl";
            await StartGame(ctx, otherUser, gameType, gameName).ConfigureAwait(false);
        }

        [Command("chess")]
        public async Task Chess(CommandContext ctx, DiscordMember otherUser = null)
        {
            var gameType = typeof(ChessService);
            var gameName = "Chess";
            await StartGame(ctx, otherUser, gameType, gameName).ConfigureAwait(false);
        }

        [Command("chinesechess")]
        public async Task ChineseChess(CommandContext ctx, DiscordMember otherUser = null)
        {
            var gameType = typeof(ZoengKeiService);
            var gameName = "Chinese Chess";
            await StartGame(ctx, otherUser, gameType, gameName).ConfigureAwait(false);
        }
        [Command("zoengkei")]
        public async Task ZoengKei(CommandContext ctx, DiscordMember otherUser = null)
        {
            await ChineseChess(ctx, otherUser).ConfigureAwait(false);
        }

        [Command("reversi")]
        public async Task Reversi(CommandContext ctx, DiscordMember otherUser = null)
        {
            var gameType = typeof(ReversiService);
            var gameName = "Reversi";
            await StartGame(ctx, otherUser, gameType, gameName).ConfigureAwait(false);
        }
        private async Task StartGame(CommandContext ctx, DiscordMember otherUser, Type gameType, string gameName)
        {
            if(otherUser == null)
            {
                otherUser = ctx.Member;
            }
            else
            {
                var interactivity = ctx.Client.GetInteractivity();
                var msg = $"{ctx.Member.Mention} has challenged {otherUser.Mention} to a {gameName} game!\n";
                msg += $"Type \"accept\" to d-d-duel!";
                await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
                var tries = 3;
                while (true)
                {
                    var response = await interactivity.WaitForMessageAsync(x => x.Author.Id == otherUser.Id && x.ChannelId == ctx.Channel.Id).ConfigureAwait(false);
                    if (response.TimedOut)
                    {
                        await ctx.Channel.SendMessageAsync($"The game was not accepted.").ConfigureAwait(false);
                        return;
                    }
                    var responseMsg = response.Result.Content.ToLower();
                    response.Result.DeleteAfter(3);
                    if (responseMsg == "accept")
                    {
                        break;
                    }
                    else
                    {
                        --tries;
                        if (tries == 0)
                        {
                            await ctx.Channel.SendMessageAsync($"The game was not accepted.").ConfigureAwait(false);
                            return;
                        }
                    }
                }
            }
            try
            {
                using (var uow = _db.UnitOfWork())
                {
                    var serverSetting = uow.ServerSettings.GetOrAddByGuildId(ctx.Guild.Id);
                    var catId = serverSetting.GamesCategory;
                    var cat = ctx.Guild.GetChannel(catId);
                    var tempName = Path.GetRandomFileName();
                    var chName = $"{gameName} Game {tempName}";
                    var chs = await ctx.Guild.GetChannelsAsync().ConfigureAwait(false);
                    while (chs.FirstOrDefault(x => x.Name == chName) != null)
                    {
                        tempName = Path.GetRandomFileName();
                        chName = $"{gameName} Game {tempName}";
                    }
                    var ch = await ctx.Guild.CreateChannelAsync(chName, ChannelType.Text, cat).ConfigureAwait(false);
                    await ch.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages).ConfigureAwait(false);
                    await ch.AddOverwriteAsync(ctx.Member, Permissions.SendMessages, Permissions.None).ConfigureAwait(false);
                    await ch.AddOverwriteAsync(otherUser, Permissions.SendMessages, Permissions.None).ConfigureAwait(false);
                    var gameConstructor = gameType.GetConstructor(new[] { typeof(DiscordChannel), typeof(DiscordMember),
                        typeof(DiscordMember), typeof(DiscordClient), typeof(string)});
                    var game = gameConstructor.Invoke(new object[] { ch, ctx.Member, otherUser, ctx.Client, tempName });
                    ((IGameService)game).Start();
                }
            }
            catch (Exception e)
            {
                await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
            }
        }
    }
}