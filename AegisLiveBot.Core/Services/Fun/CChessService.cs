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
using static AegisLiveBot.Core.Services.Fun.ZoengKeiService.CChessBoard;

namespace AegisLiveBot.Core.Services.Fun
{
    public class ZoengKeiImage
    {
        private static readonly Lazy<ZoengKeiImage> lazy = new Lazy<ZoengKeiImage>(() => new ZoengKeiImage());
        private readonly string _imagePath;
        public readonly Image _chessBoardWhite;
        public readonly Image _chessBoardBlack;
        public readonly Image _soldierWhite;
        public readonly Image _cannonWhite;
        public readonly Image _chariotWhite;
        public readonly Image _horseWhite;
        public readonly Image _elephantWhite;
        public readonly Image _advisorWhite;
        public readonly Image _generalWhite;
        public readonly Image _soldierBlack;
        public readonly Image _cannonBlack;
        public readonly Image _chariotBlack;
        public readonly Image _horseBlack;
        public readonly Image _elephantBlack;
        public readonly Image _advisorBlack;
        public readonly Image _generalBlack;

        private ZoengKeiImage()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images/ZoengKei");
            _imagePath = path;
            _chessBoardWhite = Image.FromFile(Path.Combine(_imagePath, "board_white_ch.png"));
            _chessBoardBlack = Image.FromFile(Path.Combine(_imagePath, "board_black_ch.png"));
            _soldierWhite = Image.FromFile(Path.Combine(_imagePath, "soldier_white_ch.png"));
            _cannonWhite = Image.FromFile(Path.Combine(_imagePath, "cannon_white_ch.png"));
            _chariotWhite = Image.FromFile(Path.Combine(_imagePath, "chariot_white_ch.png"));
            _horseWhite = Image.FromFile(Path.Combine(_imagePath, "horse_white_ch.png"));
            _elephantWhite = Image.FromFile(Path.Combine(_imagePath, "elephant_white_ch.png"));
            _advisorWhite = Image.FromFile(Path.Combine(_imagePath, "advisor_white_ch.png"));
            _generalWhite = Image.FromFile(Path.Combine(_imagePath, "general_white_ch.png"));
            _soldierBlack = Image.FromFile(Path.Combine(_imagePath, "soldier_black_ch.png"));
            _cannonBlack = Image.FromFile(Path.Combine(_imagePath, "cannon_black_ch.png"));
            _chariotBlack = Image.FromFile(Path.Combine(_imagePath, "chariot_black_ch.png"));
            _horseBlack = Image.FromFile(Path.Combine(_imagePath, "horse_black_ch.png"));
            _elephantBlack = Image.FromFile(Path.Combine(_imagePath, "elephant_black_ch.png"));
            _advisorBlack = Image.FromFile(Path.Combine(_imagePath, "advisor_black_ch.png"));
            _generalBlack = Image.FromFile(Path.Combine(_imagePath, "general_black_ch.png"));
        }
        public static ZoengKeiImage Instance { get { return lazy.Value; } }
    }
    public class ZoengKeiService : IGameService
    {
        private readonly string _tempPath;
        private readonly CChessBoard Board;
        private readonly DiscordChannel _ch;
        private readonly DiscordClient _client;
        private readonly DiscordMember _whitePlayer;
        private readonly DiscordMember _blackPlayer;
        private const int _borderOffset = 40;
        private const int _tileSize = 80;
        private const int _secondsToDelete = 300;
        private Player CurrentPlayer;
        internal bool HasMoved { get; set; }
        internal bool ShowBoard { get; set; }
        public ZoengKeiService(DiscordChannel ch, DiscordMember p1, DiscordMember p2, DiscordClient client, string tempName)
        {
            _tempPath = Path.Combine(AppContext.BaseDirectory, "Temp/Images/ZoengKei", tempName);
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
            Board = new CChessBoard(this);
        }
        public void Start()
        {
            Task.Run(async () =>
            {
                var interactivity = _client.GetInteractivity();
                var initMsg = $"A Chinese Chess game has started between { _whitePlayer.Mention} and { _blackPlayer.Mention}!\n";
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
                    if (HasMoved)
                    {
                        if (CurrentPlayer == Player.White)
                        {
                            CurrentPlayer = Player.Black;
                            curPlayer = _blackPlayer;
                        }
                        else
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
                        helpMsg += $"flipboard: toggle showing flipped boards for black player\n";
                        helpMsg += $"showboard: shows the current board\n";
                        helpMsg += $"history: shows the history of the game\n";
                        helpMsg += $"draw: draws the game if both players draw\n";
                        helpMsg += $"Supports algebraic notation, for inputs with algebraic notation, visit and check out SYSTEM 3 <https://en.wikipedia.org/wiki/Xiangqi>";
                        await _ch.SendMessageAsync(helpMsg).ConfigureAwait(false);
                    }
                    else if (command.ToLower() == "resign")
                    {
                        var msg = $"{((DiscordMember)response.Result.Author).DisplayName} has resigned the game!\n";
                        var history = Board.WriteHistory(response.Result.Author.Id == _blackPlayer.Id ? Player.White : Player.Black, false);
                        msg += history;
                        await Dispose(msg).ConfigureAwait(false);
                        break;
                    }
                    else if (!HasMoved && command.ToLower() == "move" && response.Result.Author.Id == curPlayer.Id)
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
                                if (Board.CheckEnd())
                                {
                                    var msg = "";
                                    var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                                    await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
                                    //var history = Board.WriteHistory(Board.InCheck ? CurrentPlayer : Player.Draw);
                                    //msg += history;
                                    await Dispose(msg).ConfigureAwait(false);
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                            }
                        }
                    }
                    else if (command.ToLower() == "flipBoard")
                    {
                        flipBoard = !flipBoard;
                        var msg = flipBoard ? $"Boards will now be shown flipped for Black." : $"Boards will no longer be shown flipped.";
                        await _ch.SendMessageAsync(msg).ConfigureAwait(false);
                    }
                    else if (command.ToLower() == "showboard")
                    {
                        var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                        await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
                    }
                    else if (command.ToLower() == "history")
                    {
                        var history = Board.WriteHistory(Player.Draw, gameEnded: false);
                        await _ch.SendMessageAsync(history).ConfigureAwait(false);
                    }
                    else if (command.ToLower() == "draw")
                    {
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
                        }
                        else if (whitePlayerDraw || blackPlayerDraw)
                        {
                            await _ch.SendMessageAsync("Draw has been offered.").ConfigureAwait(false);
                        }
                        else
                        {
                            await _ch.SendMessageAsync("Draw offer has been rescinded.").ConfigureAwait(false);
                        }
                    }
                    else if (response.Result.Author.Id == curPlayer.Id)
                    {
                        // try to parse algebraic notation
                        if (responseSplit.Length == 1)
                        {
                            try
                            {
                                // remove last character if its :, x, +, # and indicate appropriately the action
                                var isCapture = false;
                                var isCheck = false;
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
                                    else
                                    {
                                        break;
                                    }
                                }
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
                                    var destLength = 2;
                                    if(command.Length >= 3 && command.Last() == '0')
                                    {
                                        destLength = 3;
                                    }
                                    var destCommand = command.Substring(command.Length - destLength, 2);
                                    command = command.Remove(command.Length - 2);
                                    dest = ParsePoint(destCommand.ToLower());
                                }
                                // we have the destination, get the piece
                                Type movePieceType = typeof(Soldier);
                                char file = 'z';
                                // if there is nothing at the start, it is a pawn move
                                if (command.Length >= 1)
                                {
                                    var movePieceChar = command.First();
                                    command = command.Remove(0, 1);
                                    var incorrectPiece = false;
                                    switch (movePieceChar)
                                    {
                                        case 'G':
                                            movePieceType = typeof(General);
                                            break;
                                        case 'E':
                                            movePieceType = typeof(Elephant);
                                            break;
                                        case 'A':
                                            movePieceType = typeof(Advisor);
                                            break;
                                        case 'C':
                                            movePieceType = typeof(Cannon);
                                            break;
                                        case 'H':
                                            movePieceType = typeof(Horse);
                                            break;
                                        case 'N':
                                            movePieceType = typeof(Horse);
                                            break;
                                        case 'R':
                                            movePieceType = typeof(Chariot);
                                            break;
                                        default:
                                            if (movePieceChar >= 'a' && movePieceChar <= 'i')
                                            {
                                                file = movePieceChar;
                                            } else
                                            {
                                                incorrectPiece = true;
                                            }
                                            break;
                                    }
                                    if (incorrectPiece)
                                    {
                                        continue;
                                    }
                                }
                                // if there is anything left, it is identifying
                                string rank = "";
                                Point origin = new Point(-1, -1);
                                // if there are two characters left, check if 10
                                if(command == "10")
                                {
                                    rank = "10";
                                }
                                else if (command.Length == 1)
                                {
                                    // if there is only one character left, check if file or rank
                                    var commandLower = char.ToLower(command.First());
                                    if (commandLower >= 'a' && commandLower <= 'i')
                                    {
                                        file = commandLower;
                                    }
                                    else if (commandLower >= '1' && commandLower <= '9')
                                    {
                                        rank = commandLower.ToString();
                                    }
                                }
                                else if(command.Length >= 2)
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
                                    var reachablePieces = Board.Pieces.Where(x => x.GetType() == movePieceType && x.Player == CurrentPlayer && x.CanReach(dest));
                                    // if there is only one, move that one
                                    if (reachablePieces.Count() == 1)
                                    {
                                        Board.TryMove(reachablePieces.ElementAt(0).Pos, dest);
                                    }
                                    else if (reachablePieces.Count() > 1)
                                    {
                                        // get by file if set, or rank if set
                                        if (file != 'z')
                                        {
                                            reachablePieces = reachablePieces.Where(x => x.Pos.X == file - 'a');
                                        }
                                        else if (rank != "")
                                        {
                                            reachablePieces = reachablePieces.Where(x => x.Pos.Y == int.Parse(rank) - 1);
                                        }
                                        // if still more than 1 reachable piece, unambiguous move
                                        if (reachablePieces.Count() == 1)
                                        {
                                            Board.TryMove(reachablePieces.ElementAt(0).Pos, dest);
                                        }
                                    }
                                }
                                if (Board.CheckEnd())
                                {
                                    var msg = "";
                                    msg = $"{curPlayer.Mention} has won the game!\n";
                                    var imagePath = Show(flipBoard ? CurrentPlayer : Player.White);
                                    await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
                                    var history = Board.WriteHistory(CurrentPlayer);
                                    msg += history;
                                    await Dispose(msg).ConfigureAwait(false);
                                    break;
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
            var xs = s[0];
            var ys = s.Remove(0, 1);
            int y;
            var yIsInt = int.TryParse(ys.ToString(), out y);
            if(xs < 'a' || xs > 'i' || !yIsInt || y < 0 || y > 10)
            {
                throw new ParsePointException();
            }
            return new Point(xs - 'a', y - 1);
        }
        private string Show(Player player)
        {
            try
            {
                var pieces = Board.Pieces;
                var boardImage = new Bitmap(760, 840);
                var imagePath = Path.Combine(_tempPath, "temp.jpg");
                using (Graphics g = Graphics.FromImage(boardImage))
                {
                    var chessBoardImage = player == Player.White ? ZoengKeiImage.Instance._chessBoardWhite : ZoengKeiImage.Instance._chessBoardBlack;
                    g.DrawImage(chessBoardImage, new Point(0, 0));
                    foreach (var piece in pieces)
                    {
                        var pos = piece.Pos;
                        var x = player == Player.White ? pos.X : (8 - pos.X);
                        var y = player == Player.White ? (9 - pos.Y) : pos.Y;
                        g.DrawImage(piece.Image, new Point(_borderOffset + x * _tileSize, y * _tileSize));
                    }
                    boardImage.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                }
                return imagePath;
            } catch(Exception e)
            {
                Console.Write(e.Message);
            }
            return "";
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
        internal class CChessBoard
        {
            internal List<List<Piece>> PiecesOnBoard { get; set; } // this allows for much faster retrieval
            internal List<Piece> Pieces { get; set; }
            internal ZoengKeiService Parent { get; set; }
            internal List<string> History { get; set; }
            internal CChessBoard()
            {

            }
            internal CChessBoard(ZoengKeiService parent)
            {
                Parent = parent;
                Pieces = new List<Piece>
                {
                    new Chariot(this, new Point(0, 0), Player.White),
                    new Chariot(this, new Point(8, 0), Player.White),
                    new Horse(this, new Point(1, 0), Player.White),
                    new Horse(this, new Point(7, 0), Player.White),
                    new Cannon(this, new Point(1, 2), Player.White),
                    new Cannon(this, new Point(7, 2), Player.White),
                    new Elephant(this, new Point(2, 0), Player.White),
                    new Elephant(this, new Point(6, 0), Player.White),
                    new Advisor(this, new Point(3, 0), Player.White),
                    new Advisor(this, new Point(5, 0), Player.White),
                    new Soldier(this, new Point(0, 3), Player.White),
                    new Soldier(this, new Point(2, 3), Player.White),
                    new Soldier(this, new Point(4, 3), Player.White),
                    new Soldier(this, new Point(6, 3), Player.White),
                    new Soldier(this, new Point(8, 3), Player.White),
                    new General(this, new Point(4, 0), Player.White),

                    new Chariot(this, new Point(0, 9), Player.Black),
                    new Chariot(this, new Point(8, 9), Player.Black),
                    new Horse(this, new Point(1, 9), Player.Black),
                    new Horse(this, new Point(7, 9), Player.Black),
                    new Cannon(this, new Point(1, 7), Player.Black),
                    new Cannon(this, new Point(7, 7), Player.Black),
                    new Elephant(this, new Point(2, 9), Player.Black),
                    new Elephant(this, new Point(6, 9), Player.Black),
                    new Advisor(this, new Point(3, 9), Player.Black),
                    new Advisor(this, new Point(5, 9), Player.Black),
                    new Soldier(this, new Point(0, 6), Player.Black),
                    new Soldier(this, new Point(2, 6), Player.Black),
                    new Soldier(this, new Point(4, 6), Player.Black),
                    new Soldier(this, new Point(6, 6), Player.Black),
                    new Soldier(this, new Point(8, 6), Player.Black),
                    new General(this, new Point(4, 9), Player.Black)
                };
                PiecesOnBoard = new List<List<Piece>>();
                for (var i = 0; i < 9; ++i)
                {
                    var column = new List<Piece>();
                    for (var j = 0; j < 10; ++j)
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
                if (piece != null && piece.Player == Parent.CurrentPlayer)
                {
                    try
                    {
                        if (!piece.CanReach(dest))
                        {
                            throw new InvalidMoveException();
                        }
                        // check if this move puts you in check
                        var tempPiece = PiecesOnBoard[dest.X][dest.Y];
                        try
                        {
                            PiecesOnBoard[origin.X][origin.Y] = null;
                            PiecesOnBoard[dest.X][dest.Y] = piece;
                            if (piece.GetType() == typeof(General))
                            {
                                piece.Pos = dest;
                            }
                            CheckValidMove(false, tempPiece);
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                        finally
                        {
                            PiecesOnBoard[origin.X][origin.Y] = piece;
                            PiecesOnBoard[dest.X][dest.Y] = tempPiece;
                            if (piece.GetType() == typeof(General))
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
                        PiecesOnBoard[origin.X][origin.Y] = null;
                        PiecesOnBoard[piece.Pos.X][piece.Pos.Y] = piece;
                        // write to history
                        var moveString = "";
                        var pieceType = piece.GetType();
                        char pieceIdentifier = '\0';
                        if (pieceType == typeof(General))
                        {
                            pieceIdentifier = 'G';
                        }
                        else if (pieceType == typeof(Elephant))
                        {
                            pieceIdentifier = 'E';
                        }
                        else if (pieceType == typeof(Advisor))
                        {
                            pieceIdentifier = 'A';
                        }
                        else if (pieceType == typeof(Cannon))
                        {
                            pieceIdentifier = 'C';
                        }
                        else if (pieceType == typeof(Horse))
                        {
                            pieceIdentifier = 'H';
                        }
                        else if (pieceType == typeof(Chariot))
                        {
                            pieceIdentifier = 'R';
                        }
                        else
                        {
                            pieceIdentifier = (char)('a' + origin.X);
                        }
                        var pieceFile = (char)('0' + origin.X);
                        var pieceRank = (char)('a' + origin.Y);
                        var destString = ((char)('a' + dest.X)).ToString() + ((char)('1' + dest.Y)).ToString();
                        var originString = "";
                        var myPieces = Pieces.Where(x => x.Player == Parent.CurrentPlayer &&
                        x.GetType() != typeof(Soldier) && x.GetType() == pieceType && x.CanReach(dest));
                        // pawns dont need origin identifiers
                        if (myPieces.Count() >= 1) // if one or more of my pieces of same type can also reach that location
                        {
                            // check if can be separated by file
                            if (myPieces.Where(x => x.Pos.X == origin.X).Count() == 0)
                            {
                                originString += pieceFile;
                            }
                            else if (myPieces.Where(x => x.Pos.Y == origin.Y).Count() == 0)
                            {
                                originString += pieceRank;
                            }
                            else
                            {
                                originString += pieceFile += pieceRank;
                            }
                        }
                        else
                        {
                            // if its not a pawn or is taking, put piece identifier
                            if (pieceType != typeof(Soldier) || destPiece != null)
                            {
                                moveString += pieceIdentifier;
                            }
                            moveString += originString;
                            if (destPiece != null)
                            {
                                moveString += 'x';
                            }
                            moveString += destString;
                        }
                        History.Add(moveString);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else
                {
                    throw new InvalidMoveException();
                }
                Parent.HasMoved = true;
                Parent.ShowBoard = true;
            }
            // checks if current move will put you in check
            internal void CheckValidMove(bool currentPlayer = false, Piece ignorePiece = null)
            {
                var pieces = currentPlayer ? Pieces.Where(x => x.Player == Parent.CurrentPlayer && x != ignorePiece) : Pieces.Where(x => x.Player != Parent.CurrentPlayer && x != ignorePiece);
                var generalPiece = currentPlayer ? Pieces.FirstOrDefault(x => x.Player != Parent.CurrentPlayer && x.GetType() == typeof(General))
                    : Pieces.FirstOrDefault(x => x.Player == Parent.CurrentPlayer && x.GetType() == typeof(General));
                foreach (var piece in pieces)
                {
                    if (piece.CanReach(generalPiece.Pos))
                    {
                        throw new CheckMoveException();
                    }
                }
            }
            internal bool CheckEnd()
            {
                var myPieces = Pieces.Where(x => x.Player == Parent.CurrentPlayer);
                var enemyPieces = Pieces.Where(x => x.Player != Parent.CurrentPlayer);
                var generalPiece = enemyPieces.FirstOrDefault(x => x.GetType() == typeof(General));
                var checkingPieces = new List<Piece>();
                foreach (var myPiece in myPieces)
                {
                    if (myPiece.CanReach(generalPiece.Pos))
                    {
                        checkingPieces.Add(myPiece);
                    }
                }
                if (checkingPieces.Count == 0)
                {
                    // if not in check, check if there are any valid moves
                    foreach (var enemyPiece in enemyPieces)
                    {
                        var enemyReachableSquares = enemyPiece.GetReachableSquares();
                        Piece temp = null;
                        Point tempPoint = enemyPiece.Pos;
                        foreach (var enemyReachableSquare in enemyReachableSquares)
                        {
                            try
                            {
                                temp = PiecesOnBoard[enemyReachableSquare.X][enemyReachableSquare.Y];
                                PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = null;
                                PiecesOnBoard[enemyReachableSquare.X][enemyReachableSquare.Y] = enemyPiece;
                                if (enemyPiece.GetType() == typeof(General))
                                {
                                    enemyPiece.Pos = enemyReachableSquare;
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
                                if (enemyPiece.GetType() == typeof(General))
                                {
                                    enemyPiece.Pos = tempPoint;
                                }
                                PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = enemyPiece;
                                PiecesOnBoard[enemyReachableSquare.X][enemyReachableSquare.Y] = temp;
                            }
                        }
                    }
                }
                else
                {
                    // add check mark if check
                    var moveString = History.Last() + '+';
                    History.RemoveAt(History.Count() - 1);
                    History.Add(moveString);
                    var generalReachableSquares = generalPiece.GetReachableSquares();
                    // check that there is a square that the king can move to without being checked
                    foreach (var generalReachableSquare in generalReachableSquares)
                    {
                        // if this is a king take, temporarily swap out that piece
                        var tempPiece = PiecesOnBoard[generalReachableSquare.X][generalReachableSquare.Y];
                        PiecesOnBoard[generalReachableSquare.X][generalReachableSquare.Y] = generalPiece;
                        // if this is a square that escapes check, check if any other pieces can reach this square
                        var canReach = myPieces.FirstOrDefault(x => x.CanReach(generalReachableSquare));
                        PiecesOnBoard[generalReachableSquare.X][generalReachableSquare.Y] = tempPiece;
                        if (canReach == null)
                        {
                            return false;
                        }
                    }
                    var checkingPiece = checkingPieces[0];
                    // check if the checking piece can be captured without getting discover checked
                    // we have already checked king takes checking piece, check everything else
                    foreach (var enemyPiece in enemyPieces)
                    {
                        if (enemyPiece.GetType() == typeof(General))
                        {
                            continue;
                        }
                        if (enemyPiece.CanReach(checkingPiece.Pos))
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
                    // if checking piece is a soldier, it cannot be blocked, therefore cant capture = checkmate
                    if (checkingPiece.GetType() == typeof(Soldier))
                    {
                        return true;
                    }
                    else
                    // otherwise check if can be blocked
                    {
                        var blockeableSquares = checkingPiece.GetPathToPos(generalPiece.Pos);
                        foreach (var blockeableSquare in blockeableSquares)
                        {
                            foreach (var enemyPiece in enemyPieces)
                            {
                                if (enemyPiece.CanReach(blockeableSquare))
                                {
                                    try
                                    {
                                        PiecesOnBoard[enemyPiece.Pos.X][enemyPiece.Pos.Y] = null;
                                        PiecesOnBoard[blockeableSquare.X][blockeableSquare.Y] = enemyPiece;
                                        CheckValidMove(true);
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
                                        PiecesOnBoard[blockeableSquare.X][blockeableSquare.Y] = null;
                                    }
                                }
                            }
                        }
                    }
                }
                // if cannot be blocked, return true
                return true;
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
                for (var i = 0; i < History.Count(); i += 2)
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
                internal CChessBoard Parent { get; set; }
                internal Point Pos { get; set; }
                internal Player Player { get; set; }
                internal Image Image { get; set; }
                internal Piece(CChessBoard chessBoard, Point pos, Player player)
                {
                    Parent = chessBoard;
                    Pos = pos;
                    Player = player;
                }
                internal virtual void TryMove(Point dest)
                {
                    if(Parent.PiecesOnBoard[dest.X][dest.Y] != null && Player == Parent.PiecesOnBoard[dest.X][dest.Y].Player)
                    {
                        throw new InvalidMoveException();
                    }
                    if (CanReach(dest))
                    {
                        Pos = new Point(dest.X, dest.Y);
                        return;
                    }
                    throw new InvalidMoveException();
                }
                internal abstract bool CanReach(Point dest);
                internal virtual List<Point> GetPathToPos(Point pos) { return null; }
                internal abstract List<Point> GetReachableSquares();
            }
            internal class Soldier : Piece
            {
                internal Soldier(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._soldierWhite : ZoengKeiImage.Instance._soldierBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    // move one square only
                    if(Math.Abs(dest.X - Pos.X + dest.Y - Pos.Y) != 1 || Math.Abs(dest.X - Pos.X) > 1 || Math.Abs(dest.Y - Pos.Y) > 1)
                    {
                        return false;
                    }
                    // move only forward
                    if(Player == Player.White && dest.Y < Pos.Y || Player == Player.Black && dest.Y > Pos.Y)
                    {
                        return false;
                    }
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if(piece != null && piece.Player == Player)
                    {
                        return false;
                    }
                    // if river crossed, can also move horizontally
                    var riverCrossed = (Player == Player.White && Pos.Y > 4) || (Player == Player.Black && Pos.Y < 5);
                    if(dest.X == Pos.X || (riverCrossed && dest.Y == Pos.Y))
                    {
                        return true;
                    }
                    return false;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>
                    {
                        new Point(Pos.X + 1, Pos.Y),
                        new Point(Pos.X - 1, Pos.Y),
                        new Point(Pos.X, Pos.Y + 1),
                        new Point(Pos.X, Pos.Y - 1),
                    };
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
            internal class Cannon : Piece
            {
                internal Cannon(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._cannonWhite : ZoengKeiImage.Instance._cannonBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    // cannon captures over a piece
                    if(dest == Pos)
                    {
                        return false;
                    }
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (piece != null && piece.Player == Player)
                    {
                        return false;
                    }
                    var hasCover = false;
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
                                if(hasCover && i != dest.X)
                                {
                                    return false;
                                }
                                hasCover = true;
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
                                if (hasCover && i != dest.X)
                                {
                                    return false;
                                }
                                hasCover = true;
                            }
                        }
                    } else
                    {
                        return false;
                    }
                    if(Parent.PiecesOnBoard[dest.X][dest.Y] == null)
                    {
                        if (!hasCover)
                        {
                            return true;
                        } else
                        {
                            return false;
                        }
                    } else
                    {
                        if (!hasCover)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var path = new List<Point>();
                    if (Pos.Y == pos.Y)
                    {
                        var i = Pos.X;
                        while (i != pos.X)
                        {
                            _ = pos.X > i ? ++i : --i;
                            if (i == pos.X)
                            {
                                break;
                            }
                            // since this piece takes over cover, also check behind
                            // we assume this can reach dest, so dont check for second cover
                            if (Parent.PiecesOnBoard[i][Pos.Y] != null)
                            {
                                continue;
                            }
                            path.Add(new Point(i, Pos.Y));
                        }
                    }
                    else if (Pos.X == pos.X) // move vertically
                    {
                        var i = Pos.Y;
                        while (i != pos.Y)
                        {
                            _ = pos.Y > i ? ++i : --i;
                            if (i == pos.Y)
                            {
                                break;
                            }
                            if (Parent.PiecesOnBoard[Pos.X][i] != null)
                            {
                                continue;
                            }
                            path.Add(new Point(Pos.X, i));
                        }
                    }
                    return path;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>();
                    for(var i = 1; i < 10; ++i)
                    {
                        reachableSquares.Add(new Point(Pos.X + i, Pos.Y));
                        reachableSquares.Add(new Point(Pos.X - i, Pos.Y));
                        reachableSquares.Add(new Point(Pos.X, Pos.Y + i));
                        reachableSquares.Add(new Point(Pos.X, Pos.Y - i));
                    }
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
            internal class Chariot : Piece
            {
                internal Chariot(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._chariotWhite : ZoengKeiImage.Instance._chariotBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    if (dest == Pos)
                    {
                        return false;
                    }
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (piece != null && piece.Player == Player)
                    {
                        return false;
                    }
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
                    else
                    {
                        return false;
                    }
                    return true;
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var path = new List<Point>();
                    if (Pos.Y == pos.Y)
                    {
                        var i = Pos.X;
                        while (i != pos.X)
                        {
                            _ = pos.X > i ? ++i : --i;
                            if (i == pos.X)
                            {
                                break;
                            }
                            path.Add(new Point(i, Pos.Y));
                        }
                    }
                    else if (Pos.X == pos.X) // move vertically
                    {
                        var i = Pos.Y;
                        while (i != pos.Y)
                        {
                            _ = pos.Y > i ? ++i : --i;
                            if (i == pos.Y)
                            {
                                break;
                            }
                            path.Add(new Point(Pos.X, i));
                        }
                    }
                    return path;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>();
                    for (var i = 1; i < 10; ++i)
                    {
                        reachableSquares.Add(new Point(Pos.X + i, Pos.Y));
                        reachableSquares.Add(new Point(Pos.X - i, Pos.Y));
                        reachableSquares.Add(new Point(Pos.X, Pos.Y + i));
                        reachableSquares.Add(new Point(Pos.X, Pos.Y - i));
                    }
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
            internal class Horse : Piece
            {
                internal Horse(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._horseWhite : ZoengKeiImage.Instance._horseBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (piece != null && piece.Player == Player)
                    {
                        return false;
                    }
                    if ((Math.Abs(Pos.X - dest.X) == 2 && Math.Abs(Pos.Y - dest.Y) == 1) ||
                        (Math.Abs(Pos.X - dest.X) == 1 && Math.Abs(Pos.Y - dest.Y) == 2))
                    {
                        // horse cannot move forward if blocked
                        Piece blocker = null;
                        if(dest.Y - Pos.Y == 2)
                        {
                            blocker = Parent.PiecesOnBoard[Pos.X][Pos.Y + 1];
                        }
                        else if (dest.Y - Pos.Y == -2)
                        {
                            blocker = Parent.PiecesOnBoard[Pos.X][Pos.Y - 1];
                        }
                        else if (dest.X - Pos.X == 2)
                        {
                            blocker = Parent.PiecesOnBoard[Pos.X + 1][Pos.Y];
                        }
                        else if (dest.X - Pos.X == -2)
                        {
                            blocker = Parent.PiecesOnBoard[Pos.X - 1][Pos.Y];
                        }

                        if (blocker == null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var path = new List<Point>();
                    if (pos.Y - Pos.Y == 2)
                    {
                        path.Add(new Point(Pos.X, Pos.Y + 1));
                    }
                    else if (pos.Y - Pos.Y == -2)
                    {
                        path.Add(new Point(Pos.X, Pos.Y - 1));
                    }
                    else if (pos.X - Pos.X == 2)
                    {
                        path.Add(new Point(Pos.X + 1, Pos.Y));
                    }
                    else if (pos.X - Pos.X == -2)
                    {
                        path.Add(new Point(Pos.X - 1, Pos.Y));
                    }
                    return path;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>
                    {
                        new Point(Pos.X + 2, Pos.Y + 1),
                        new Point(Pos.X + 2, Pos.Y - 1),
                        new Point(Pos.X - 2, Pos.Y + 1),
                        new Point(Pos.X - 2, Pos.Y - 1),
                        new Point(Pos.X + 1, Pos.Y + 2),
                        new Point(Pos.X + 1, Pos.Y - 2),
                        new Point(Pos.X - 1, Pos.Y + 2),
                        new Point(Pos.X - 1, Pos.Y - 2)
                    };
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
            internal class Elephant : Piece
            {
                internal Elephant(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._elephantWhite : ZoengKeiImage.Instance._elephantBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    // elephants cannot move past river
                    if((Player == Player.White && dest.Y > 4) || (Player == Player.Black && dest.Y < 5))
                    {
                        return false;
                    }
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (piece != null && piece.Player == Player)
                    {
                        return false;
                    }
                    if (Math.Abs(Pos.X - dest.X) == Math.Abs(Pos.Y - dest.Y) && Math.Abs(Pos.X - dest.X) == 2)
                    {
                        // elephants cannot move if blocked
                        Piece blocker = Parent.PiecesOnBoard[(Pos.X + dest.X) / 2][(Pos.Y + dest.Y)/ 2];

                        if (blocker == null)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>
                    {
                        new Point(Pos.X + 2, Pos.Y + 2),
                        new Point(Pos.X + 2, Pos.Y - 2),
                        new Point(Pos.X - 2, Pos.Y + 2),
                        new Point(Pos.X - 2, Pos.Y - 2)
                    };
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
            internal class Advisor : Piece
            {
                internal Advisor(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._advisorWhite : ZoengKeiImage.Instance._advisorBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    // advisor only has 5 available squares
                    if(dest.X < 3 || dest.X > 5 || (Player == Player.White && dest.Y > 2) || (Player == Player.Black && dest.Y < 7))
                    {
                        return false;
                    }
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (piece != null && piece.Player == Player)
                    {
                        return false;
                    }
                    if (Math.Abs(Pos.X - dest.X) == Math.Abs(Pos.Y - dest.Y) && Math.Abs(Pos.X - dest.X) == 1)
                    {
                        return true;
                    }
                    return false;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>
                    {
                        new Point(Pos.X + 1, Pos.Y + 1),
                        new Point(Pos.X + 1, Pos.Y - 1),
                        new Point(Pos.X - 1, Pos.Y + 1),
                        new Point(Pos.X - 1, Pos.Y - 1)
                    };
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
            internal class General : Piece
            {
                internal General(CChessBoard chessBoard, Point pos, Player player) : base(chessBoard, pos, player)
                {
                    Image = player == Player.White ? ZoengKeiImage.Instance._generalWhite : ZoengKeiImage.Instance._generalBlack;
                }
                internal override bool CanReach(Point dest)
                {
                    // if generals has a clear shot to the enemy general, return true
                    if(Parent.PiecesOnBoard[dest.X][dest.Y] != null && Parent.PiecesOnBoard[dest.X][dest.Y].GetType() == typeof(General))
                    {
                        if (dest.X == Pos.X)
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
                        return true;
                    }
                    var piece = Parent.PiecesOnBoard[dest.X][dest.Y];
                    if (piece != null && piece.Player == Player)
                    {
                        return false;
                    }
                    // generals only has 9 available squares
                    if (dest.X < 3 || dest.X > 5 || (Player == Player.White && dest.Y > 2) || (Player == Player.Black && dest.Y < 7))
                    {
                        return false;
                    }// move one square only
                    if (Math.Abs(dest.X - Pos.X + dest.Y - Pos.Y) != 1 || Math.Abs(dest.X - Pos.X) > 1 || Math.Abs(dest.Y - Pos.Y) > 1)
                    {
                        return false;
                    }
                    return true;
                }
                internal override List<Point> GetPathToPos(Point pos)
                {
                    var path = new List<Point>();
                    var i = Pos.Y;
                    while (i != pos.Y)
                    {
                        _ = pos.Y > i ? ++i : --i;
                        if (i == pos.Y)
                        {
                            break;
                        }
                        path.Add(new Point(Pos.X, i));
                    }
                    return path;
                }
                internal override List<Point> GetReachableSquares()
                {
                    var reachableSquares = new List<Point>
                    {
                        new Point(Pos.X + 1, Pos.Y),
                        new Point(Pos.X - 1, Pos.Y),
                        new Point(Pos.X, Pos.Y + 1),
                        new Point(Pos.X, Pos.Y - 1)
                    };
                    reachableSquares = reachableSquares.Where(x => x.X >= 0 && x.X <= 8 && x.Y >= 0 && x.Y <= 9 && CanReach(x)).ToList();
                    return reachableSquares;
                }
            }
        }
    }
}
