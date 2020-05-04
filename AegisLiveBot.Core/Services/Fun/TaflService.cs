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
using static AegisLiveBot.Core.Services.Fun.TaflService.TaflBoard;

namespace AegisLiveBot.Core.Services.Fun
{
    public class TaflImage
    {
        private static readonly Lazy<TaflImage> lazy = new Lazy<TaflImage>(() => new TaflImage());
        private readonly string _imagePath;
        public readonly Image _tile;
        public readonly Image _tileDark;
        public readonly Image _king;
        public readonly Image _white;
        public readonly Image _black;

        private TaflImage()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images/Tafl");
            _imagePath = path;
            _tile = Image.FromFile(Path.Combine(_imagePath, "tile.jpg"));
            _tileDark = Image.FromFile(Path.Combine(_imagePath, "tile_dark.jpg"));
            _king = Image.FromFile(Path.Combine(_imagePath, "king.png"));
            _white = Image.FromFile(Path.Combine(_imagePath, "pawn_white.png"));
            _black = Image.FromFile(Path.Combine(_imagePath, "pawn_black.png"));
        }
        public static TaflImage Instance { get { return lazy.Value; } }
    }
    public class PointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JToken.Load(reader);
            if(obj.Type == JTokenType.Array)
            {
                var arr = (JArray)obj;
                if (arr.Count == 2 && arr.All(token => token.Type == JTokenType.Integer))
                {
                    return new Point(arr[0].Value<int>(), arr[1].Value<int>());
                }
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    public class TaflConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; private set; }
        [JsonProperty("size")]
        public int Size { get; private set; }
        [JsonProperty("blackPositions")]
        public List<Point> BlackPositions { get; private set; }
        [JsonProperty("whitePositions")]
        public List<Point> WhitePositions { get; private set; }
        [JsonProperty("strongKing")]
        public bool StrongKing { get; private set; }
        [JsonProperty("winOnCorner")]
        public bool WinOnCorner { get; private set; }
        [JsonProperty("cornerRestricted")]
        public bool CornerRestricted { get; private set; }
    }
    public class TaflService : IGameService
    {
        private TaflConfiguration TaflConfiguration;
        private TaflBoard Board;
        private FontFamily _fontFamily;
        private Font _font;
        private SolidBrush _solidBrush;
        private const int _tileSize = 64;

        public Image _background;
        public Image _boardIndex;

        private DiscordChannel _ch;
        private DiscordMember _blackPlayer;
        private DiscordMember _whitePlayer;
        private DiscordClient _client;
        private string _tempPath;

        private const int _secondsToDelete = 60;
        internal Piece CurrentPlayer { get; private set; }

        public TaflService(DiscordChannel ch, DiscordMember p1, DiscordMember p2, DiscordClient client, string tempName)
        {
            _fontFamily = new FontFamily("Arial");
            _font = new Font(_fontFamily, 24, FontStyle.Bold, GraphicsUnit.Pixel);
            _solidBrush = new SolidBrush(Color.DarkGray);
            _tempPath = Path.Combine(AppContext.BaseDirectory, "Temp/Images/Tafl", tempName);
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
        }
        private async Task<TaflConfiguration> PickBoard()
        {
            var json = string.Empty;

            using (var fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data/Fun/taflConfig.json")))
            {
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = sr.ReadToEnd();
                }
            }

            var configurations = JObject.Parse(json);
            var configurationsList = configurations["configurations"].ToString();
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new PointConverter() }
            };
            var taflConfigurations = JsonConvert.DeserializeObject<List<TaflConfiguration>>(configurationsList);
            var msg = $"A Tafl game has been created for {_blackPlayer.Mention} and {_whitePlayer.Mention}.\n";
            msg += $"Select or preview one of the following configurations:\n";
            for(var i = 0; i < taflConfigurations.Count; ++i)
            {
                msg += $"{i + 1}. {taflConfigurations.ElementAt(i).Name}\n";
            }
            msg += $"Use the commands: select <index/name>, preview <index/name>";
            await _ch.SendMessageAsync(msg).ConfigureAwait(false);
            var interactivity = _client.GetInteractivity();
            while (true)
            {
                var response = await interactivity.WaitForMessageAsync(x => (x.Author.Id == _blackPlayer.Id || x.Author.Id == _whitePlayer.Id) && x.ChannelId == _ch.Id).ConfigureAwait(false);
                var result = response.Result.Content.ToLower();
                var resultSplit = result.Split(" ");
                bool select = true;
                if(resultSplit.Length < 2)
                {
                    continue;
                }
                var command = resultSplit[0];
                if(command == "select")
                {
                    select = true;
                } else if(command == "preview")
                {
                    select = false;
                } else
                {
                    continue;
                }
                var index = 0;
                bool isIndex = int.TryParse(resultSplit[1], out index);
                TaflConfiguration selected = null;
                if (isIndex)
                {
                    if(index > 0 && index <= taflConfigurations.Count)
                    {
                        selected = taflConfigurations.ElementAt(index - 1);
                    } else
                    {
                        await _ch.SendMessageAsync($"Please specify a valid index.").ConfigureAwait(false);
                        continue;
                    }
                } else
                {
                    var resultName = string.Join(" ", resultSplit.Skip(1));
                    taflConfigurations.FirstOrDefault(x => x.Name.ToLower() == result.ToLower());
                }
                if(selected == null)
                {
                    await _ch.SendMessageAsync($"Please specify a valid name.").ConfigureAwait(false);
                    continue;
                }
                if (select)
                {
                    await _ch.SendMessageAsync($"{selected.Name} has been selected.").ConfigureAwait(false);
                    return selected;
                } else
                {
                    await PreviewBoard(selected).ConfigureAwait(false);
                    continue;
                }
            }
        }
        private async Task PreviewBoard(TaflConfiguration taflConfiguration)
        {
            TaflConfiguration = taflConfiguration;
            Board = new TaflBoard(taflConfiguration, this);
            _background = DrawBoard();
            _boardIndex = DrawBoardIndex();
            var board = Draw();
            await _ch.SendFileAsync(board, $"Now previewing {taflConfiguration.Name}.").ConfigureAwait(false);
        }
        private Image DrawBoard()
        {
            var size = TaflConfiguration.Size;
            var boardImage = new Bitmap(size * _tileSize, size * _tileSize);
            using (Graphics g = Graphics.FromImage(boardImage))
            {
                for(var i = 0; i < size; ++i)
                {
                    for(var j = 0; j < size; ++j)
                    {
                        g.DrawImage(TaflImage.Instance._tile, new Point(i * _tileSize, j * _tileSize));
                    }
                }
                g.DrawImage(TaflImage.Instance._tileDark, new Point((size / 2) * _tileSize, (size / 2) * _tileSize));
                if (TaflConfiguration.CornerRestricted)
                {
                    g.DrawImage(TaflImage.Instance._tileDark, new Point(0, 0));
                    g.DrawImage(TaflImage.Instance._tileDark, new Point(0, (size - 1) * _tileSize));
                    g.DrawImage(TaflImage.Instance._tileDark, new Point((size - 1) * _tileSize, 0));
                    g.DrawImage(TaflImage.Instance._tileDark, new Point((size - 1) * _tileSize, (size - 1) * _tileSize));
                }
                boardImage.Save(Path.Combine(_tempPath, "background.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            Image img;
            using(var temp = new Bitmap(Path.Combine(_tempPath, "background.jpg")))
            {
                return img = new Bitmap(temp);
            }
        }
        private Image DrawBoardIndex()
        {
            var size = TaflConfiguration.Size;
            var boardIndex = new Bitmap(size * _tileSize, size * _tileSize);
            boardIndex.MakeTransparent();
            using (Graphics g = Graphics.FromImage(boardIndex))
            {
                for (var i = 0; i < size; ++i)
                {
                    g.DrawString((i + 1).ToString(), _font, _solidBrush, new Point(0, 20 + (size - i - 1) * _tileSize));
                    g.DrawString(((char)('a' + i)).ToString(), _font, _solidBrush, new Point(20 + i * _tileSize, 36 + (size - 1) * _tileSize));
                }
                boardIndex.Save(Path.Combine(_tempPath, "boardIndex.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            Image img;
            using (var temp = new Bitmap(Path.Combine(_tempPath, "boardIndex.png")))
            {
                return img = new Bitmap(temp);
            }
        }
        private string Draw()
        {
            var size = TaflConfiguration.Size;
            var boardImage = new Bitmap(size * _tileSize, size * _tileSize);
            var tiles = Board.GetTiles();
            using(Graphics g = Graphics.FromImage(boardImage))
            {
                g.DrawImage(_background, new Point(0, 0));
                for (int y = 0; y < size; ++y)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        if(tiles[y][x].Piece == Piece.White)
                        {
                            g.DrawImage(TaflImage.Instance._white, new Point(x * _tileSize, y * _tileSize));
                        } else if(tiles[y][x].Piece == Piece.Black)
                        {
                            g.DrawImage(TaflImage.Instance._black, new Point(x * _tileSize, y * _tileSize));
                        } else if(tiles[y][x].Piece == Piece.King)
                        {
                            g.DrawImage(TaflImage.Instance._king, new Point(x * _tileSize, y * _tileSize));
                        }
                    }
                }
                g.DrawImage(_boardIndex, new Point(0, 0));
                var filePath = Path.Combine(_tempPath, "temp.jpg");
                boardImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                return filePath;
            }
        }
        private Point TryStringToPoint(string s)
        {
            var x = -1;
            var y = -1;
            var i = 0;
            if (s.Length > i && char.IsLetter(s[i]))
            {
                var c = s[i];
                ++i;
                x = int.Parse((c - 'a').ToString());
            } else
            {
                throw new StringToPointException();
            }
            var yString = "";
            while (s.Length > i && char.IsDigit(s[i]))
            {
                yString += s[i];
                ++i;
            }
            try
            {
                y = int.Parse(yString);
            } catch(Exception)
            {
                throw new StringToPointException();
            }
            var size = TaflConfiguration.Size;
            var point = new Point(x, size - y);
            if(point.X < 0 || point.Y < 0 || point.X >= size || point.Y >= size)
            {
                throw new PointOutOfBoundsException();
            }
            return point;
        }

        private async Task<bool> TryMove(string response)
        {
            var responseList = response.Split(" ");
            if(responseList.Length == 0)
            {
                return false;
            }
            if(responseList[0] == "move")
            {
                if(responseList.Length < 3)
                {
                    return false;
                }
                try
                {
                    var piece = TryStringToPoint(responseList[1]);
                    var location = TryStringToPoint(responseList[2]);
                    Board.TryMove(piece, location);
                    return true;
                } catch(Exception e)
                {
                    await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                }
            } else if(responseList[0] == "help")
            {
                var msg = $"To move pieces, use the following command: move <piece> <location>\n";
                msg += $"Example: move e2 d2";
                await _ch.SendMessageAsync(msg).ConfigureAwait(false);
            } else if(responseList[0] == "quit")
            {
                await Dispose().ConfigureAwait(false);
            }
            return false;
        }
        public void Start()
        {
            Task.Run(async () =>
            {
                var interactivity = _client.GetInteractivity();

                TaflConfiguration = await PickBoard().ConfigureAwait(false);
                Board = new TaflBoard(TaflConfiguration, this);
                _background = DrawBoard();
                _boardIndex = DrawBoardIndex();

                var board = Draw();
                await _ch.SendFileAsync(board, $"Tafl game has started!").ConfigureAwait(false);
                var startMsg = $"{_blackPlayer.DisplayName}(Black) goes first!\n";
                startMsg += $"Type 'help' for help, or 'quit' to quit game.\n";
                startMsg += $"For detailed rules, click here: http://aagenielsen.dk/tafl_rules.php";
                await _ch.SendMessageAsync(startMsg).ConfigureAwait(false);

                CurrentPlayer = Piece.Black;
                var curPlayer = _blackPlayer;
                while (true)
                {
                    var afk = false;
                    var isMove = false;
                    while (!isMove)
                    {
                        var response = await interactivity.WaitForMessageAsync(x => x.Author.Id == curPlayer.Id && x.ChannelId == _ch.Id).ConfigureAwait(false);
                        if (response.TimedOut)
                        {
                            await _ch.SendMessageAsync($"This channel will be deleted soon unless there is activity.").ConfigureAwait(false);
                            if (afk)
                            {
                                await Dispose().ConfigureAwait(false);
                            }
                            afk = true;
                            continue;
                        }
                        isMove = await TryMove(response.Result.Content.ToLower()).ConfigureAwait(false);
                        afk = false;
                    }
                    board = Draw();
                    await _ch.SendFileAsync(board).ConfigureAwait(false);
                    var color = CurrentPlayer == Piece.Black ? "Black" : "White";
                    if (Board.HasWin())
                    {
                        await _ch.SendMessageAsync($"{curPlayer.DisplayName}({color}) has won the game!").ConfigureAwait(false);
                        break;
                    }
                    if (CurrentPlayer == Piece.Black)
                    {
                        CurrentPlayer = Piece.White;
                        curPlayer = _whitePlayer;
                        color = "White";
                    } else
                    {
                        CurrentPlayer = Piece.Black;
                        curPlayer = _blackPlayer;
                        color = "Black";
                    }
                    await _ch.SendMessageAsync($"{curPlayer.DisplayName}({color})'s turn to move!").ConfigureAwait(false);
                }
                await Dispose().ConfigureAwait(false);
            });
        }
        private async Task Dispose()
        {
            _background.Dispose();
            _boardIndex.Dispose();
            DirectoryInfo dir = new DirectoryInfo(_tempPath);
            foreach(FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            Directory.Delete(_tempPath);
            await _ch.SendMessageAsync($"Channel will be deleted in {_secondsToDelete} seconds.").ConfigureAwait(false);
            await Task.Delay(_secondsToDelete * 1000).ConfigureAwait(false);
            await _ch.DeleteAsync().ConfigureAwait(false);
        }
        internal class TaflBoard
        {
            private readonly TaflService _parent;
            private readonly int Size;
            private readonly bool WinOnCorner;
            private List<List<Tile>> Tiles;
            private Point KingPosition;
            private bool KingWin;
            private bool KingCaptured;
            private bool StrongKing;
            private bool KingRestricted;
            private bool OpponentNoMoves;

            internal TaflBoard(TaflConfiguration taflConfiguration, TaflService parent)
            {
                _parent = parent;
                Size = taflConfiguration.Size;
                WinOnCorner = taflConfiguration.WinOnCorner;
                StrongKing = taflConfiguration.StrongKing;
                Tiles = new List<List<Tile>>();
                for (var i = 0; i < Size; ++i)
                {
                    var row = new List<Tile>();
                    for (var j = 0; j < Size; ++j)
                    {
                        row.Add(new Tile());
                    }
                    Tiles.Add(row);
                }
                Tiles[Size / 2][Size / 2].Restricted = true;
                Tiles[Size / 2][Size / 2].Piece = Piece.King;
                KingPosition = new Point(Size / 2, Size / 2);
                if (taflConfiguration.CornerRestricted)
                {
                    Tiles[0][0].Restricted = true;
                    Tiles[0][Size - 1].Restricted = true;
                    Tiles[Size - 1][0].Restricted = true;
                    Tiles[Size - 1][Size - 1].Restricted = true;
                }
                var blackPositions = taflConfiguration.BlackPositions;
                foreach (var blackPosition in blackPositions)
                {
                    Tiles[blackPosition.Y][blackPosition.X].Piece = Piece.Black;
                }
                var whitePositions = taflConfiguration.WhitePositions;
                foreach (var whitePosition in whitePositions)
                {
                    Tiles[whitePosition.Y][whitePosition.X].Piece = Piece.White;
                }
            }
            internal List<List<Tile>> GetTiles()
            {
                return Tiles;
            }
            internal void TryMove(Point piece, Point location)
            {
                if(piece == location)
                {
                    throw new InvalidMoveException();
                }
                var toMove = Tiles[piece.Y][piece.X];
                if((_parent.CurrentPlayer == Piece.Black && toMove.Piece == Piece.Black) ||
                    (_parent.CurrentPlayer == Piece.White && (toMove.Piece == Piece.White || toMove.Piece == Piece.King)))
                {
                    if(piece.X != location.X && piece.Y != location.Y)
                    {
                        throw new InvalidMoveException();
                    }
                    if(piece.X == location.X) // move vertical
                    {
                        var i = piece.Y;
                        while(i != location.Y)
                        {
                            _ = location.Y > i ? ++i : --i;
                            if (Tiles[i][piece.X].Piece != Piece.Empty)
                            {
                                throw new InvalidMoveException();
                            }
                        }
                        if(Tiles[i][piece.X].Restricted == true && toMove.Piece != Piece.King)
                        {
                            throw new InvalidMoveException();
                        }
                    } else // move horizontal
                    {
                        var i = piece.X;
                        while (i != location.X)
                        {
                            _ = location.X > i ? ++i : --i;
                            if (Tiles[piece.Y][i].Piece != Piece.Empty)
                            {
                                throw new InvalidMoveException();
                            }
                        }
                        if (Tiles[piece.Y][i].Restricted == true && toMove.Piece != Piece.King)
                        {
                            throw new InvalidMoveException();
                        }
                    }
                    Tiles[location.Y][location.X].Piece = toMove.Piece;
                    if (toMove.Piece == Piece.King) // update king location
                    {
                        KingPosition.X = location.X;
                        KingPosition.Y = location.Y;
                        // check if king is on or next to throne
                        if (Tiles[Size / 2][Size / 2].Piece == Piece.King ||
                            Tiles[Size / 2 - 1][Size / 2].Piece == Piece.King ||
                            Tiles[Size / 2][Size / 2 - 1].Piece == Piece.King ||
                            Tiles[Size / 2 + 1][Size / 2].Piece == Piece.King ||
                            Tiles[Size / 2][Size / 2 + 1].Piece == Piece.King)
                        {
                            KingRestricted = true;
                        } else
                        {
                            KingRestricted = false;
                        }
                    }
                    toMove.Piece = Piece.Empty;
                } else
                {
                    throw new InvalidPieceException();
                }
                Capture(location);
                CheckKingWin();
                CheckMoves();
            }
            internal bool HasWin()
            {
                if(KingWin || KingCaptured || OpponentNoMoves)
                {
                    return true;
                }
                return false;
            }
            internal void CheckKingWin()
            {
                if((!WinOnCorner && (KingPosition.X == 0 || KingPosition.X == Size - 1 || KingPosition.Y == 0 || KingPosition.Y == Size - 1)) ||
                    (KingPosition.X == 0 && KingPosition.Y == 0) ||
                    (KingPosition.X == Size - 1 && KingPosition.Y == 0) ||
                    (KingPosition.X == 0 && KingPosition.Y == Size - 1) ||
                    (KingPosition.X == Size - 1 && KingPosition.Y == Size - 1))
                {
                    KingWin = true;
                }
            }
            internal void Capture(Point pieceLoc)
            {
                // up
                if (pieceLoc.Y > 1) // cannot capture if piece is not at least 2 squares away from edge
                {
                    var otherLoc = new Point(pieceLoc.X, pieceLoc.Y - 1);
                    CheckPieceCapture(otherLoc, true);
                }
                // left
                if (pieceLoc.X > 1)
                {
                    var otherLoc = new Point(pieceLoc.X - 1, pieceLoc.Y);
                    CheckPieceCapture(otherLoc, false);
                }
                // right
                if (pieceLoc.X < Size - 2)
                {
                    var otherLoc = new Point(pieceLoc.X + 1, pieceLoc.Y);
                    CheckPieceCapture(otherLoc, false);
                }
                // down
                if (pieceLoc.Y < Size - 2)
                {
                    var otherLoc = new Point(pieceLoc.X, pieceLoc.Y + 1);
                    CheckPieceCapture(otherLoc, true);
                }
            }
            internal void CheckPieceCapture(Point pieceLoc, bool isVertical, bool strongKingSecondCheck = false)
            {
                var tile = Tiles[pieceLoc.Y][pieceLoc.X];
                if (tile.Piece == Piece.Empty)
                {
                    return;
                }
                if (isVertical)
                {
                    if(pieceLoc.Y > 0 && pieceLoc.Y < Size - 1)
                    {
                        var upTile = Tiles[pieceLoc.Y - 1][pieceLoc.X];
                        var downTile = Tiles[pieceLoc.Y + 1][pieceLoc.X];
                        if (IsDifferentColor(tile, upTile) && IsDifferentColor(tile, downTile))
                        {
                            if((StrongKing || KingRestricted) && tile.Piece == Piece.King && !strongKingSecondCheck)
                            {
                                CheckPieceCapture(pieceLoc, !isVertical, true);
                            } else
                            {
                                if(tile.Piece == Piece.King)
                                {
                                    KingCaptured = true;
                                }
                                tile.Piece = Piece.Empty;
                            }
                        }
                    }
                } else
                {
                    if (pieceLoc.X > 0 && pieceLoc.X < Size - 1)
                    {
                        var leftTile = Tiles[pieceLoc.Y][pieceLoc.X - 1];
                        var rightTile = Tiles[pieceLoc.Y][pieceLoc.X + 1];
                        if (IsDifferentColor(tile, leftTile) && IsDifferentColor(tile, rightTile))
                        {
                            if ((StrongKing || KingRestricted) && tile.Piece == Piece.King && !strongKingSecondCheck)
                            {
                                CheckPieceCapture(pieceLoc, !isVertical, true);
                            }
                            else
                            {
                                if (tile.Piece == Piece.King)
                                {
                                    KingCaptured = true;
                                }
                                tile.Piece = Piece.Empty;
                            }
                        }
                    }
                }
            }
            internal Piece GetColor(Tile tile)
            {
                if (tile.Piece == Piece.White || tile.Piece == Piece.King)
                {
                    return Piece.White;
                }
                else
                {
                    return tile.Piece;
                }
            }
            internal bool IsDifferentColor(Tile tile, Tile otherTile)
            {
                if ((GetColor(tile) != GetColor(otherTile) && tile.Piece != Piece.Empty && otherTile.Piece != Piece.Empty) ||
                    (GetColor(tile) == Piece.Empty && tile.Restricted) || // also checks if restricted for capture
                    (GetColor(otherTile) == Piece.Empty && otherTile.Restricted))
                {
                    return true;
                }
                return false;
            }
            internal bool IsDifferentColorByPiece(Piece tile, Piece otherTile)
            {
                if (((tile == Piece.White || tile == Piece.King) && otherTile == Piece.Black) ||
                    (tile == Piece.Black && (otherTile == Piece.White || otherTile == Piece.King)))
                {
                    return true;
                }
                return false;
            }
            internal void CheckMoves()
            {
                for(var y = 0; y < Size; ++y)
                {
                    for(var x = 0; x < Size; ++x)
                    {
                        var tile = Tiles[y][x];
                        if (IsDifferentColorByPiece(_parent.CurrentPlayer, tile.Piece))
                        {
                            if (x > 0)
                            {
                                var behindTile = Tiles[y][x - 1];
                                if (behindTile.Piece == Piece.Empty && (tile.Piece == Piece.King || !behindTile.Restricted))
                                {
                                    return;
                                }
                            }
                            if (x < Size - 1)
                            {
                                var behindTile = Tiles[y][x + 1];
                                if (behindTile.Piece == Piece.Empty && (tile.Piece == Piece.King || !behindTile.Restricted))
                                {
                                    return;
                                }
                            }
                            if (y > 0)
                            {
                                var behindTile = Tiles[y - 1][x];
                                if (behindTile.Piece == Piece.Empty && (tile.Piece == Piece.King || !behindTile.Restricted))
                                {
                                    return;
                                }
                            }
                            if (y < Size - 1)
                            {
                                var behindTile = Tiles[y + 1][x];
                                if (behindTile.Piece == Piece.Empty && (tile.Piece == Piece.King || !behindTile.Restricted))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                OpponentNoMoves = true;
            }

            internal enum Piece
            {
                Empty,
                White,
                Black,
                King
            }
            internal class Tile
            {
                internal bool Restricted = false;
                internal Piece Piece = Piece.Empty;
            }
        }
    }
}
