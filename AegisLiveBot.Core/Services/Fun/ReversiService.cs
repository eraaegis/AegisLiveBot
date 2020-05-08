using AegisLiveBot.Core.Common;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Services.Fun
{
    public class ReversiImage
    {
        private static readonly Lazy<ReversiImage> lazy = new Lazy<ReversiImage>(() => new ReversiImage());
        private readonly string _imagePath;
        public readonly Image _board;
        public readonly Image _white;
        public readonly Image _black;

        private ReversiImage()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images/Reversi");
            _imagePath = path;
            _board = Image.FromFile(Path.Combine(_imagePath, "board.jpg"));
            _white = Image.FromFile(Path.Combine(_imagePath, "white.png"));
            _black = Image.FromFile(Path.Combine(_imagePath, "black.png"));
        }
        public static ReversiImage Instance { get { return lazy.Value; } }
    }
    public class ReversiService : IGameService
    {
        private List<List<Piece>> Board;
        private const int _tileSize = 64;
        private const int _offset = 24;
        private const int _size = 8;

        private DiscordChannel _ch;
        private DiscordMember _blackPlayer;
        private DiscordMember _whitePlayer;
        private DiscordClient _client;
        private string _tempPath;

        private const int _secondsToDelete = 60;
        private Piece CurrentPlayer { get; set; }

        public ReversiService(DiscordChannel ch, DiscordMember p1, DiscordMember p2, DiscordClient client, string tempName)
        {
            _tempPath = Path.Combine(AppContext.BaseDirectory, "Temp/Images/Reversi", tempName);
            Directory.CreateDirectory(_tempPath);
            _ch = ch;

            if (AegisRandom.RandomBool())
            {
                _blackPlayer = p1;
                _whitePlayer = p2;
            }
            else
            {
                _blackPlayer = p2;
                _whitePlayer = p1;
            }
            _client = client;
            Board = InitBoard();
        }
        private List<List<Piece>> InitBoard()
        {
            var board = new List<List<Piece>>();
            for (var i = 0; i < _size; ++i)
            {
                var column = new List<Piece>();
                for (var j = 0; j < _size; ++j)
                {
                    column.Add(Piece.Empty);
                }
                board.Add(column);
            }
            board[3][3] = Piece.White;
            board[4][4] = Piece.White;
            board[3][4] = Piece.Black;
            board[4][3] = Piece.Black;
            return board;
        }
        private string Show()
        {
            var boardImage = new Bitmap(_offset * 2 + _size * _tileSize, _offset * 2 + _size * _tileSize);
            var pieces = Board;
            using (Graphics g = Graphics.FromImage(boardImage))
            {
                g.DrawImage(ReversiImage.Instance._board, new Point(0, 0));
                for (int x = 0; x < _size; ++x)
                {
                    for (int y = 0; y < _size; ++y)
                    {
                        if (pieces[x][y] == Piece.White)
                        {
                            g.DrawImage(ReversiImage.Instance._white, new Point(_offset + x * _tileSize, _offset + (_size - y - 1) * _tileSize));
                        }
                        else if (pieces[x][y] == Piece.Black)
                        {
                            g.DrawImage(ReversiImage.Instance._black, new Point(_offset + x * _tileSize, _offset + (_size - y - 1) * _tileSize));
                        }
                    }
                }
                var filePath = Path.Combine(_tempPath, "temp.jpg");
                boardImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                return filePath;
            }
        }
        private Point TryStringToPoint(string s)
        {
            if(s.Length != 2)
            {
                throw new StringToPointException();
            }
            var x = s.ToLower()[0];
            var y = s[1];
            if(x < 'a' || x > 'h' || y < '1' || y > '8')
            {
                throw new PointOutOfBoundsException();
            }
            var point = new Point(x - 'a', y - '1');
            return point;
        }

        private void TryMove(Point pos)
        {
            // see if there are anything on that pos
            if(Board[pos.X][pos.Y] != Piece.Empty)
            {
                throw new InvalidMoveException();
            }
            // if this move succeeds, then convert already
            TryValidMove(pos, Direction.None, false, true);
            Board[pos.X][pos.Y] = CurrentPlayer;
        }
        // converts if possible, since this wont throw if convert is available
        private bool TryValidMove(Point pos, Direction from, bool canCapture, bool convert)
        {
            // can never be marked if on the edge
            if(pos.X < 0 || pos.X >= _size || pos.Y < 0 || pos.Y >= _size)
            {
                return false;
            }
            // if direction is none, then this is starting point
            if(from == Direction.None)
            {
                // ping all neighbouring eight squares and convers all that can be mark, returns true if at least one converted
                var bool1 = TryValidMove(new Point(pos.X, pos.Y - 1), Direction.N, false, convert);
                var bool2 = TryValidMove(new Point(pos.X - 1, pos.Y - 1), Direction.NE, false, convert);
                var bool3 = TryValidMove(new Point(pos.X - 1, pos.Y), Direction.E, false, convert);
                var bool4 = TryValidMove(new Point(pos.X - 1, pos.Y + 1), Direction.SE, false, convert);
                var bool5 = TryValidMove(new Point(pos.X, pos.Y + 1), Direction.S, false, convert);
                var bool6 = TryValidMove(new Point(pos.X + 1, pos.Y + 1), Direction.SW, false, convert);
                var bool7 = TryValidMove(new Point(pos.X + 1, pos.Y), Direction.W, false, convert);
                var bool8 = TryValidMove(new Point(pos.X + 1, pos.Y - 1), Direction.NW, false, convert);
                if (bool1 || bool2 || bool3 || bool4 || bool5 || bool6 || bool7 || bool8)
                {
                    return true;
                }

                if (convert)
                {
                    throw new InvalidMoveException();
                } else
                {
                    return false;
                }
            }
            // if this piece is empty, return false
            if (Board[pos.X][pos.Y] == Piece.Empty)
            {
                return false;
            }
            // if this piece is the same color as the piece placed, return true if there was a space between
            if(Board[pos.X][pos.Y] == CurrentPlayer)
            {
                if (canCapture)
                {
                    return true;
                } else
                {
                    return false;
                }
            }
            // otherwise, this is an enemy piece, can be converted after pinging the next and getting a true
            Point point;
            switch (from)
            {
                case (Direction.N):
                    point = new Point(pos.X, pos.Y - 1);
                    break;
                case (Direction.NE):
                    point = new Point(pos.X - 1, pos.Y - 1);
                    break;
                case (Direction.E):
                    point = new Point(pos.X - 1, pos.Y);
                    break;
                case (Direction.SE):
                    point = new Point(pos.X - 1, pos.Y + 1);
                    break;
                case (Direction.S):
                    point = new Point(pos.X, pos.Y + 1);
                    break;
                case (Direction.SW):
                    point = new Point(pos.X + 1, pos.Y + 1);
                    break;
                case (Direction.W):
                    point = new Point(pos.X + 1, pos.Y);
                    break;
                case (Direction.NW):
                    point = new Point(pos.X + 1, pos.Y - 1);
                    break;
                default:
                    return false;
            }
            var canConvert = TryValidMove(point, from, true, convert);
            if(canConvert && convert)
            {
                Board[pos.X][pos.Y] = CurrentPlayer;
            }
            return canConvert;
        }
        private bool HasEnded()
        {
            var emptyPieces = new List<Point>();
            for(var x = 0; x < _size; ++x)
            {
                for(var y = 0; y < _size; ++y)
                {
                    if(Board[x][y] == Piece.Empty)
                    {
                        emptyPieces.Add(new Point(x, y));
                    }
                }
            }
            foreach(var emptyPiece in emptyPieces)
            {
                if(TryValidMove(emptyPiece, Direction.None, false, false))
                {
                    return false;
                }
            }
            return true;
        }
        private void CountPieces(out int blackPieces, out int whitePieces)
        {
            blackPieces = 0;
            whitePieces = 0;
            for (var x = 0; x < _size; ++x)
            {
                for (var y = 0; y < _size; ++y)
                {
                    if (Board[x][y] == Piece.Black)
                    {
                        ++blackPieces;
                    } else if (Board[x][y] == Piece.White)
                    {
                        ++whitePieces;
                    }
                }
            }
        }
        public void Start()
        {
            Task.Run(async () =>
            {
                var interactivity = _client.GetInteractivity();

                var board = Show();
                await _ch.SendFileAsync(board, $"Reversi game has started!").ConfigureAwait(false);
                var startMsg = $"{_blackPlayer.DisplayName}(Black) goes first!\n";
                startMsg += $"Use 'help' for commands";
                await _ch.SendMessageAsync(startMsg).ConfigureAwait(false);

                CurrentPlayer = Piece.Black;
                var curPlayer = _blackPlayer;
                var afk = false;
                var hasMoved = false;
                while (true)
                {
                    if (hasMoved)
                    {
                        if(CurrentPlayer == Piece.Black)
                        {
                            CurrentPlayer = Piece.White;
                            curPlayer = _whitePlayer;
                        } else
                        {
                            CurrentPlayer = Piece.Black;
                            curPlayer = _blackPlayer;
                        }
                        hasMoved = false;
                        // check here since currentPlayer changes here
                        if (HasEnded())
                        {
                            CountPieces(out int blackPieces, out int whitePieces);
                            var msg = "Game has ended due to board full or no available moves!\n";
                            if (blackPieces > whitePieces)
                            {
                                msg += $"{_blackPlayer.DisplayName}(Black) has won the game!\n";
                            }
                            else if (whitePieces > blackPieces)
                            {
                                msg += $"{_whitePlayer.DisplayName}(White) has won the game!\n";
                            }
                            else
                            {
                                msg += "The game has drawn!\n";
                            }
                            board = Show();
                            await _ch.SendFileAsync(board).ConfigureAwait(false);
                            await Dispose(msg).ConfigureAwait(false);
                            break;
                        }
                        board = Show();
                        var colorMessage = CurrentPlayer == Piece.White ? "White" : "Black";
                        var showMsg = $"{curPlayer.DisplayName}({colorMessage})'s turn to move.";
                        await _ch.SendFileAsync(board, showMsg).ConfigureAwait(false);
                    }
                    var response = await interactivity.WaitForMessageAsync(x => (x.Author.Id == _whitePlayer.Id || x.Author.Id == _blackPlayer.Id) && x.ChannelId == _ch.Id).ConfigureAwait(false);
                    if (response.TimedOut)
                    {
                        await _ch.SendMessageAsync($"This channel will be deleted soon unless there is activity.").ConfigureAwait(false);
                        if (afk)
                        {
                            var msg = $"The channel has seen no activity for 10 minutes.\n";
                            await Dispose(msg).ConfigureAwait(false);
                            break;
                        }
                        afk = true;
                        continue;
                    }
                    afk = false;
                    var command = response.Result.Content.Split(" ");
                    if(command.Length == 1)
                    {
                        if(command[0] == "help")
                        {
                            var helpMsg = $"Type 'help' for help, or 'resign' to resign game.\n";
                            helpMsg += $"To place a piece, type the grid index. E.g d3";
                            helpMsg += $"For detailed rules, click here: https://en.wikipedia.org/wiki/Reversi#Rules";
                            await _ch.SendMessageAsync(helpMsg).ConfigureAwait(false);
                        } else if(command[0] == "resign")
                        {
                            var resignPlayer = response.Result.Author.Id == _blackPlayer.Id ? _blackPlayer.DisplayName : _whitePlayer.DisplayName;
                            var resignMsg = $"{resignPlayer} has resigned the game!";
                            await Dispose(resignMsg).ConfigureAwait(false);
                            break;
                        } else if(command[0] == "show")
                        {
                            board = Show();
                            var colorMessage = CurrentPlayer == Piece.White ? "White" : "Black";
                            var showMsg = $"{curPlayer.DisplayName}({colorMessage})'s turn to move.";
                            await _ch.SendFileAsync(board).ConfigureAwait(false);
                        } else if(response.Result.Author.Id == curPlayer.Id)
                        {
                            try
                            {
                                var pos = TryStringToPoint(command[0]);
                                TryMove(pos);
                                hasMoved = true;
                            } catch(Exception e)
                            {
                                await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                            }
                        }
                    }
                }
                await Dispose().ConfigureAwait(false);
            });
        }
        private async Task Dispose(string msg = "")
        {
            DirectoryInfo dir = new DirectoryInfo(_tempPath);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            Directory.Delete(_tempPath);
            var timeMsg = _secondsToDelete <= 60 ? $"{_secondsToDelete} seconds" : $"{_secondsToDelete / 60} minutes";
            msg += $"Channel will be deleted in {timeMsg}.\n";
            await _ch.SendMessageAsync(msg).ConfigureAwait(false);
            await Task.Delay(_secondsToDelete * 1000).ConfigureAwait(false);
            await _ch.DeleteAsync().ConfigureAwait(false);
        }

        private enum Piece
        {
            Empty,
            White,
            Black
        }
        private enum Direction
        {
            None,
            N,
            NE,
            E,
            SE,
            S,
            SW,
            W,
            NW
        }
    }
}
