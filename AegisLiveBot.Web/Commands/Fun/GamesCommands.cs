using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands.Fun
{
    public class GamesCommands : BaseCommandModule
    {
        private readonly DbService _db;

        public GamesCommands(DbService db)
        {
            _db = db;
        }
        [Command("setgamescategory")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetGamesCategory(CommandContext ctx, DiscordChannel cat = null)
        {
            ctx.Message.DeleteAfter(3);
            if (!cat.IsCategory)
            {
                await ctx.Channel.SendMessageAsync($"{cat.Mention} is not a category!").ConfigureAwait(false);
                return;
            }
            using (var uow = _db.UnitOfWork())
            {
                var catId = cat != null ? cat.Id : 0;
                uow.ServerSettings.SetGamesCategory(ctx.Guild.Id, catId);
                await uow.SaveAsync().ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"Games category has been set to {cat.Mention}").ConfigureAwait(false);
            }
        }
    }
}