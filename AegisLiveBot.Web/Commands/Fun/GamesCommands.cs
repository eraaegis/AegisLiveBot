using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Fun;
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

        public GamesCommands(DbService db)
        {
            _db = db;
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

        [Command("tafl")]
        public async Task Tafl(CommandContext ctx, DiscordMember otherUser = null)
        {
            var gameType = typeof(TaflService);
            var gameName = "Tafl";
            await StartGame(ctx, otherUser, gameType, gameName);
        }

        [Command("chess")]
        public async Task Chess(CommandContext ctx, DiscordMember otherUser = null)
        {
            var gameType = typeof(ChessService);
            var gameName = "Chess";
            await StartGame(ctx, otherUser, gameType, gameName);
        }

        [Command("testdraw")]
        public async Task TestDraw(CommandContext ctx)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Images/Chess");
                var board = Image.FromFile(Path.Combine(path, "chessboard_white.jpg"));
                using (Graphics g = Graphics.FromImage(board))
                {
                    var pawnWhite = Image.FromFile(Path.Combine(path, "pawn_white.png"));
                    var rookWhite = Image.FromFile(Path.Combine(path, "rook_white.png"));
                    var knightWhite = Image.FromFile(Path.Combine(path, "knight_white.png"));
                    var bishopWhite = Image.FromFile(Path.Combine(path, "bishop_white.png"));
                    var kingWhite = Image.FromFile(Path.Combine(path, "king_white.png"));
                    var queenWhite = Image.FromFile(Path.Combine(path, "queen_white.png"));
                    var pawnBlack = Image.FromFile(Path.Combine(path, "pawn_black.png"));
                    var rookBlack = Image.FromFile(Path.Combine(path, "rook_black.png"));
                    var knightBlack = Image.FromFile(Path.Combine(path, "knight_black.png"));
                    var bishopBlack = Image.FromFile(Path.Combine(path, "bishop_black.png"));
                    var kingBlack = Image.FromFile(Path.Combine(path, "king_black.png"));
                    var queenBlack = Image.FromFile(Path.Combine(path, "queen_black.png"));
                    for (var i = 0; i < 8; ++i)
                    {
                        g.DrawImage(pawnWhite, new Point(40 + i * 80, 520));
                        g.DrawImage(pawnBlack, new Point(40 + i * 80, 120));
                    }
                    g.DrawImage(rookWhite, new Point(40, 600));
                    g.DrawImage(knightWhite, new Point(120, 600));
                    g.DrawImage(bishopWhite, new Point(200, 600));
                    g.DrawImage(queenWhite, new Point(280, 600));
                    g.DrawImage(kingWhite, new Point(360, 600));
                    g.DrawImage(bishopWhite, new Point(440, 600));
                    g.DrawImage(knightWhite, new Point(520, 600));
                    g.DrawImage(rookWhite, new Point(600, 600));
                    g.DrawImage(rookBlack, new Point(40, 40));
                    g.DrawImage(knightBlack, new Point(120, 40));
                    g.DrawImage(bishopBlack, new Point(200, 40));
                    g.DrawImage(queenBlack, new Point(280, 40));
                    g.DrawImage(kingBlack, new Point(360, 40));
                    g.DrawImage(bishopBlack, new Point(440, 40));
                    g.DrawImage(knightBlack, new Point(520, 40));
                    g.DrawImage(rookBlack, new Point(600, 40));
                }
                board.Save(Path.Combine(path, "test.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                await ctx.Channel.SendFileAsync(Path.Combine(path, "test.jpg")).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
            }
        }
        private async Task StartGame(CommandContext ctx, DiscordMember otherUser, Type gameType, string gameName)
        {
            ctx.Message.DeleteAfter(3);
            if(otherUser == null)
            {
                otherUser = ctx.Member;
            }
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