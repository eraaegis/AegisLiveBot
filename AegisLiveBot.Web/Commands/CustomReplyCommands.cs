using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.CustomCrawler;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands
{
    public class CustomReplyCommands : BaseCommandModule
    {
        private readonly DbService _db;
        private readonly ICustomCrawlerService _service;

        public CustomReplyCommands(DbService db, ICustomCrawlerService service)
        {
            _db = db;
            _service = service;
        }

        [Command("togglecustomreply")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ToggleCustomReply(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var result = uow.ServerSettings.ToggleCustomReply(ctx.Guild.Id);
                await uow.SaveAsync().ConfigureAwait(false);
                var msg = result ? "on" : "off";
                await ctx.Channel.SendMessageAsync($"Custom replies is now {msg}.");
            }
            ctx.Message.DeleteAfter(3);
        }

        [Command("customreplyeditor")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task CustomReplyEditor(CommandContext ctx)
        {
            try
            {
                _service.SetUpCustomReplyEditor(ctx.Channel, ctx.User.Id);
            }
            catch (Exception e)
            {
                await ctx.Channel.SendMessageAsync($"Some error occured: {e.Message}").ConfigureAwait(false);
                AegisLog.Log(e.Message, e);
            }
        }
    }
}
