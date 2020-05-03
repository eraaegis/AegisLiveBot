using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.DAL.Repository;
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
    public class RoastCommands : BaseCommandModule
    {
        private readonly DbService _db;

        public RoastCommands(DbService db)
        {
            _db = db;
        }

        [Command("addroast")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task AddRoast(CommandContext ctx, params string[] msgs)
        {
            var msg = string.Join(" ", msgs);
            if(msg[0] != '\'')
            {
                msg = ' ' + msg;
            }
            if (!char.IsPunctuation(msg.Last()))
            {
                msg += '.';
            }
            using (var uow = _db.UnitOfWork())
            {
                uow.RoastMsgs.AddByGuildId(ctx.Guild.Id, msg);
                await uow.SaveAsync().ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} added more coal to the fire.").ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("roast")]
        public async Task Roast(CommandContext ctx, DiscordUser user = null)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    var msgs = uow.RoastMsgs.GetAllByGuildId(ctx.Guild.Id);
                    var msg = msgs.ElementAt(AegisRandom.RandomNumber(0, msgs.Count()));
                    var victim = user ?? ctx.User;
                    await ctx.Channel.SendMessageAsync($"{victim.Mention}{msg.Msg}").ConfigureAwait(false);
                } catch(Exception e)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} {e.Message}").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("listroast")]
        public async Task ListRoast(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    var roastMsgs = uow.RoastMsgs.GetAllByGuildId(ctx.Guild.Id);
                    var index = 1;
                    var msg = $"";
                    foreach (var roastMsg in roastMsgs)
                    {
                        msg += $"{index}. Someone{roastMsg.Msg}\n";
                        ++index;
                    }
                    await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
                } catch(Exception e)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} {e.Message}").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("deleteroast")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task DeleteRoast(CommandContext ctx, int msgId)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    uow.RoastMsgs.RemoveByGuildIdMsgId(ctx.Guild.Id, msgId);
                    await uow.SaveAsync().ConfigureAwait(false);
                    await ctx.Channel.SendMessageAsync($"The bot decided that the roast is not fiery enough.").ConfigureAwait(false);
                } catch(ZeroOrNegativeRoastException e)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} {e.Message}").ConfigureAwait(false);
                } catch(OutOfRangeRoastException e)
                {
                    await ctx.Channel.SendMessageAsync($"{msgId} {e.Message}").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
    }
}
