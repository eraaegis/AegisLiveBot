using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Fun;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.IO;
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

        [Command("tafl")]
        public async Task Tafl(CommandContext ctx, DiscordMember otherUser)
        {
            ctx.Message.DeleteAfter(3);
            var interactivity = ctx.Client.GetInteractivity();
            await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} has challenged {otherUser.Mention} to a Tafl game!").ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync($"Type \"accept\" to d-d-duel.").ConfigureAwait(false);
            var tries = 3;
            while (true)
            {
                var response = await interactivity.WaitForMessageAsync(x => x.Author.Id == otherUser.Id && x.ChannelId == ctx.Channel.Id).ConfigureAwait(false);
                if (response.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"The game was not accepted.").ConfigureAwait(false);
                    return;
                }
                var responseMsg = response.Result.Content.ToLower();
                response.Result.DeleteAfter(3);
                if (responseMsg == "accept")
                {
                    break;
                }
                else
                {
                    --tries;
                    if (tries == 0)
                    {
                        await ctx.Channel.SendMessageAsync($"The game was not accepted.").ConfigureAwait(false);
                        return;
                    }
                }
            }
            try
            {
                using (var uow = _db.UnitOfWork())
                {
                    var serverSetting = uow.ServerSettings.GetOrAddByGuildId(ctx.Guild.Id);
                    var catId = serverSetting.GamesCategory;
                    var cat = ctx.Guild.GetChannel(catId);
                    var tempName = Path.GetRandomFileName();
                    var chName = $"Tafl Game {tempName}";
                    var chs = await ctx.Guild.GetChannelsAsync().ConfigureAwait(false);
                    while (chs.FirstOrDefault(x => x.Name == chName) != null)
                    {
                        tempName = Path.GetRandomFileName();
                        chName = $"Tafl Game {tempName}";
                    }
                    var ch = await ctx.Guild.CreateChannelAsync(chName, ChannelType.Text, cat).ConfigureAwait(false);
                    await ch.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages).ConfigureAwait(false);
                    await ch.AddOverwriteAsync(ctx.Member, Permissions.SendMessages, Permissions.None).ConfigureAwait(false);
                    await ch.AddOverwriteAsync(otherUser, Permissions.SendMessages, Permissions.None).ConfigureAwait(false);
                    var taflService = await TaflService.CreateTaflService(ch, ctx.Member, otherUser, ctx.Client, tempName).ConfigureAwait(false);
                    taflService.Start();
                }
            }
            catch (Exception e)
            {
                await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
            }
        }
    }
}