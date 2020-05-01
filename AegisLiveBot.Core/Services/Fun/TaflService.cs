using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace AegisLiveBot.Core.Services.Fun
{
    public enum Piece
    {
        Empty,
        White,
        Black,
        King
    }
    public class Tile
    {
        public bool Restricted = false;
        public Piece Piece = Piece.Empty;
    }
    public class Board
    {
        private List<List<Tile>> Tiles;

        public Board(ITaflConfiguration taflConfiguration)
        {
            var size = taflConfiguration.GetSize();
            Tiles = new List<List<Tile>>();
            for(var i = 0; i < size; ++i)
            {
                var row = new List<Tile>();
                for (var j = 0; j < size; ++j)
                {
                    row.Add(new Tile());
                }
                Tiles.Add(row);
            }
            Tiles[size / 2][size / 2].Restricted = true;
            Tiles[size / 2][size / 2].Piece = Piece.King;
            if (taflConfiguration.IsCornerRestricted())
            {
                Tiles[0][0].Restricted = true;
                Tiles[0][size - 1].Restricted = true;
                Tiles[size - 1][0].Restricted = true;
                Tiles[size - 1][size - 1].Restricted = true;
            }
            var blackPositions = taflConfiguration.GetBlackPositions();
            foreach(var blackPosition in blackPositions)
            {
                Tiles[blackPosition.X][blackPosition.Y].Piece = Piece.Black;
            }
            var whitePositions = taflConfiguration.GetWhitePositions();
            foreach(var whitePosition in whitePositions)
            {
                Tiles[whitePosition.X][whitePosition.Y].Piece = Piece.White;
            }
        }
        public List<List<Tile>> GetTiles()
        {
            return Tiles;
        }
    }
    public interface ITaflConfiguration
    {
        int GetSize();
        List<Point> GetBlackPositions();
        List<Point> GetWhitePositions();
        bool IsStrongKing();
        bool IsWinOnCorner();
        bool IsCornerRestricted();
    }
    public class SaamiTablut : ITaflConfiguration
    {
        public int GetSize()
        {
            return 9;
        }
        public List<Point> GetBlackPositions()
        {
            return new List<Point> { new Point(3, 0), new Point(4, 0), new Point(5, 0), new Point(4, 1),
            new Point(0, 3), new Point(0, 4), new Point(0, 5), new Point(1, 4),
            new Point(8, 3), new Point(8, 4), new Point(8, 5), new Point(7, 4),
            new Point(3, 8), new Point(4, 8), new Point(5, 8), new Point(4, 7)};
        }
        public List<Point> GetWhitePositions()
        {
            return new List<Point> { new Point(4, 2), new Point(4, 3), new Point(2, 4), new Point(3, 4),
            new Point(5, 4), new Point(6, 4), new Point(4, 5), new Point(4, 6)};
        }
        public bool IsStrongKing()
        {
            return false;
        }
        public bool IsWinOnCorner()
        {
            return false;
        }
        public bool IsCornerRestricted()
        {
            return false;
        }
    }
    public interface ITaflService
    {
    }
    public class TaflService : ITaflService
    {
        private readonly ITaflConfiguration TaflConfiguration;
        private Board Board;
        private readonly FontFamily _fontFamily;
        private readonly Font _font;
        private readonly SolidBrush _solidBrush;
        private readonly Image _background;
        private readonly Image _boardIndex;
        private readonly Image _tile;
        private readonly Image _tileDark;
        private readonly Image _king;
        private readonly Image _white;
        private readonly Image _black;
        private const int _tileSize = 64;

        public TaflService()
        {
            TaflConfiguration = new SaamiTablut();
            Board = new Board(TaflConfiguration);
            _fontFamily = new FontFamily("Arial");
            _font = new Font(_fontFamily, 16, FontStyle.Bold, GraphicsUnit.Pixel);
            _solidBrush = new SolidBrush(Color.DarkGray);

            var imageFolderPath = $"../AegisLiveBot.DAL/Images/Tafl";
            _tile = Image.FromFile(Path.Combine(imageFolderPath, "tile.jpg"));
            _tileDark = Image.FromFile(Path.Combine(imageFolderPath, "tile_dark.jpg"));
            _king = Image.FromFile(Path.Combine(imageFolderPath, "king.png"));
            _white = Image.FromFile(Path.Combine(imageFolderPath, "pawn_white.png"));
            _black = Image.FromFile(Path.Combine(imageFolderPath, "pawn_black.png"));
            _background = DrawBoard();
            _boardIndex = DrawBoardIndex();
        }
        private Image DrawBoard()
        {
            var size = TaflConfiguration.GetSize();
            var boardImage = new Bitmap(size * _tileSize, size * _tileSize);
            using (Graphics g = Graphics.FromImage(boardImage))
            {
                for(var i = 0; i < size; ++i)
                {
                    for(var j = 0; j < size; ++j)
                    {
                        g.DrawImage(_tile, new Point(i * _tileSize, j * _tileSize));
                    }
                }
                g.DrawImage(_tileDark, new Point((size / 2) * _tileSize, (size / 2) * _tileSize));
                if (TaflConfiguration.IsCornerRestricted())
                {
                    g.DrawImage(_tileDark, new Point(0, 0));
                    g.DrawImage(_tileDark, new Point(0, (size - 1) * _tileSize));
                    g.DrawImage(_tileDark, new Point((size - 1) * _tileSize, 0));
                    g.DrawImage(_tileDark, new Point((size - 1) * _tileSize, (size - 1) * _tileSize));
                }
                boardImage.Save(Path.Combine(AppContext.BaseDirectory, "Images/Tafl/background.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            return Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Images/Tafl/background.jpg"));
        }
        private Image DrawBoardIndex()
        {
            var size = TaflConfiguration.GetSize();
            var boardIndex = new Bitmap(size * _tileSize, size * _tileSize);
            boardIndex.MakeTransparent();
            using (Graphics g = Graphics.FromImage(boardIndex))
            {
                for (var i = 0; i < size; ++i)
                {
                    g.DrawString(i.ToString(), _font, _solidBrush, new Point(0, 24 + (size - i - 1) * _tileSize));
                    g.DrawString(((char)('a' + i)).ToString(), _font, _solidBrush, new Point(24 + i * _tileSize, 44 + (size - 1) * _tileSize));
                }
                boardIndex.Save(Path.Combine(AppContext.BaseDirectory, "Images/Tafl/boardIndex.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            return Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Images/Tafl/boardIndex.png"));
        }
        public void Draw()
        {
            var size = TaflConfiguration.GetSize();
            var boardImage = new Bitmap(size * _tileSize, size * _tileSize);
            var tiles = Board.GetTiles();
            using(Graphics g = Graphics.FromImage(boardImage))
            {
                g.DrawImage(_background, new Point(0, 0));
                for (int y = 0; y < size; ++y)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        if(tiles[x][y].Piece == Piece.White)
                        {
                            g.DrawImage(_white, new Point(x * _tileSize, y * _tileSize));
                        } else if(tiles[x][y].Piece == Piece.Black)
                        {
                            g.DrawImage(_black, new Point(x * _tileSize, y * _tileSize));
                        } else if(tiles[x][y].Piece == Piece.King)
                        {
                            g.DrawImage(_king, new Point(x * _tileSize, y * _tileSize));
                        }
                    }
                }
                g.DrawImage(_boardIndex, new Point(0, 0));
                var tempPath = Path.Combine(AppContext.BaseDirectory, "Images/Tafl");
                Directory.CreateDirectory(tempPath);
                boardImage.Save(Path.Combine(tempPath, "temp.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }
    }
}
