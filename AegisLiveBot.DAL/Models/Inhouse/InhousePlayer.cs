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

        public InhousePlayer(DiscordMember player, PlayerSide playerSide, PlayerRole playerRole)
        {
            Player = player;
            PlayerStatus = PlayerStatus.None;
            PlayerSide = playerSide;
            PlayerRole = playerRole;
            PlayerConfirm = PlayerConfirm.None;
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
        Top,
        Jgl,
        Mid,
        Bot,
        Sup
    }

    public enum PlayerConfirm
    {
        None,
        Accept,
        Deny
    }
}
