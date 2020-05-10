using AegisLiveBot.Core.Common;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Services.Fun
{
    public class OldMaidImage
    {
        private static readonly Lazy<OldMaidImage> lazy = new Lazy<OldMaidImage>(() => new OldMaidImage());
        private readonly string _imagePath;
        public readonly Image _back;
        public readonly Image _black;
        public readonly Image _blue;
        public readonly Image _cyan;
        public readonly Image _green;
        public readonly Image _orange;
        public readonly Image _pink;
        public readonly Image _purple;
        public readonly Image _red;
        public readonly Image _yellow;
        public readonly Image _blackSmall;
        public readonly Image _blueSmall;
        public readonly Image _cyanSmall;
        public readonly Image _greenSmall;
        public readonly Image _orangeSmall;
        public readonly Image _pinkSmall;
        public readonly Image _purpleSmall;
        public readonly Image _redSmall;
        public readonly Image _yellowSmall;

        private OldMaidImage()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Images/OldMaid");
            _imagePath = path;
            _back = Image.FromFile(Path.Combine(path, "back.png"));
            _black = Image.FromFile(Path.Combine(path, "black.png"));
            _blue = Image.FromFile(Path.Combine(path, "blue.png"));
            _cyan = Image.FromFile(Path.Combine(path, "cyan.png"));
            _green = Image.FromFile(Path.Combine(path, "green.png"));
            _orange = Image.FromFile(Path.Combine(path, "orange.png"));
            _pink = Image.FromFile(Path.Combine(path, "pink.png"));
            _purple = Image.FromFile(Path.Combine(path, "purple.png"));
            _red = Image.FromFile(Path.Combine(path, "red.png"));
            _yellow = Image.FromFile(Path.Combine(path, "yellow.png"));
            _blackSmall = Image.FromFile(Path.Combine(path, "black_small.png"));
            _blueSmall = Image.FromFile(Path.Combine(path, "blue_small.png"));
            _cyanSmall = Image.FromFile(Path.Combine(path, "cyan_small.png"));
            _greenSmall = Image.FromFile(Path.Combine(path, "green_small.png"));
            _orangeSmall = Image.FromFile(Path.Combine(path, "orange_small.png"));
            _pinkSmall = Image.FromFile(Path.Combine(path, "pink_small.png"));
            _purpleSmall = Image.FromFile(Path.Combine(path, "purple_small.png"));
            _redSmall = Image.FromFile(Path.Combine(path, "red_small.png"));
            _yellowSmall = Image.FromFile(Path.Combine(path, "yellow_small.png"));
        }
        public static OldMaidImage Instance { get { return lazy.Value; } }
    }
    public class OldMaidService : IGameService
    {
        private const int _size = 1200;
        private const int _cardWidth = 315;
        private const int _cardHeight = 440;
        private const int _textOffset = 40;
        private const int _secondsToDelete = 60;

        private readonly string _tempPath;

        private readonly DiscordChannel _ch;
        private readonly DiscordClient _client;
        private bool IsDisposed = false;
        private readonly object DisposeLock = new object();

        private List<OldMaidPlayer> Players;
        private int Turn = 0;
        private readonly object TurnLock = new object();

        private readonly object _drawLock = new object();

        private static readonly Font _font = new Font("Arial", 48, FontStyle.Bold);
        private static readonly SolidBrush _solidBrush = new SolidBrush(Color.White);
        private static readonly StringFormat _centerBold = new StringFormat();

        private bool HasMatch = false;
        private int MatchIndex = 0;
        private CardColor PreviousCard;
        private OldMaidPlayer MatchPlayer;

        private bool HasWon = false;
        private int WonPlayer = 0;

        public OldMaidService(DiscordChannel ch, DiscordMember p1, DiscordMember p2, DiscordClient client, string tempName)
        {
            _tempPath = Path.Combine(AppContext.BaseDirectory, "Temp/Images/OldMaid", tempName);
            Directory.CreateDirectory(_tempPath);
            _ch = ch;

            _centerBold.Alignment = StringAlignment.Center;
            _centerBold.LineAlignment = StringAlignment.Center;

            Players = new List<OldMaidPlayer>();
            if (AegisRandom.RandomBool())
            {
                Players.Add(new OldMaidPlayer(this, p1, 0));
                Players.Add(new OldMaidPlayer(this, p2, 1));
            } else
            {
                Players.Add(new OldMaidPlayer(this, p2, 0));
                Players.Add(new OldMaidPlayer(this, p1, 1));
            }
            MatchPlayer = Players[0];
            var cards = new List<CardColor>
            {
                CardColor.BLACK,
                CardColor.BLACK,
                CardColor.BLUE,
                CardColor.BLUE,
                CardColor.CYAN,
                CardColor.CYAN,
                CardColor.GREEN,
                CardColor.GREEN,
                CardColor.ORANGE,
                CardColor.ORANGE,
                CardColor.PINK,
                CardColor.PINK,
                CardColor.PURPLE,
                CardColor.PURPLE,
                CardColor.RED,
                CardColor.RED,
                CardColor.YELLOW,
                CardColor.YELLOW
            };
            var indexToRemove = AegisRandom.RandomNumber(0, cards.Count - 1);
            cards.RemoveAt(indexToRemove);
            for(var i = 0; i < cards.Count; ++i)
            {
                var playerToInsert = i % Players.Count;
                Players[playerToInsert].AddCard(cards[i]);
            }
            Turn = Players.Count - 1;
            _client = client;
        }
        public void Start()
        {
            Task.Run(async () =>
            {
                await _ch.AddOverwriteAsync(_ch.Guild.EveryoneRole, Permissions.SendMessages, Permissions.None).ConfigureAwait(false);

                var initMsg = $"An Old Maid game has started between {Players[0].Player.DisplayName} and {Players[1].Player.DisplayName}!\n";
                initMsg += $"{Players[Players.Count - 1].Player.DisplayName} goes first!\n";
                try
                {
                    await _ch.SendMessageAsync(initMsg).ConfigureAwait(false);
                    await Players[0].Start().ConfigureAwait(false);
                    await Players[1].Start().ConfigureAwait(false);
                    await ShowAll().ConfigureAwait(false);
                } catch(Exception e)
                {
                    await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
                    await Dispose().ConfigureAwait(false);
                }
            });
        }
        private async Task ShowAll(bool asyncShow = true)
        {
            try
            {
                if (asyncShow)
                {
                    _ = Task.Run(async () => { await Players[0].Show().ConfigureAwait(false); });
                    _ = Task.Run(async () => { await Players[1].Show().ConfigureAwait(false); });
                    await Show().ConfigureAwait(false);
                } else
                {
                    await Players[0].Show().ConfigureAwait(false);
                    await Players[1].Show().ConfigureAwait(false);
                    await Show().ConfigureAwait(false);
                }
            } catch(Exception e)
            {
                await _ch.SendMessageAsync(e.Message).ConfigureAwait(false);
            }
        }
        private async Task Take(int index, int victimIndex, int playerIndex)
        {
            if(Players.Count <= victimIndex || Players[victimIndex].GetCardCount() <= index)
            {
                throw new InvalidCardException();
            }
            lock (TurnLock)
            {
                if (Turn != playerIndex)
                {
                    throw new NotYourTurnException();
                }
                ++Turn;
                if (Turn >= Players.Count)
                {
                    Turn = 0;
                }
                var card = Players[victimIndex].RemoveCard(index);
                Players[playerIndex].AddCard(card);
                PreviousCard = card;
            }
            if (HasWon)
            {
                await ShowAll(false).ConfigureAwait(false);
                await Dispose().ConfigureAwait(false);
            } else
            {
                await ShowAll().ConfigureAwait(false);
            }
        }
        private async Task Show()
        {
            var imagePath = Path.Combine(_tempPath, "main.png");
            var image = new Bitmap(_size, _size);
            lock (_drawLock)
            {
                using (Graphics g = Graphics.FromImage(image))
                {
                    var back = new Bitmap(OldMaidImage.Instance._back);

                    var enemyY = 0;
                    var player1Cards = Players[0].GetCardCount();
                    // if players cards total width is less than size, draw from center
                    if (player1Cards * _cardWidth < _size)
                    {
                        var startingOffset = (_size - player1Cards * _cardWidth) / 2;
                        for(var i = 0; i < player1Cards; ++i)
                        {
                            g.DrawImage(back, new Point(startingOffset + i * _cardWidth, enemyY));
                        }
                    } else
                    {
                        var enemyXOffset = (_size - _cardWidth) / (player1Cards - 1);
                        for (var i = 0; i < player1Cards; ++i)
                        {
                            g.DrawImage(back, new Point(enemyXOffset * i, enemyY));
                        }
                    }

                    var myY = _size - _cardHeight;
                    var player2Cards = Players[1].GetCardCount();
                    if (player2Cards * _cardWidth < _size)
                    {
                        var startingOffset = (_size - player2Cards * _cardWidth) / 2;
                        for (var i = 0; i < player2Cards; ++i)
                        {
                            g.DrawImage(back, new Point(startingOffset + i * _cardWidth, myY));
                        }
                    }
                    else
                    {
                        var myXOffset = (_size - _cardWidth) / (player2Cards - 1);
                        for (var i = 0; i < player2Cards; ++i)
                        {
                            g.DrawImage(back, new Point(myXOffset * i, myY));
                        }
                    }
                    g.DrawString($"{Players[0].Player.DisplayName}", _font, _solidBrush, new PointF(_size/2, _cardHeight + _textOffset), _centerBold);
                    g.DrawString($"{Players[1].Player.DisplayName}", _font, _solidBrush, new PointF(_size / 2, _size - _cardHeight - _textOffset), _centerBold);
                    var centerMsg = GetCenterMsg();
                    g.DrawString(centerMsg, _font, _solidBrush, new PointF(_size / 2, _size / 2), _centerBold);
                    if (HasMatch)
                    {
                        var stringSize = g.MeasureString(centerMsg, _font);
                        var cardSmallImage = GetColorSmall(PreviousCard);
                        g.DrawImage(cardSmallImage, new PointF(_size / 2 - stringSize.Width / 2 - cardSmallImage.Width, _size / 2 - cardSmallImage.Height / 2));
                        g.DrawImage(cardSmallImage, new PointF(_size / 2 + stringSize.Width / 2, _size / 2 - cardSmallImage.Height / 2));
                    }
                }
            }
            image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
            await _ch.SendFileAsync(imagePath).ConfigureAwait(false);
        }
        private string GetCenterMsg(int index = -1)
        {
            // if won game
            if (HasWon)
            {
                if(index == WonPlayer)
                {
                    return "You have won the game!";
                }
                return $"{MatchPlayer.Player.DisplayName} has won the game!";
            }
            // if anyone got a match previously, return match
            if (HasMatch)
            {
                if(index == MatchIndex)
                {
                    return "You got a match!";
                }
                return $"{MatchPlayer.Player.DisplayName} got a match!";
            } else
            {
                if(index == Turn)
                {
                    return $"It is currently your turn.";
                }
                // else state the turn
                return $"It is currently {Players[Turn].Player.DisplayName}'s turn.";
            }

        }
        private async Task Dispose(string msg = "")
        {
            lock (DisposeLock)
            {
                if (IsDisposed)
                {
                    return;
                }
                IsDisposed = true;
            }
            foreach (var player in Players)
            {
                _ = Task.Run(async () => {
                    await player.Dispose(msg).ConfigureAwait(false);
                });
            }
            lock (_drawLock)
            {
                DirectoryInfo dir = new DirectoryInfo(_tempPath);
                foreach (FileInfo file in dir.GetFiles())
                {
                    file.Delete();
                }
                Directory.Delete(_tempPath);
            }
            var timeMsg = _secondsToDelete <= 60 ? $"{_secondsToDelete} seconds" : $"{_secondsToDelete / 60} minutes";
            msg += $"Channel will be deleted in {timeMsg}.\n";
            await _ch.SendMessageAsync(msg).ConfigureAwait(false);
            await Task.Delay(_secondsToDelete * 1000).ConfigureAwait(false);
            await _ch.DeleteAsync().ConfigureAwait(false);
        }
        internal class OldMaidPlayer
        {
            private readonly OldMaidService _parent;
            private DiscordChannel _pCh;
            private int _index;
            internal DiscordMember Player { get; private set; }
            private List<CardColor> Cards;
            private bool IsDisposed = false;

            internal OldMaidPlayer(OldMaidService parent, DiscordMember player, int index)
            {
                _parent = parent;
                Player = player;
                _index = index;
                Cards = new List<CardColor>();
            }
            internal void AddCard(CardColor card)
            {
                lock (_parent._drawLock)
                {
                    foreach(var cardInHand in Cards)
                    {
                        if(cardInHand == card)
                        {
                            Cards.Remove(cardInHand);
                            _parent.HasMatch = true;
                            _parent.MatchIndex = _index;
                            _parent.MatchPlayer = this;
                            if(Cards.Count == 0)
                            {
                                _parent.HasWon = true;
                                _parent.WonPlayer = _index;
                            }
                            return;
                        }
                    }
                    var indexToInsert = AegisRandom.RandomNumber(0, Cards.Count);
                    Cards.Insert(indexToInsert, card);
                    _parent.HasMatch = false;
                }
            }
            internal CardColor RemoveCard(int index)
            {
                lock (_parent._drawLock)
                {
                    var card = Cards[index];
                    Cards.RemoveAt(index);
                    if (Cards.Count == 0)
                    {
                        _parent.HasWon = true;
                        _parent.WonPlayer = _index;
                    }
                    return card;
                }
            }
            internal async Task Start()
            {
                var cat = _parent._ch.Parent;
                var chName = $"{_parent._ch.Name} {Player.DisplayName}";
                var guild = _parent._ch.Guild;
                _pCh = await guild.CreateChannelAsync(chName, ChannelType.Text, cat).ConfigureAwait(false);
                await _pCh.AddOverwriteAsync(guild.EveryoneRole, Permissions.None, Permissions.AccessChannels).ConfigureAwait(false);
                await _pCh.AddOverwriteAsync(Player, Permissions.AccessChannels | Permissions.SendMessages, Permissions.None).ConfigureAwait(false);
                var initMsg = $"This is your channel, {Player.Mention}\n";
                initMsg += $"Type 'help' for commands.";
                await _pCh.SendMessageAsync(initMsg).ConfigureAwait(false);

                _ = Task.Run(async () =>
                {
                    var interactivity = _parent._client.GetInteractivity();
                    while (true)
                    {
                        var response = await interactivity.WaitForMessageAsync(x => x.Author.Id == Player.Id && x.ChannelId == _pCh.Id).ConfigureAwait(false);
                        if (IsDisposed)
                        {
                            break;
                        }
                        var afk = false;
                        if (response.TimedOut)
                        {
                            await _pCh.SendMessageAsync($"The game will end soon unless there is activity.").ConfigureAwait(false);
                            if (afk)
                            {
                                var msg = $"The channel has seen no activity for 10 minutes.\n";
                                break;
                            }
                            afk = true;
                            continue;
                        }
                        afk = false;
                        var command = response.Result.Content;
                        var commandSplit = command.Split();
                        if (commandSplit.Length == 1 && command == "show")
                        {
                            await Show().ConfigureAwait(false);
                        } else if (commandSplit.Length == 1 && command == "help")
                        {
                            await _parent.Dispose($"{Player.DisplayName} has resigned the game!").ConfigureAwait(false);
                        }
                        else if (commandSplit.Length == 1 && command == "resign")
                        {
                            var helpMsg = $"To grab a card, use 'take <number>' or simply '<number>'\n";
                            helpMsg += $"To show your hand, type 'show'";
                            helpMsg += $"To resign the game, type 'resign'";
                            await _pCh.SendMessageAsync(helpMsg).ConfigureAwait(false);
                        }
                        else if (commandSplit.Length == 2 && commandSplit[0] == "take")
                        {
                            int index = 0;
                            var canParse = int.TryParse(commandSplit[1], out index);
                            try
                            {
                                if (canParse)
                                {
                                    await _parent.Take(index - 1, 1 - _index, _index).ConfigureAwait(false);
                                } else
                                {
                                    throw new InvalidCardException();
                                }
                            } catch(Exception e)
                            {
                                await _pCh.SendMessageAsync(e.Message).ConfigureAwait(false);
                            }
                        } else if(commandSplit.Length == 1)
                        {
                            int index = 0;
                            var canParse = int.TryParse(command, out index);
                            try
                            {
                                if (canParse)
                                {
                                    await _parent.Take(index - 1, 1 - _index, _index).ConfigureAwait(false);
                                }
                            }
                            catch (Exception e)
                            {
                                await _pCh.SendMessageAsync(e.Message).ConfigureAwait(false);
                            }
                        }
                        await Task.Delay(250).ConfigureAwait(false);
                    }
                    await _parent.Dispose().ConfigureAwait(false);
                });
            }
            internal int GetCardCount()
            {
                return Cards.Count;
            }
            internal async Task Show()
            {
                var imagePath = Path.Combine(_parent._tempPath, $"{_index}.png");
                var image = new Bitmap(_size, _size);
                lock (_parent._drawLock)
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        var back = new Bitmap(OldMaidImage.Instance._back);

                        var enemyY = 0;
                        var player1Cards = _parent.Players[1 - _index].GetCardCount(); ;
                        // if players cards total width is less than size, draw from center
                        for(var i = 0; i < player1Cards; ++i)
                        {
                            if (player1Cards * _cardWidth < _size)
                            {
                                var enemyStartingOffset = (_size - player1Cards * _cardWidth) / 2;
                                g.DrawImage(back, new Point(enemyStartingOffset + i * _cardWidth, enemyY));
                                g.DrawString($"{i + 1}", _font, _solidBrush, new Point(enemyStartingOffset + i * _cardWidth, enemyY + _cardHeight));
                            }
                            else
                            {
                                var enemyXOffset = (_size - _cardWidth) / (player1Cards - 1);
                                g.DrawImage(back, new Point(enemyXOffset * i, enemyY));
                                g.DrawString($"{i + 1}", _font, _solidBrush, new Point(enemyXOffset * i, enemyY + _cardHeight));
                            }
                        }

                        var myY = _size - _cardHeight;
                        for (var i = 0; i < Cards.Count; ++i)
                        {
                            var drawColor = GetColor(Cards[i]);
                            if(Cards.Count * _cardWidth < _size)
                            {
                                var myStartingOffset = (_size - Cards.Count * _cardWidth) / 2;
                                g.DrawImage(drawColor, new Point(myStartingOffset + i * _cardWidth, myY));
                            } else
                            {
                                var myXOffset = (_size - _cardWidth) / (Cards.Count - 1);
                                g.DrawImage(drawColor, new Point(myXOffset * i, myY));
                            }
                        }
                        var centerMsg = _parent.GetCenterMsg(_index);
                        g.DrawString(centerMsg, _font, _solidBrush, new PointF(_size / 2, _size / 2), _centerBold);
                        if (_parent.HasMatch)
                        {
                            var stringSize = g.MeasureString(centerMsg, _font);
                            var cardSmallImage = GetColorSmall(_parent.PreviousCard);
                            g.DrawImage(cardSmallImage, new PointF(_size / 2 - stringSize.Width / 2 - cardSmallImage.Width, _size / 2 - cardSmallImage.Height / 2));
                            g.DrawImage(cardSmallImage, new PointF(_size / 2 + stringSize.Width / 2, _size / 2 - cardSmallImage.Height / 2));
                        }
                    }
                }
                image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                await _pCh.SendFileAsync(imagePath).ConfigureAwait(false);
            }
            internal async Task Dispose(string msg = "")
            {
                if (IsDisposed)
                {
                    return;
                }
                var timeMsg = _secondsToDelete <= 60 ? $"{_secondsToDelete} seconds" : $"{_secondsToDelete / 60} minutes";
                msg += $"Channel will be deleted in {timeMsg}. \n";
                await _pCh.SendMessageAsync(msg).ConfigureAwait(false);
                await Task.Delay(_secondsToDelete * 1000).ConfigureAwait(false);
                await _pCh.DeleteAsync().ConfigureAwait(false);
                IsDisposed = true;
            }
        }
        internal enum CardColor
        {
            BLACK,
            BLUE,
            CYAN,
            GREEN,
            ORANGE,
            PINK,
            PURPLE,
            RED,
            YELLOW
        }
        private static Image GetColor(CardColor cardColor)
        {
            switch (cardColor)
            {
                case CardColor.BLUE:
                    return OldMaidImage.Instance._blue;
                case CardColor.CYAN:
                    return OldMaidImage.Instance._cyan;
                case CardColor.GREEN:
                    return OldMaidImage.Instance._green;
                case CardColor.ORANGE:
                    return OldMaidImage.Instance._orange;
                case CardColor.PINK:
                    return OldMaidImage.Instance._pink;
                case CardColor.PURPLE:
                    return OldMaidImage.Instance._purple;
                case CardColor.RED:
                    return OldMaidImage.Instance._red;
                case CardColor.YELLOW:
                    return OldMaidImage.Instance._yellow;
                default:
                    return OldMaidImage.Instance._black;
            }
        }
        private static Image GetColorSmall(CardColor cardColor)
        {
            switch (cardColor)
            {
                case CardColor.BLUE:
                    return OldMaidImage.Instance._blueSmall;
                case CardColor.CYAN:
                    return OldMaidImage.Instance._cyanSmall;
                case CardColor.GREEN:
                    return OldMaidImage.Instance._greenSmall;
                case CardColor.ORANGE:
                    return OldMaidImage.Instance._orangeSmall;
                case CardColor.PINK:
                    return OldMaidImage.Instance._pinkSmall;
                case CardColor.PURPLE:
                    return OldMaidImage.Instance._purpleSmall;
                case CardColor.RED:
                    return OldMaidImage.Instance._redSmall;
                case CardColor.YELLOW:
                    return OldMaidImage.Instance._yellowSmall;
                default:
                    return OldMaidImage.Instance._blackSmall;
            }
        }
    }
}
