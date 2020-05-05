using AegisLiveBot.Core.Common;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static AegisLiveBot.Core.Services.Fun.ChessService.ChessBoard;

namespace AegisLiveBot.Core.Services.Fun
{
    public class ChessImage
    {
        private static readonly Lazy<ChessImage> lazy = new Lazy<ChessImage>(() => new ChessImage());
        private readonly string _imagePath;
        public readonly Image _chessBoardWhite;
        public readonly Image _chessBoardBlack;
        public readonly Image _pawnWhite;
        public readonly Image _rookWhite;
        public readonly Image _knightWhite;
        public readonly Image _bishopWhite;
        public readonly Image _queenWhite;
        public readonly Image _kingWhite;
        public readonly Image _pawnBlack;
        public readonly Image _rookBlack;
        public readonly Image _knightBlack;
        public readonly Image _bishopBlack;
        public readonly Image _queenBlack;
        public readonly Image _kingBlack;

        private ChessImage()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images/Chess");
            _imagePath = path;
            _chessBoardWhite = Image.FromFile(Path.Combine(_imagePath, "chessboard_white.jpg"));
            _chessBoardBlack = Image.FromFile(Path.Combine(_imagePath, "chessboard_black.jpg"));
            _pawnWhite = Image.FromFile(Path.Combine(_imagePath, "pawn_white.png"));
            _rookWhite = Image.FromFile(Path.Combine(_imagePath, "rook_white.png"));
            _knightWhite = Image.FromFile(Path.Combine(_imagePath, "knight_white.png"));
            _bishopWhite = Image.FromFile(Path.Combine(_imagePath, "bishop_white.png"));
            _queenWhite = Image.FromFile(Path.Combine(_imagePath, "queen_white.png"));
            _kingWhite = Image.FromFile(Path.Combine(_imagePath, "king_white.png"));
            _pawnBlack = Image.FromFile(Path.Combine(_imagePath, "pawn_black.png"));
            _rookBlack = Image.FromFile(Path.Combine(_imagePath, "rook_black.png"));
            _knightBlack = Image.FromFile(Path.Combine(_imagePath, "knight_black.png"));
            _bishopBlack = Image.FromFile(Path.Combine(_imagePath, "bishop_black.png"));
            _queenBlack = Image.FromFile(Path.Combine(_imagePath, "queen_black.png"));
            _kingBlack = Image.FromFile(Path.Combine(_imagePath, "king_black.png"));
        }
        public static ChessImage Instance { get { return lazy.Value; } }
    }
    public class ChessService : IGameService
    {
        private readonly string _tempPath;
        private readonly ChessBoard Board;
        private readonly DiscordChannel _ch;
        private readonly DiscordClient _client;
        private readonly DiscordMember _whitePlayer;
        private readonly DiscordMember _blackPlayer;
        private const int _borderOffset = 40;
        private const int _tileSize = 80;
        private const int _secondsToDelete = 300;
        private Player CurrentPlayer;
        internal bool HasMoved { get; set; }
        internal bool HasPromote { get; set; }
        internal bool ShowBoard { get; set; }
        public ChessService(DiscordChannel ch, DiscordMember p1, DiscordMember p2, DiscordClient client, string tempName)
        {
            _tempPath = Path.Combine(AppContext.BaseDirectory, "Temp/Images/Chess", tempName);
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
            Board = new ChessBoard(this);
        }
        public void Start()
        {
            Task.Run(async () =>
            {
                var interactivity = _client.GetInteractivity();
                var initMsg = $"A Chess game has started between { _whitePlayer.Mention} and { _blackPlayer.Mention}!\n";
                initMsg += $"Enter 'help' for commands.";
                await _ch.SendMessageAsync(initMsg).ConfigureAwait(false);
                CurrentPlayer = Player.White;
                var curPlayer = _whitePlayer;
                var afk = false;
                ShowBoard = true;
                var flipBoard = false;
                var whitePlayerDraw = false;
                var blackPlayerDraw = false;
                while (true)
                {
                    if (HasMoved && !HasPromote)
                    {
                        if(CurrentPlayer == Player.White)
                        {
                            CurrentPlayer = Player.Black;
                            curPlayer = _blackPlayer;
                        } else
                        {
                            CurrentPlayer = Player.White;
                            curPlayer = _whitePlayer;
                        }
                        HasMoved = false;
                        whitePlayerDraw = false;
                        blackPlayerDraw = false;
                    }
                    if (ShowBoard)
                    {
                        var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                        var colorMessage = CurrentPlayer == Player.White ? "White" : "Black";
                        var showMsg = $"{curPlayer.DisplayName}({colorMessage})'s turn to move.";
                        if (HasPromote)
                        {
                            showMsg = $"Use: promote q/r/n/b to select the promotion.";
                        }
                        await _ch.SendFileAsync(imagePath, showMsg).ConfigureAwait(false);
                        ShowBoard = false;
                    }
                    var response = await interactivity.WaitForMessageAsync(x => (x.Author.Id == _whitePlayer.Id || x.Author.Id == _blackPlayer.Id) && x.ChannelId == _ch.Id).ConfigureAwait(false);
                    if (response.TimedOut)
                    {
                        await _ch.SendMessageAsync($"This channel will be deleted soon unless there is activity.").ConfigureAwait(false);
                        if (afk)
                        {
                            var msg = $"The channel has seen no activity for 10 minutes.\n";
                            var history = Board.WriteHistory(Player.Draw, gameEnded: false);
                            msg += history;
                            await Dispose(msg).ConfigureAwait(false);
                            break;
                        }
                        afk = true;
                        continue;
                    }
                    afk = false;
                    var responseSplit = response.Result.Content.Split(" ");
                    var command = responseSplit[0];
                    if (command.ToLower() == "help")
                    {
                        var helpMsg = $"The following commands are available:\n";
                        helpMsg += $"resign: resign the game.\n";
                        helpMsg += $"move <origin> <destination>: moves the piece designated in origin to destination. E.g move e2 e4\n";
                        helpMsg += $"promote q/r/n/b: when a pawn is at the last rank, select promotion\n";
                        helpMsg += $"flipboard: toggle showing flipped boards for black player\n";
                        helpMsg += $"showboard: shows the current board\n";
                        helpMsg += $"history: shows the history of the game\n";
                        helpMsg += $"draw: draws the game if both players draw\n";
                        helpMsg += $"Supports algebraic notation, for inputs with algebraic notation, visit https://en.wikipedia.org/wiki/Algebraic_notation_(chess)";
                        await _ch.SendMessageAsync(helpMsg).ConfigureAwait(false);
                    } else if (command.ToLower() == "resign")
                    {
                        var msg = $"{((DiscordMember)response.Result.Author).DisplayName} has resigned the game!\n";
                        var history = Board.WriteHistory(response.Result.Author.Id == _blackPlayer.Id ? Player.White : Player.Black, false);
                        msg += history;
                        await Dispose(msg).ConfigureAwait(false);
                        break;
                    } else if (!HasMoved && command.ToLower() == "move" && response.Result.Author.Id == curPlayer.Id)
                    {
                        if (responseSplit.Length > 2)
                        {
                            try
                            {
                                var origin = responseSplit[1];
                                var destination = responseSplit[2];
                                var originPos = ParsePoint(origin);
                                var destinationPos = ParsePoint(destination);
                                Board.TryMove(originPos, destinationPos);
                                // if waiting to promote, dont check for board end
                                if (!HasPromote)
                                {
                                    if (Board.CheckEnd())
                                    {
                                        var msg = "";
                                        if (Board.InCheck)
                                        {
                                            msg = $"{curPlayer.Mention} has won the game!\n";
                                        }
                                        else
                                        {
                                            msg = $"Stalemate!\n";
                                        }
                                        var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                                        await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
                                        var history = Board.WriteHistory(Board.InCheck ? CurrentPlayer : Player.Draw);
                                        msg += history;
                                        await Dispose(msg).ConfigureAwait(false);
                                        break;
                                    }
                                }
                            } catch (Exception e)
                            {
                                await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                            }
                        }
                    } else if (HasPromote && command.ToLower() == "promote" && response.Result.Author.Id == curPlayer.Id)
                    {
                        try
                        {
                            if (responseSplit.Length > 1)
                            {
                                HasPromote = false;
                                var promote = responseSplit[1].ToLower()[0];
                                switch (promote)
                                {
                                    case 'q':
                                        Board.Promote(typeof(Queen));
                                        break;
                                    case 'r':
                                        Board.Promote(typeof(Rook));
                                        break;
                                    case 'k':
                                        Board.Promote(typeof(Knight));
                                        break;
                                    case 'n':
                                        Board.Promote(typeof(Knight));
                                        break;
                                    case 'b':
                                        Board.Promote(typeof(Bishop));
                                        break;
                                    default:
                                        HasPromote = true;
                                        throw new InvalidPromotionException();
                                }
                                if (!HasPromote)
                                {
                                    ShowBoard = true;
                                    if (Board.CheckEnd())
                                    {
                                        var msg = "";
                                        if (Board.InCheck)
                                        {
                                            msg = $"{curPlayer.Mention} has won the game!\n";
                                        }
                                        else
                                        {
                                            msg = $"Stalemate!\n";
                                        }
                                        var history = Board.WriteHistory(Board.InCheck ? CurrentPlayer : Player.Draw);
                                        msg += history;
                                        await Dispose(msg).ConfigureAwait(false);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                throw new InvalidPromotionException();
                            }
                        } catch (Exception e)
                        {
                            await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                        }
                    } else if (command.ToLower() == "flipBoard")
                    {
                        flipBoard = !flipBoard;
                        var msg = flipBoard ? $"Boards will now be shown flipped for Black." : $"Boards will no longer be shown flipped.";
                        await _ch.SendMessageAsync(msg).ConfigureAwait(false);
                    } else if (command.ToLower() == "showboard")
                    {
                        var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                        await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
                    } else if (command.ToLower() == "history")
                    {
                        var history = Board.WriteHistory(Player.Draw, gameEnded: false);
                        await _ch.SendMessageAsync(history).ConfigureAwait(false);
                    } else if (command.ToLower() == "draw") {
                        if (response.Result.Author.Id == _whitePlayer.Id)
                        {
                            whitePlayerDraw = !whitePlayerDraw;
                        }
                        if (response.Result.Author.Id == _blackPlayer.Id)
                        {
                            blackPlayerDraw = !blackPlayerDraw;
                        }
                        if (whitePlayerDraw && blackPlayerDraw)
                        {
                            var msg = $"Both players have agreed to a draw.";
                            var history = Board.WriteHistory(Player.Draw);
                            msg += history;
                            await Dispose(msg).ConfigureAwait(false);
                            break;
                        } else if (whitePlayerDraw || blackPlayerDraw)
                        {
                            await _ch.SendMessageAsync("Draw has been offered.").ConfigureAwait(false);
                        } else
                        {
                            await _ch.SendMessageAsync("Draw offer has been rescinded.").ConfigureAwait(false);
                        }
                    } else if (response.Result.Author.Id == curPlayer.Id)
                    {
                        // try to parse algebraic notation
                        if (responseSplit.Length == 1)
                        {
                            try
                            {
                                // remove last character if its :, x, +, # and indicate appropriately the action
                                var isCapture = false;
                                var isCheck = false;
                                char isPromote = 'z';
                                var lastChar = command.Last();
                                while (true)
                                {
                                    if (lastChar == ':' || lastChar == 'x')
                                    {
                                        isCapture = true;
                                        command = command.Remove(command.Length - 1);
                                        lastChar = command.Last();
                                    }
                                    else if (lastChar == '+' || lastChar == '#')
                                    {
                                        isCheck = true;
                                        command = command.Remove(command.Length - 1);
                                        lastChar = command.Last();
                                    }
                                    else if (lastChar == 'Q' || lastChar == 'q' || lastChar == 'R' || lastChar == 'r' ||
                                  lastChar == 'N' || lastChar == 'n' || lastChar == 'B' || lastChar == 'b')
                                    {
                                        isPromote = char.ToLower(lastChar);
                                        command = command.Remove(command.Length - 1);
                                        lastChar = command.Last();
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                // if castling
                                var castling = command.ToLower().Replace("-", "");
                                if ((castling == "oo" || castling == "00") && Board.PiecesOnBoard[4][0] != null)
                                { // king side castling
                                    if (CurrentPlayer == Player.White && Board.PiecesOnBoard[4][0].GetType() == typeof(King))
                                    {
                                        Board.TryMove(new Point(4, 0), new Point(6, 0));
                                    }
                                    else if (CurrentPlayer == Player.Black && Board.PiecesOnBoard[4][7].GetType() == typeof(King))
                                    {
                                        Board.TryMove(new Point(4, 7), new Point(6, 7));
                                    }
                                }
                                else if ((castling == "ooo" || castling == "000") && Board.PiecesOnBoard[4][0] != null)
                                { // queen side castling
                                    if (CurrentPlayer == Player.White && Board.PiecesOnBoard[4][0].GetType() == typeof(King))
                                    {
                                        Board.TryMove(new Point(4, 0), new Point(2, 0));
                                    }
                                    else if (CurrentPlayer == Player.Black && Board.PiecesOnBoard[4][7].GetType() == typeof(King))
                                    {
                                        Board.TryMove(new Point(4, 7), new Point(2, 7));
                                    }
                                }
                                else
                                {
                                    var commandSplit = command.Split(new char[] { 'x', ':' });
                                    Point dest = new Point(-1, -1);
                                    // if x or : is given, second part is destination
                                    if (commandSplit.Length == 2)
                                    {
                                        dest = ParsePoint(commandSplit[1].ToLower());
                                        command = commandSplit[0];
                                    }
                                    else if (command.Length >= 2)
                                    {
                                        // length larger than 2 indicates destination
                                        var destCommand = command.Substring(command.Length - 2, 2);
                                        command = command.Remove(command.Length - 2);
                                        dest = ParsePoint(destCommand.ToLower());
                                    }
                                    // we have the destination, get the piece
                                    Type movePieceType = typeof(Pawn);
                                    char file = 'z';
                                    // if there is nothing at the start, it is a pawn move
                                    if (command.Length >= 1)
                                    {
                                        var movePieceChar = command.First();
                                        command = command.Remove(0, 1);
                                        switch (movePieceChar)
                                        {
                                            case 'Q':
                                                movePieceType = typeof(Queen);
                                                break;
                                            case 'R':
                                                movePieceType = typeof(Rook);
                                                break;
                                            case 'N':
                                                movePieceType = typeof(Knight);
                                                break;
                                            case 'B':
                                                movePieceType = typeof(Bishop);
                                                break;
                                            case 'K':
                                                movePieceType = typeof(King);
                                                break;
                                            default:
                                                if (movePieceChar >= 'a' && movePieceChar <= 'h')
                                                {
                                                    file = movePieceChar;
                                                }
                                                break;
                                        }
                                    }
                                    // if there is anything left, it is identifying
                                    char rank = 'z';
                                    Point origin = new Point(-1, -1);
                                    // if there is only one character left, check if file or rank
                                    if (command.Length == 1)
                                    {
                                        var commandLower = char.ToLower(command.First());
                                        if (commandLower >= 'a' && commandLower <= 'h')
                                        {
                                            file = commandLower;
                                        }
                                        else if (commandLower >= '1' && commandLower <= '8')
                                        {
                                            rank = commandLower;
                                        }
                                    }
                                    else if (command.Length == 2)
                                    {
                                        origin = ParsePoint(command.Substring(0, 2));
                                    }

                                    // now for the fun, convert the parse into command
                                    // if there is an origin supplied, check if its the same piece type as specified
                                    if (origin != new Point(-1, -1))
                                    {
                                        if (Board.PiecesOnBoard[origin.X][origin.Y].GetType() == movePieceType)
                                        {
                                            Board.TryMove(origin, dest);
                                        }
                                    }
                                    else
                                    {
                                        // get all the pieces specified by piece type that can move to dest
                                        var reacheablePieces = Board.Pieces.Where(x => x.GetType() == movePieceType && x.Player == CurrentPlayer && x.CanReach(dest)).ToList();
                                        // if there is only one, move that one
                                        if (reacheablePieces.Count() == 1)
                                        {
                                            Board.TryMove(reacheablePieces.ElementAt(0).Pos, dest);
                                        }
                                        else if (reacheablePieces.Count() > 1)
                                        {
                                            // get by file if set, or rank if set
                                            if (file != 'z')
                                            {
                                                reacheablePieces = reacheablePieces.Where(x => x.Pos.X == file - 'a').ToList();
                                            }
                                            else if (rank != 'z')
                                            {
                                                reacheablePieces = reacheablePieces.Where(x => x.Pos.Y == rank - '1').ToList();
                                            }
                                            // if still more than 1 reacheable piece, unambiguous move
                                            if (reacheablePieces.Count() == 1)
                                            {
                                                Board.TryMove(reacheablePieces.ElementAt(0).Pos, dest);
                                            }
                                        }
                                    }
                                }
                                // if theres a promotion and specified promotion, promote immediately
                                if (HasPromote && isPromote != 'z')
                                {
                                    HasPromote = false;
                                    switch (isPromote)
                                    {
                                        case 'q':
                                            Board.Promote(typeof(Queen));
                                            break;
                                        case 'r':
                                            Board.Promote(typeof(Rook));
                                            break;
                                        case 'n':
                                            Board.Promote(typeof(Knight));
                                            break;
                                        case 'b':
                                            Board.Promote(typeof(Bishop));
                                            break;
                                        default:
                                            HasPromote = true;
                                            break;
                                    }
                                }
                                // if waiting to promote, dont check for board end
                                if (!HasPromote)
                                {
                                    if (Board.CheckEnd())
                                    {
                                        var msg = "";
                                        if (Board.InCheck)
                                        {
                                            msg = $"{curPlayer.Mention} has won the game!\n";
                                        }
                                        else
                                        {
                                            msg = $"Stalemate!\n";
                                        }
                                        var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                                        await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
                                        var history = Board.WriteHistory(Board.InCheck ? CurrentPlayer : Player.Draw);
                                        msg += history;
                                        await Dispose(msg).ConfigureAwait(false);
                                        break;
                                    }
                                }
                            }
                            catch (ParsePointException e) { }
                            catch (Exception e)
                            {
                                await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                            }
                        }
                    }
                }
            });
        }
        private Point ParsePoint(string s)
        {
            if (s.Length == 2)
            {
                var sX = char.ToLower(s[0]) - 'a';
                var sY = ((int)char.GetNumericValue(s[1])) - 1;
                if (sX >= 0 && sX < 8 &&
                sY >= 0 && sY < 8)
                {
                    return new Point(sX, sY);
                }
            }
            throw new ParsePointException();
        }
        private string Show(Player player)
        {
            var pieces = Board.Pieces;
            var boardImage = new Bitmap(720, 720);
            var imagePath = Path.Combine(_tempPath, "temp.jpg");
            using (Graphics g = Graphics.FromImage(boardImage))
            {
                var chessBoardImage = player == Player.White ? ChessImage.Instance._chessBoardWhite : ChessImage.Instance._chessBoardBlack;
                g.DrawImage(chessBoardImage, new Point(0, 0));
                foreach (var piece in pieces)
                {
                    var pos = piece.Pos;
                    var x = player == Player.White ? pos.X : (7 - pos.X);
                    var y = player == Player.White ? (7 - pos.Y) : pos.Y;
                    g.DrawImage(piece.Image, new Point(_borderOffset + x * _tileSize, _borderOffset + y * _tileSize));
                }
                boardImage.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            return imagePath;
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
            msg += $"You may use this time to save the moves record.";
            await _ch.SendMessageAsync(msg).ConfigureAwait(false);
            await Task.Delay(_secondsToDelete * 1000).ConfigureAwait(false);
            await _ch.DeleteAsync().ConfigureAwait(false);
        }
        internal enum Player
        {
            White,
            Black,
            Draw
        }
        internal class ChessBoard
        {
            internal List<List<Piece>> PiecesOnBoard { get; set; } // this allows for much faster retrieval
            internal List<Piece> Pieces { get; set; }
            internal ChessService Parent { get; set; }
            internal Point EnPassant { get; set; }
            internal Player EnPassantPlayer { get; set; }
            internal bool InCheck { get; set; }
            internal Piece PawnToPromote { get; set; }
            internal List<string> History { get; set; }
            internal ChessBoard()
            {

            }
            internal ChessBoard(ChessService parent)
            {
                Parent = parent;
                Pieces = new List<Piece>
                {
                    new Rook(this, new Point(0, 0), Player.White),
                    new Knight(this, new Point(1, 0), Player.White),
                    new Bishop(this, new Point(2, 0), Player.White),
                    new Queen(this, new Point(3, 0), Player.White),
                    new King(this, new Point(4, 0), Player.White),
                    new Bishop(this, new Point(5, 0), Player.White),
                    new Knight(this, new Point(6, 0), Player.White),
                    new Rook(this, new Point(7, 0), Player.White),
                    new Pawn(this, new Point(0, 1), Player.White),
                    new Pawn(this, new Point(1, 1), Player.White),
                    new Pawn(this, new Point(2, 1), Player.White),
                    new Pawn(this, new Point(3, 1), Player.White),
                    new Pawn(this, new Point(4, 1), Player.White),
                    new Pawn(this, new Point(5, 1), Player.White),
                    new Pawn(this, new Point(6, 1), Player.White),
                    new Pawn(this, new Point(7, 1), Player.White),
                    new Pawn(this, new Point(0, 6), Player.Black),
                    new Pawn(this, new Point(1, 6), Player.Black),
                    new Pawn(this, new Point(2, 6), Player.Black),
                    new Pawn(this, new Point(3, 6), Player.Black),
                    new Pawn(this, new Point(4, 6), Player.Black),
                    new Pawn(this, new Point(5, 6), Player.Black),
                    new Pawn(this, new Point(6, 6), Player.Black),
                    new Pawn(this, new Point(7, 6), Player.Black),
                    new Rook(this, new Point(0, 7), Player.Black),
                    new Knight(this, new Point(1, 7), Player.Black),
                    new Bishop(this, new Point(2, 7), Player.Black),
                    new Queen(this, new Point(3, 7), Player.Black),
                    new King(this, new Point(4, 7), Player.Black),
                    new Bishop(this, new Point(5, 7), Player.Black),
                    new Knight(this, new Point(6, 7), Player.Black),
                    new Rook(this, new Point(7, 7), Player.Black)
                };
                PiecesOnBoard = new List<List<Piece>>();
                for (var i = 0; i < 8; ++i)
                {
                    var column = new List<Piece>();
                    for (var j = 0; j < 8; ++j)
                    {
                        column.Add(null);
                    }
                    PiecesOnBoard.Add(column);
                }
                foreach (var piece in Pieces)
                {
                    PiecesOnBoard[piece.Pos.X][piece.Pos.Y] = piece;
                }
                History = new List<string>();
            }
            internal void TryMove(Point origin, Point dest)
            {
                var piece = PiecesOnBoard[origin.X][origin.Y];
                if(piece != null && piece.Player == Parent.CurrentPlayer)
                {
                    try
                    {
                        if (!piece.CanReach(dest))
                        {
                            throw new InvalidMoveException();
                        }
                        // check if this move puts you in check
                        var tempPiece = PiecesOnBoard[dest.X][dest.Y];
                        var isEnPassant = dest == EnPassant && piece.GetType() == typeof(Pawn);
                        var enPassantY = Parent.CurrentPlayer == Player.White ? dest.Y - 1 : dest.Y + 1;
                        if (isEnPassant)
                        {
                            tempPiece = PiecesOnBoard[dest.X][enPassantY];
                        }
                        try
                        {
                            PiecesOnBoard[origin.X][origin.Y] = null;
                            PiecesOnBoard[dest.X][dest.Y] = piece;
                            if (isEnPassant)
                            {
                                PiecesOnBoard[dest.X][enPassantY] = null;
                            }
                            if (piece.GetType() == typeof(King))
                            {
                                piece.Pos = dest;
                            }
                            CheckValidMove(false, tempPiece);
                        } catch(Exception e)
                        {
                            throw e;
                        } finally
                        {
                            PiecesOnBoard[origin.X][origin.Y] = piece;
                            if (isEnPassant)
                            {
                                PiecesOnBoard[dest.X][dest.Y] = null;
                                PiecesOnBoard[dest.X][enPassantY] = tempPiece;
                            } else
                            {
                                PiecesOnBoard[dest.X][dest.Y] = tempPiece;
                            }
                            if (piece.GetType() == typeof(King))
                            {
                                piece.Pos = origin;
                            }
                        }
                        piece.TryMove(dest);
                        var destPiece = PiecesOnBoard[dest.X][dest.Y];
                        if (destPiece != null)
                        {
                            Pieces.Remove(destPiece);
                        }
                        // enpassant move
                        if (isEnPassant)
                        {
                            var enPassantPiece = PiecesOnBoard[dest.X][enPassantY];
                            Pieces.Remove(enPassantPiece);
                            PiecesOnBoard[dest.X][enPassantY] = null;
                        }
                        // castling move
                        if(piece.GetType() == typeof(King))
                        {
                            if(Math.Abs(origin.X - dest.X) == 2)
                            {
                                // king side castle, move rook
                                if(dest.X > origin.X)
                                {
                                    var rook = PiecesOnBoard[origin.X + 3][origin.Y];
                                    PiecesOnBoard[origin.X + 3][origin.Y] = null;
                                    PiecesOnBoard[origin.X + 1][origin.Y] = rook;
                                } else
                                {
                                    var rook = PiecesOnBoard[origin.X - 4][origin.Y];
                                    PiecesOnBoard[origin.X - 4][origin.Y] = null;
                                    PiecesOnBoard[origin.X - 1][origin.Y] = rook;
                                }
                            }
                        }
                        PiecesOnBoard[origin.X][origin.Y] = null;
                        PiecesOnBoard[piece.Pos.X][piece.Pos.Y] = piece;
                        // write to history
                        var moveString = "";
                        // castle
                        if (piece.GetType() == typeof(King) && Math.Abs(origin.X - dest.X) == 2) 
                        {
                            if(dest.X > origin.X)
                            {
                                moveString = "O-O";
                            } else
                            {
                                moveString = "O-O-O";
                            }
                        } else
                        {
                            var pieceType = piece.GetType();
                            char pieceIdentifier = '\0';
                            if(pieceType == typeof(King))
                            {
                                pieceIdentifier = 'K';
                            } else if (pieceType == typeof(Queen))
                            {
                                pieceIdentifier = 'Q';
                            } else if (pieceType == typeof(Bishop))
                            {
                                pieceIdentifier = 'B';
                            } else if (pieceType == typeof(Knight))
                            {
                                pieceIdentifier = 'N';
                            } else if (pieceType == typeof(Rook))
                            {
                                pieceIdentifier = 'R';
                            } else
                            {
                                pieceIdentifier = (char)('a' + origin.X);
                            }
                            var pieceFile = (char)('0' + origin.X);
                            var pieceRank = (char)('a' + origin.Y);
                            var destString = ((char)('a' + dest.X)).ToString() + ((char)('1' + dest.Y)).ToString();
                            var originString = "";
                            var myPieces = Pieces.Where(x => x.Player == Parent.CurrentPlayer &&
                            x.GetType() != typeof(Pawn) && x.GetType() == pieceType && x.CanReach(dest));
                            // pawns dont need origin identifiers
                            if (myPieces.Count() >= 1) // if one or more of my pieces of same type can also reach that location
                            {
                                // check if can be separated by file
                                if(myPieces.Where(x => x.Pos.X == origin.X).Count() == 0)
                                {
                                    originString += pieceFile;
                                } else if(myPieces.Where(x => x.Pos.Y == origin.Y).Count() == 0)
                                {
                                    originString += pieceRank;
                                } else
                                {
                                    originString += pieceFile += pieceRank;
                                }
                            } else
                            {
                                // if its not a pawn or is taking, put piece identifier
                                if (pieceType != typeof(Pawn) || destPiece != null || dest == EnPassant)
                                {
                                    moveString += pieceIdentifier;
                                }
                                moveString += originString;
                                if(destPiece != null || dest == EnPassant)
                                {
                                    moveString += 'x';
                                }
                                moveString += destString;
                            }
                        }
                        History.Add(moveString);
                    } catch(Exception e)
                    {
                        throw e;
                    }
                } else
                {
                    throw new InvalidMoveException();
                }
                if(EnPassantPlayer != Parent.CurrentPlayer)
                {
                    EnPassant = new Point(-1, -1);
                }
                if(PawnToPromote != null)
                {
                    Parent.HasPromote = true;
                }
                Parent.HasMoved = true;
                Parent.ShowBoard = true;
            }
            // checks if current move will put you in check
            internal void CheckValidMove(bool currentPlayer = false, Piece ignorePiece = null)
            {
                var pieces = currentPlayer ? Pieces.Where(x => x.Player == Parent.CurrentPlayer && x != ignorePiece) : Pieces.Where(x => x.Player != Parent.CurrentPlayer && x != ignorePiece);
                var kingPiece = currentPlayer ? Pieces.FirstOrDefault(x => x.Player != Parent.CurrentPlayer && x.GetType() == typeof(King))
                    : Pieces.FirstOrDefault(x => x.Player == Parent.CurrentPlayer && x.GetType() == typeof(King));
                foreach (var piece in pieces)
                {
                    if (piece.CanReach(kingPiece.Pos, true))
                    {
                        throw new CheckMoveException();
                    }
                }
            }
            internal bool CheckEnd()
            {
                var myPieces = Pieces.Where(x => x.Player == Parent.CurrentPlayer);
                var enemyPieces = Pieces.Where(x => x.Player != Parent.CurrentPlayer);
                var kingPiece = (King)enemyPieces.FirstOrDefault(x => x.GetType() == typeof(King));
                var checkingPieces = new List<Piece>();
                foreach (var myPiece in myPieces)
                {
                    if (myPiece.CanReach(kingPiece.Pos, true))
                    {
                        checkingPieces.Add(myPiece);
                    }
                }
                if(checkingPieces.Count == 0)
                {
                    InCheck = false;
                    // if not in check, check if there are any valid moves
                    foreach(var enemyPiece in enemyPieces)
                    {
                        var enemyReacheableSquares = enemyPiece.GetReacheableSquares();
                        Piece temp = null;
                        Point tempPoint = enemyPiece.Pos;
                        foreach(var enemyReacheableSquare in enemyReacheableSquares)
                        {
                            // check en passant
                            var enPassantY = Parent.CurrentPlayer == Player.White ? enemyReacheableSquare.Y + 1 : enemyReacheableSquare.Y - 1;
                            var isEnPassant = enemyPiece.GetType() == typeof(Pawn) && enemyReacheableSquare == EnPassant;
                            try
                            {
                                if (isEnPassant)
                                {
                                    temp = PiecesOnBoard[enemyReacheableSquare.X][enPassantY];
                                    PiecesOnBoard[enemyReacheableSquare.X][enPassantY] = null;
                                } else
                                {
                                    temp = PiecesOnBoard[enemyReacheableSquare.X][enemyReacheableSquare.Y];
                                }
                                PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = null;
                                PiecesOnBoard[enemyReacheableSquare.X][enemyReacheableSquare.Y] = enemyPiece;
                                if(enemyPiece.GetType() == typeof(King))
                                {
                                    enemyPiece.Pos = enemyReacheableSquare;
                                }
                                CheckValidMove(true, temp);
                                return false;
                            }
                            catch (CheckMoveException)
                            {

                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                            finally
                            {
                                if(enemyPiece.GetType() == typeof(King))
                                {
                                    enemyPiece.Pos = tempPoint;
                                }
                                PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = enemyPiece;
                                if (isEnPassant)
                                {
                                    PiecesOnBoard[enemyReacheableSquare.X][enPassantY] = temp;
                                } else
                                {
                                    PiecesOnBoard[enemyReacheableSquare.X][enemyReacheableSquare.Y] = temp;
                                }
                            }
                        }
                    }
                } else
                {
                    InCheck = true;
                    // add check mark if check
                    var moveString = History.Last() + '+';
                    History.RemoveAt(History.Count() - 1);
                    History.Add(moveString);
                    var kingReacheableSquares = kingPiece.GetReacheableSquares();
                    // check that there is a square that the king can move to without being checked
                    foreach (var kingReacheableSquare in kingReacheableSquares)
                    {
                        // if this is a king take, temporarily swap out that piece
                        var tempPiece = PiecesOnBoard[kingReacheableSquare.X][kingReacheableSquare.Y];
                        PiecesOnBoard[kingReacheableSquare.X][kingReacheableSquare.Y] = kingPiece;
                        // if this is a square that escapes check, check if any other pieces can reach this square
                        var canReach = myPieces.FirstOrDefault(x => x.CanReach(kingReacheableSquare, true));
                        PiecesOnBoard[kingReacheableSquare.X][kingReacheableSquare.Y] = tempPiece;
                        if (canReach == null)
                        {
                            return false;
                        }
                    }
                    // if this is a double check, only king can move, therefore no reacheable square = checkmate
                    if (checkingPieces.Count >= 2)
                    {
                        return true;
                    } else
                    {
                        var checkingPiece = checkingPieces[0];
                        // check if the checking piece can be captured without getting discover checked
                        // we have already checked king takes checking piece, check everything else
                        foreach (var enemyPiece in enemyPieces)
                        {
                            if(enemyPiece.GetType() == typeof(King))
                            {
                                continue;
                            }
                            // check en passant
                            var enPassantY = Parent.CurrentPlayer == Player.White ? checkingPiece.Pos.Y - 1 : checkingPiece.Pos.Y + 1;
                            if(enemyPiece.GetType() == typeof(Pawn) && EnPassant == new Point(checkingPiece.Pos.X, enPassantY))
                            {
                                if (enemyPiece.CanReach(new Point(checkingPiece.Pos.X, enPassantY), true))
                                {
                                    try
                                    {
                                        PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = null;
                                        PiecesOnBoard[checkingPiece.Pos.X][enPassantY] = enemyPiece;
                                        CheckValidMove(true, checkingPiece);
                                        return false;
                                    }
                                    catch (CheckMoveException)
                                    {

                                    }
                                    catch (Exception e)
                                    {
                                        throw e;
                                    }
                                    finally
                                    {
                                        PiecesOnBoard[checkingPiece.Pos.X][enPassantY] = null;
                                        PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = enemyPiece;
                                    }
                                }
                            }
                            if (enemyPiece.CanReach(checkingPiece.Pos, true))
                            {
                                try
                                {
                                    PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = null;
                                    CheckValidMove(true, checkingPiece);
                                    return false;
                                }
                                catch (CheckMoveException)
                                {

                                }
                                catch (Exception e)
                                {
                                    throw e;
                                }
                                finally
                                {
                                    PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = enemyPiece;
                                }
                            }
                        }
                        // if checking piece is knight or pawn, it cannot be blocked, therefore cant capture = checkmate
                        if (checkingPiece.GetType() == typeof(Pawn) || checkingPiece.GetType() == typeof(Knight))
                        {
                            return true;
                        }
                        else
                        // otherwise check if can be blocked
                        {
                            var blockeableSquares = checkingPiece.GetPathToPos(kingPiece.Pos);
                            foreach(var blockeableSquare in blockeableSquares)
                            {
                                foreach(var enemyPiece in enemyPieces)
                                {
                                    if(enemyPiece.CanReach(blockeableSquare, true))
                                    {
                                        try
                                        {
                                            PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = null;
                                            CheckValidMove(true);
                                            return false;
                                        } catch (CheckMoveException)
                                        {

                                        } catch (Exception e)
                                        {
                                            throw e;
                                        }
                                        finally
                                        {
                                            PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = enemyPiece;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // if cannot be blocked, return true
                return true;
            }
            internal void Promote(Type pieceType)
            {
                var pawnToPromote = PawnToPromote;
                PawnToPromote = null;
                var pieceConstructor = pieceType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(ChessBoard), typeof(Point),
                        typeof(Player) }, null);
                var newPiece = (Piece)pieceConstructor.Invoke(new object[] { this, pawnToPromote.Pos, pawnToPromote.Player });
                Pieces.Remove(pawnToPromote);
                Pieces.Add(newPiece);
                PiecesOnBoard[pawnToPromote.Pos.X][pawnToPromote.Pos.Y] = newPiece;

                char pieceIdentifier = '\0';
                if (pieceType == typeof(Queen))
                {
                    pieceIdentifier = 'Q';
                }
                else if (pieceType == typeof(Bishop))
                {
                    pieceIdentifier = 'B';
                }
                else if (pieceType == typeof(Knight))
                {
                    pieceIdentifier = 'N';
                }
                else if (pieceType == typeof(Rook))
                {
                    pieceIdentifier = 'R';
                }
                var moveString = History.Last() + pieceIdentifier;
                History.RemoveAt(History.Count() - 1);
                History.Add(moveString);
            }
            internal string WriteHistory(Player gameEndCondition, bool checkMate = true, bool gameEnded = true)
            {
                if (gameEnded)
                {
                    if (gameEndCondition == Player.Draw)
                    {
                        History.Add("½-½");
                    }
                    else
                    {
                        if (checkMate)
                        {
                            var moveString = History.Last();
                            moveString = moveString.Remove(moveString.Length - 1) + '#';
                            History.RemoveAt(History.Count() - 1);
                            History.Add(moveString);
                        }
                        History.Add(gameEndCondition == Player.White ? "1-0" : "0-1");
                    }
                }
                var historyString = "```";
                var padRight = History.Count() >= 200 ? 5 : 4;
                for(var i = 0; i < History.Count(); i += 2)
                {
                    var index = (((i + 1) / 2 + 1).ToString() + '.').PadRight(padRight);
                    var firstMove = History[i].PadRight(5);
                    var secondMove = History.Count() != i + 1 ? History[i + 1] : "";
                    historyString += $"{index}{firstMove} {secondMove}\n";
                }
                historyString += "```";
                return historyString;
            }
            internal abstract class Piece
            {
                internal ChessBoard Parent { get; set; }
                internal Point Pos { get; set; }
                internal Player Player { get; set; }
                internal Image Image { get; set; }
                internal bool HasMoved { get; set; } // for castling and pawns
                internal Piece(ChessBoard chessBoard, Point pos, Player player)
                {
                    Parent = chessBoard;
                    Pos = pos;
                    Player = player;
                }
                internal virtual void TryMove(Point dest)
                {
                    if (CanReach(dest))
                    {
                        Pos = new Point(dest.X, dest.Y);
                        HasMoved = true;
                        return;
                    }
                    throw new InvalidMoveException();
                }
                internal abstract bool CanReach(Point dest, bool doNotCheckCastleOrPawnMove = false);
                internal virtual List<Point> GetPathToPos(Point pos) { return null; }
                internal abstract List<Point> GetReacheableSquares();
            }
            internal class Pawn : Piece
            {
                internal Pawn(ChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ChessImage.Instance._pawnWhite : ChessImage.Instance._pawnBlack;
                }
                internal override void TryMove(Point dest)
                {
                    if (CanReach(dest))
                    {
                        if (Math.Abs(Pos.Y - dest.Y) == 2)
                        {
                            Parent.EnPassant = new Point(Pos.X, (Pos.Y + dest.Y) / 2);
                            Parent.EnPassantPlayer = Player;
                        }
                        Pos = new Point(dest.X, dest.Y);
                        HasMoved = true;
                        if((Player == Player.White && Pos.Y == 7) ||
                            (Player == Player.Black && Pos.Y == 0))
                        {
                            Parent.PawnToPromote = this;
                        }
                        return;
                    }
                    throw new InvalidMoveException();
                }
                internal override bool CanReach(Point dest, bool doNotCheckCastleOrPawnMove = false)
                {
                    var destPiece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (Parent.EnPassant == dest && Parent.EnPassantPlayer != Player)
                    {
                        var enPassantY = Player == Player.White ? dest.Y - 1 : dest.Y + 1;
                        destPiece = Parent.PiecesOnBoard[dest.X][enPassantY];
                    }
                    // for moving two squares, check forward square
                    var blockingPiece = Player == Player.White ? Parent.PiecesOnBoard[Pos.X][Pos.Y + 1] : Parent.PiecesOnBoard[Pos.X][Pos.Y - 1];
                    // captures diagonally
                    if ((destPiece != null &&
                        Player != destPiece.Player &&
                        ((Player == Player.White && Math.Abs(dest.X - Pos.X) == 1 && dest.Y == Pos.Y + 1) ||
                        (Player == Player.Black && Math.Abs(dest.X - Pos.X) == 1 && dest.Y == Pos.Y - 1))) ||
                        // moves vertically, moves 2 if not moved
                        (!doNotCheckCastleOrPawnMove &&
                        destPiece == null &&
                        ((Player == Player.White && dest.X == Pos.X && ((!HasMoved && dest.Y == Pos.Y + 2 && blockingPiece == null) || dest.Y == Pos.Y + 1)) ||
                        (Player == Player.Black && dest.X == Pos.X && ((!HasMoved && dest.Y == Pos.Y - 2 && blockingPiece == null) || dest.Y == Pos.Y - 1)))))
                    {
                        return true;
                    }
                    return false;
                }
                internal override List<Point> GetReacheableSquares()
                {
                    var reacheableSquares = new List<Point>
                    {
                        new Point(Pos.X, Pos.Y + 1),
                        new Point(Pos.X, Pos.Y + 2),
                        new Point(Pos.X, Pos.Y - 1),
                        new Point(Pos.X, Pos.Y - 2),
                        new Point(Pos.X + 1, Pos.Y + 1),
                        new Point(Pos.X - 1, Pos.Y + 1),
                        new Point(Pos.X + 1, Pos.Y - 1),
                        new Point(Pos.X - 1, Pos.Y - 1)
                    };
                    reacheableSquares.RemoveAll(x => x.X < 0 || x.X >= 8 || x.Y < 0 || x.Y >= 8 || !CanReach(x));
                    return reacheableSquares;
                }
            }
            internal class Rook : Piece
            {
                internal Rook(ChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ChessImage.Instance._rookWhite : ChessImage.Instance._rookBlack;
                }
                internal override bool CanReach(Point dest, bool doNotCheckCastleOrPawnMove = false)
                {
                    if (Pos != dest)
                    {
                        // move horizontally
                        if (Pos.Y == dest.Y)
                        {
                            var i = Pos.X;
                            while (i != dest.X)
                            {
                                _ = dest.X > i ? ++i : --i;
                                if (i == dest.X)
                                {
                                    break;
                                }
                                if (Parent.PiecesOnBoard[i][Pos.Y] != null)
                                {
                                    return false;
                                }
                            }
                        }
                        else if (Pos.X == dest.X) // move vertically
                        {
                            var i = Pos.Y;
                            while (i != dest.Y)
                            {
                                _ = dest.Y > i ? ++i : --i;
                                if (i == dest.Y)
                                {
                                    break;
                                }
                                if (Parent.PiecesOnBoard[Pos.X][i] != null)
                                {
                                    return false;
                                }
                            }
                        } else
                        {
                            return false;
                        }
                        var destPiece = Parent.PiecesOnBoard[dest.X][dest.Y];
                        if ((destPiece != null && Player != destPiece.Player) || destPiece == null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var reacheableSquares = new List<Point>();
                    if(Pos.Y == pos.Y)
                    {
                        var i = Pos.X;
                        while (i != pos.X)
                        {
                            _ = pos.X > i ? ++i : --i;
                            if (i == pos.X || Parent.PiecesOnBoard[i][Pos.Y] != null)
                            {
                                break;
                            } else
                            {
                                reacheableSquares.Add(new Point(i, Pos.Y));
                            }
                        }
                    } else if(Pos.X == pos.X)
                    {
                        var i = Pos.Y;
                        while (i != pos.Y)
                        {
                            _ = pos.Y > i ? ++i : --i;
                            if (i == pos.Y || Parent.PiecesOnBoard[Pos.X][i] != null)
                            {
                                break;
                            } else
                            {
                                reacheableSquares.Add(new Point(Pos.X, i));
                            }
                        }
                    }
                    return reacheableSquares;
                }
                internal override List<Point> GetReacheableSquares()
                {
                    var reacheableSquares = new List<Point>();
                    for(var i = 0; i < 8; ++i)
                    {
                        reacheableSquares.Add(new Point(i, Pos.Y));
                        reacheableSquares.Add(new Point(Pos.X, i));
                    }
                    reacheableSquares.RemoveAll(x => !CanReach(x));
                    return reacheableSquares;
                }
            }
            internal class Knight : Piece
            {
                internal Knight(ChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ChessImage.Instance._knightWhite : ChessImage.Instance._knightBlack;
                }

                internal override bool CanReach(Point dest, bool doNotCheckCastleOrPawnMove = false)
                {
                    if ((Math.Abs(Pos.X - dest.X) == 2 && Math.Abs(Pos.Y - dest.Y) == 1) ||
                        (Math.Abs(Pos.X - dest.X) == 1 && Math.Abs(Pos.Y - dest.Y) == 2))
                    {
                        var destPiece = Parent.PiecesOnBoard[dest.X][dest.Y];
                        if ((destPiece != null && Player != destPiece.Player) || destPiece == null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                internal override List<Point> GetReacheableSquares()
                {
                    var reacheableSquares = new List<Point>
                    {
                        new Point(Pos.X + 2, Pos.Y + 1),
                        new Point(Pos.X + 2, Pos.Y - 1),
                        new Point(Pos.X + 1, Pos.Y + 2),
                        new Point(Pos.X + 1, Pos.Y - 2),
                        new Point(Pos.X - 1, Pos.Y + 2),
                        new Point(Pos.X - 1, Pos.Y - 2),
                        new Point(Pos.X + 2, Pos.Y + 1),
                        new Point(Pos.X + 2, Pos.Y - 1)
                    };
                    reacheableSquares.RemoveAll(x => x.X < 0 || x.X >= 8 || x.Y < 0 || x.Y >= 8 || !CanReach(x));
                    return reacheableSquares;
                }
            }
            internal class Bishop : Piece
            {
                internal Bishop(ChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ChessImage.Instance._bishopWhite : ChessImage.Instance._bishopBlack;
                }

                internal override bool CanReach(Point dest, bool doNotCheckCastleOrPawnMove = false)
                {
                    if (Pos != dest && Math.Abs(Pos.X - dest.X) == Math.Abs(Pos.Y - dest.Y))
                    {
                        var x = Pos.X;
                        var y = Pos.Y;
                        while (x != dest.X && y != dest.Y)
                        {
                            _ = dest.X > x ? ++x : --x;
                            _ = dest.Y > y ? ++y : --y;
                            if (x == dest.X)
                            {
                                break;
                            }
                            if (Parent.PiecesOnBoard[x][y] != null)
                            {
                                return false;
                            }
                        }
                        var destPiece = Parent.PiecesOnBoard[dest.X][dest.Y];
                        if ((destPiece != null && Player != destPiece.Player) || destPiece == null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var reacheableSquares = new List<Point>();
                    if (Pos != pos && Math.Abs(Pos.X - pos.X) == Math.Abs(Pos.Y - pos.Y))
                    {
                        var x = Pos.X;
                        var y = Pos.Y;
                        while (x != pos.X && y != pos.Y)
                        {
                            _ = pos.X > x ? ++x : --x;
                            _ = pos.Y > y ? ++y : --y;
                            if (x == pos.X || Parent.PiecesOnBoard[x][y] != null)
                            {
                                break;
                            }
                            else
                            {
                                reacheableSquares.Add(new Point(x, y));
                            }
                        }
                    }
                    return reacheableSquares;
                }
                internal override List<Point> GetReacheableSquares()
                {
                    var reacheableSquares = new List<Point>();
                    for (var i = 0; i < 7; ++i)
                    {
                        reacheableSquares.Add(new Point(Pos.X + i + 1, Pos.Y + i + 1));
                        reacheableSquares.Add(new Point(Pos.X - i - 1, Pos.Y + i + 1));
                        reacheableSquares.Add(new Point(Pos.X + i + 1, Pos.Y - i - 1));
                        reacheableSquares.Add(new Point(Pos.X - i - 1, Pos.Y - i - 1));
                    }
                    reacheableSquares.RemoveAll(x => x.X < 0 || x.X >= 8 || x.Y < 0 || x.Y >= 8 || !CanReach(x));
                    return reacheableSquares;
                }
            }
            internal class Queen : Piece
            {
                internal Queen(ChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ChessImage.Instance._queenWhite : ChessImage.Instance._queenBlack;
                }

                internal override bool CanReach(Point dest, bool doNotCheckCastleOrPawnMove = false)
                {
                    if (Pos != dest)
                    {
                        // move horizontal
                        if (Pos.Y == dest.Y)
                        {
                            var i = Pos.X;
                            while (i != dest.X)
                            {
                                _ = dest.X > i ? ++i : --i;
                                if (i == dest.X)
                                {
                                    break;
                                }
                                if (Parent.PiecesOnBoard[i][Pos.Y] != null)
                                {
                                    return false;
                                }
                            }
                        }
                        else if (Pos.X == dest.X) // move vertically
                        {
                            var i = Pos.Y;
                            while (i != dest.Y)
                            {
                                _ = dest.Y > i ? ++i : --i;
                                if (i == dest.Y)
                                {
                                    break;
                                }
                                if (Parent.PiecesOnBoard[Pos.X][i] != null)
                                {
                                    return false;
                                }
                            }
                        }
                        else if (Math.Abs(Pos.X - dest.X) == Math.Abs(Pos.Y - dest.Y)) // move diagonal
                        {
                            var x = Pos.X;
                            var y = Pos.Y;
                            while (x != dest.X && y != dest.Y)
                            {
                                _ = dest.X > x ? ++x : --x;
                                _ = dest.Y > y ? ++y : --y;
                                if (x == dest.X)
                                {
                                    break;
                                }
                                if (Parent.PiecesOnBoard[x][y] != null)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                        var destPiece = Parent.PiecesOnBoard[dest.X][dest.Y];
                        if ((destPiece != null && Player != destPiece.Player) || destPiece == null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var reacheableSquares = new List<Point>();
                    if(Pos == pos)
                    {
                        return reacheableSquares;
                    }
                    if (Pos.Y == pos.Y)
                    {
                        var i = Pos.X;
                        while (i != pos.X)
                        {
                            _ = pos.X > i ? ++i : --i;
                            if (i == pos.X || Parent.PiecesOnBoard[i][Pos.Y] != null)
                            {
                                break;
                            }
                            else
                            {
                                reacheableSquares.Add(new Point(i, Pos.Y));
                            }
                        }
                    }
                    else if (Pos.X == pos.X)
                    {
                        var i = Pos.Y;
                        while (i != pos.Y)
                        {
                            _ = pos.Y > i ? ++i : --i;
                            if (i == pos.Y || Parent.PiecesOnBoard[Pos.X][i] != null)
                            {
                                break;
                            }
                            else
                            {
                                reacheableSquares.Add(new Point(Pos.X, i));
                            }
                        }
                    } else if (Math.Abs(Pos.X - pos.X) == Math.Abs(Pos.Y - pos.Y))
                    {
                        var x = Pos.X;
                        var y = Pos.Y;
                        while (x != pos.X && y != pos.Y)
                        {
                            _ = pos.X > x ? ++x : --x;
                            _ = pos.Y > y ? ++y : --y;
                            if (x == pos.X || Parent.PiecesOnBoard[x][y] != null)
                            {
                                break;
                            }
                            else
                            {
                                reacheableSquares.Add(new Point(x, y));
                            }
                        }
                    }
                    return reacheableSquares;
                }
                internal override List<Point> GetReacheableSquares()
                {
                    var reacheableSquares = new List<Point>();
                    for (var i = 0; i < 8; ++i)
                    {
                        reacheableSquares.Add(new Point(i, Pos.Y));
                        reacheableSquares.Add(new Point(Pos.X, i));
                        reacheableSquares.Add(new Point(Pos.X + i + 1, Pos.Y + i + 1));
                        reacheableSquares.Add(new Point(Pos.X - i - 1, Pos.Y + i + 1));
                        reacheableSquares.Add(new Point(Pos.X + i + 1, Pos.Y - i - 1));
                        reacheableSquares.Add(new Point(Pos.X - i - 1, Pos.Y - i - 1));
                    }
                    reacheableSquares.RemoveAll(x => x.X < 0 || x.X >= 8 || x.Y < 0 || x.Y >= 8 || !CanReach(x));
                    return reacheableSquares;
                }
            }
            internal class King : Piece
            {
                internal King(ChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ChessImage.Instance._kingWhite : ChessImage.Instance._kingBlack;
                }
                internal override void TryMove(Point dest)
                {
                    if (CanReach(dest))
                    {
                        // for castling
                        if(Math.Abs(Pos.X - dest.X) == 2)
                        {
                            // king side
                            if(dest.X > Pos.X)
                            {
                                var piece = Parent.PiecesOnBoard[Pos.X + 3][Pos.Y];
                                Rook rook = null;
                                if(piece.GetType() == typeof(Rook))
                                {
                                    rook = (Rook)piece;
                                }
                                rook.Pos = new Point(Pos.X + 1, Pos.Y);
                            } else
                            {
                                var piece = (Rook)Parent.PiecesOnBoard[Pos.X - 4][Pos.Y];
                                Rook rook = null;
                                if (piece.GetType() == typeof(Rook))
                                {
                                    rook = (Rook)piece;
                                }
                                rook.Pos = new Point(Pos.X - 1, Pos.Y);
                            }
                        }
                        Pos = new Point(dest.X, dest.Y);
                        HasMoved = true;
                        return;
                    }
                    throw new InvalidMoveException();
                }
                internal override bool CanReach(Point dest, bool doNotCheckCastle = false)
                {
                    if (Pos != dest)
                    {
                        // move one square
                        if (Math.Abs(Pos.X - dest.X) <= 1 && Math.Abs(Pos.Y - dest.Y) <= 1)
                        {
                            var destPiece = Parent.PiecesOnBoard[dest.X][dest.Y];
                            if ((destPiece != null && Player != destPiece.Player) || destPiece == null)
                            {
                                return true;
                            }
                        }
                        else
                        { // castling rules
                            if (doNotCheckCastle || HasMoved)
                            {
                                return false;
                            }
                            // check if in check
                            if (Parent.InCheck)
                            {
                                return false;
                            }
                            if (Math.Abs(Pos.X - dest.X) == 2 && Pos.Y == dest.Y)
                            {
                                // cast king side
                                if (dest.X > Pos.X)
                                {
                                    var rookPiece = Parent.PiecesOnBoard[Pos.X + 3][Pos.Y];
                                    Rook rook = null;
                                    if (rookPiece != null && rookPiece.GetType() == typeof(Rook))
                                    {
                                        rook = (Rook)rookPiece;
                                    }
                                    if (rook != null && !rook.HasMoved)
                                    {
                                        // check if path is reacheable by enemies
                                        var enemyPieces = Parent.Pieces.Where(x => x.Player != Player);
                                        foreach (var enemyPiece in enemyPieces)
                                        {
                                            if (enemyPiece.CanReach(new Point(Pos.X + 1, Pos.Y), true) ||
                                                enemyPiece.CanReach(new Point(Pos.X + 2, Pos.Y), true))
                                            {
                                                return false;
                                            }
                                        }
                                        for (var i = Pos.X + 1; i < Pos.X + 3; ++i)
                                        {
                                            var piece = Parent.PiecesOnBoard[i][Pos.Y];
                                            if (piece != null)
                                            {
                                                return false;
                                            }
                                        }
                                        return true;
                                    }
                                }
                                else
                                {
                                    var rookPiece = Parent.PiecesOnBoard[Pos.X - 4][Pos.Y];
                                    Rook rook = null;
                                    if (rookPiece != null && rookPiece.GetType() == typeof(Rook))
                                    {
                                        rook = (Rook)rookPiece;
                                    }
                                    if (rook != null && !rook.HasMoved)
                                    {
                                        // check if path is reacheable by enemies
                                        var enemyPieces = Parent.Pieces.Where(x => x.Player != Player);
                                        foreach (var enemyPiece in enemyPieces)
                                        {
                                            if (enemyPiece.CanReach(new Point(Pos.X - 1, Pos.Y), true) ||
                                                enemyPiece.CanReach(new Point(Pos.X - 2, Pos.Y), true))
                                            {
                                                return false;
                                            }
                                        }
                                        for (var i = Pos.X - 1; i > Pos.X - 4; --i)
                                        {
                                            var piece = Parent.PiecesOnBoard[i][Pos.Y];
                                            if (piece != null)
                                            {
                                                return false;
                                            }
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }
                internal override List<Point> GetReacheableSquares()
                {
                    var reacheableSquares = new List<Point>{
                        new Point(Pos.X + 1, Pos.Y + 1),
                        new Point(Pos.X + 1, Pos.Y),
                        new Point(Pos.X + 1, Pos.Y - 1),
                        new Point(Pos.X, Pos.Y + 1),
                        new Point(Pos.X, Pos.Y - 1),
                        new Point(Pos.X - 1, Pos.Y + 1),
                        new Point(Pos.X - 1, Pos.Y),
                        new Point(Pos.X - 1, Pos.Y - 1)
                    };
                    // for castling
                    if (!HasMoved)
                    {
                        reacheableSquares.Add(new Point(Pos.X - 2, Pos.Y));
                        reacheableSquares.Add(new Point(Pos.X + 2, Pos.Y));
                    }
                    reacheableSquares.RemoveAll(x => x.X < 0 || x.X >= 8 || x.Y < 0 || x.Y >= 8 || !CanReach(x));
                    return reacheableSquares;
                }
            }
        }
    }
}
