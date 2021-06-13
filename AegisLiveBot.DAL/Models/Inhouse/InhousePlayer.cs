using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Models.Inhouse
{
    public class InhousePlayer
    {
        public DiscordMember Player { get; set; }
        public PlayerStatus PlayerStatus { get; set; }
        public PlayerSide PlayerSide { get; set; }
        public PlayerRole PlayerRole { get; set; }
        public PlayerConfirm PlayerConfirm { get; set; }

        public Dictionary<PlayerRole, bool> QueuedRoles { get; set; }

        public InhousePlayer(DiscordMember player)
        {
            Player = player;
            PlayerStatus = PlayerStatus.None;
            PlayerConfirm = PlayerConfirm.None;
            QueuedRoles = new Dictionary<PlayerRole, bool>();
            QueuedRoles.Add(PlayerRole.Top, false);
            QueuedRoles.Add(PlayerRole.Jgl, false);
            QueuedRoles.Add(PlayerRole.Mid, false);
            QueuedRoles.Add(PlayerRole.Bot, false);
            QueuedRoles.Add(PlayerRole.Sup, false);
            QueuedRoles.Add(PlayerRole.Fill, false);
        }
    }

    public enum PlayerStatus
    {
        None,
        Ready,
        NotReady
    }

    public enum PlayerSide
    {
        Blue,
        Red
    }

    public enum PlayerRole
    {
        Top = 1,
        Jgl = 2,
        Mid = 4,
        Bot = 8,
        Sup = 16,
        Fill = 32
    }

    public enum PlayerConfirm
    {
        None,
        Accept,
        Deny
    }
}
