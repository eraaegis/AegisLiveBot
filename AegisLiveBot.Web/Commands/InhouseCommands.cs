using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Inhouse;
using AegisLiveBot.DAL.Models.Inhouse;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands
{
    public class InhouseCommands : BaseCommandModule
    {
        private readonly DbService _db;
        private readonly IInhouseService _service;

        public InhouseCommands(DbService db, IInhouseService service)
        {
            _db = db;
            _service = service;
        }

        [Command("setqueuech")]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task SetQueueChannel(CommandContext ctx, DiscordChannel channel = null)
        {
            if(channel == null)
            {
                await ctx.Channel.SendMessageAsync("Please specify channel.").ConfigureAwait(false);
                return;
            }

            await _service.SetQueueChannel(channel).ConfigureAwait(false);
        }

        [Command("unsetqueuech")]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task UnsetQueueChannel(CommandContext ctx, DiscordChannel channel = null)
        {
            if (channel == null)
            {
                await ctx.Channel.SendMessageAsync("Please specify channel.").ConfigureAwait(false);
                return;
            }

            await _service.UnsetQueueChannel(channel).ConfigureAwait(false);
        }

        [Command("resetqueuech")]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task ResetQueueChannel(CommandContext ctx, DiscordChannel channel = null)
        {
            if (channel == null)
            {
                await ctx.Channel.SendMessageAsync("Please specify channel.").ConfigureAwait(false);
                return;
            }

            await _service.ResetQueueChannel(channel).ConfigureAwait(false);
        }

        [Command("queue")]
        public async Task ShowQueue(CommandContext ctx, string role = "")
        {
            if (role.ToLower() == "top")
            {
                await _service.QueueUp(ctx.Channel, ctx.Member, PlayerRole.Top).ConfigureAwait(false);
            }
            else if (role.ToLower() == "jgl" || role.ToLower() == "jg" || role.ToLower() == "jungle" || role.ToLower() == "jng")
            {
                await _service.QueueUp(ctx.Channel, ctx.Member, PlayerRole.Jgl).ConfigureAwait(false);
            }
            else if (role.ToLower() == "mid")
            {
                await _service.QueueUp(ctx.Channel, ctx.Member, PlayerRole.Mid).ConfigureAwait(false);
            }
            else if (role.ToLower() == "bot" || role.ToLower() == "adc")
            {
                await _service.QueueUp(ctx.Channel, ctx.Member, PlayerRole.Bot).ConfigureAwait(false);
            }
            else if (role.ToLower() == "sup" || role.ToLower() == "support")
            {
                await _service.QueueUp(ctx.Channel, ctx.Member, PlayerRole.Sup).ConfigureAwait(false);
            }
            else if (role.ToLower() == "fill")
            {
                await _service.QueueUp(ctx.Channel, ctx.Member, PlayerRole.Fill).ConfigureAwait(false);
            }
            else
            {
                await _service.ShowQueue(ctx.Channel).ConfigureAwait(false);
            }
        }

        [Command("leave")]
        public async Task Unqueue(CommandContext ctx)
        {
            await _service.Unqueue(ctx.Channel, ctx.Member).ConfigureAwait(false);
        }

        [Command("won")]
        public async Task ConfirmWin(CommandContext ctx)
        {
            await _service.ConfirmWin(ctx.Channel, ctx.Member).ConfigureAwait(false);
        }
    }
}
