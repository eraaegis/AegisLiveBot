using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Fun;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands.Fun
{
    public class TaflCommands : BaseCommandModule
    {
        private readonly DbService _db;

        public TaflCommands(DbService db)
        {
            _db = db;
        }

        private TaflService TaflService;
        [Command("tafl")]
        public async Task Tafl(CommandContext ctx, DiscordUser otherUser)
        {
            TaflService = new TaflService();
            ctx.Message.DeleteAfter(3);
        }

        [Command("drawtafl")]
        public async Task DrawTafl(CommandContext ctx)
        {
            TaflService.Draw();
        }
    }
}
