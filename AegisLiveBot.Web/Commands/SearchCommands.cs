using AegisLiveBot.Core.Common;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands
{
    public class SearchCommands : BaseCommandModule
    {
        [Command("lmgtfy")]
        public async Task Lmgtfy(CommandContext ctx, params string[] s)
        {
            var search = string.Join("+", s);
            await ctx.Channel.SendMessageAsync($"https://lmgtfy.com/?q={search}").ConfigureAwait(false);
            ctx.Message.DeleteAfter(3);
        }

        [Command("testdraw")]
        public async Task TestDraw(CommandContext ctx)
        {
            try
            {
                var path = "../AegisLiveBot.DAL/Images/Chess";
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
                    for(var i = 0; i < 8; ++i)
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
            } catch(Exception e)
            {
                await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
            }
        }
    }
}
