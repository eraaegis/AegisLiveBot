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
                await ctx.Channel.SendMessageAsync($"{ctx.Member.DisplayName} added more coal to the fire.").ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("roast")]
        public async Task Roast(CommandContext ctx, DiscordMember user = null)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    var msgs = uow.RoastMsgs.GetAllByGuildId(ctx.Guild.Id);
                    var msg = msgs.ElementAt(AegisRandom.RandomNumber(0, msgs.Count()));
                    var victim = user ?? ctx.Member;
                    await ctx.Channel.SendMessageAsync($"{victim.DisplayName}{msg.Msg}").ConfigureAwait(false);
                } catch(Exception e)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.DisplayName} {e.Message}").ConfigureAwait(false);
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
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.DisplayName} {e.Message}").ConfigureAwait(false);
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
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.DisplayName} {e.Message}").ConfigureAwait(false);
                } catch(OutOfRangeRoastException e)
                {
                    await ctx.Channel.SendMessageAsync($"{msgId} {e.Message}").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("monday")]
        public async Task Monday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Monday).ConfigureAwait(false);
        }
        [Command("tuesday")]
        public async Task Tuesday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Tuesday).ConfigureAwait(false);
        }
        [Command("wednesday")]
        public async Task Wednesday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Wednesday).ConfigureAwait(false);
        }
        [Command("thursday")]
        public async Task Thursday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Thursday).ConfigureAwait(false);
        }
        [Command("friday")]
        public async Task Friday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Friday).ConfigureAwait(false);
        }
        [Command("saturday")]
        public async Task Saturday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Saturday).ConfigureAwait(false);
        }
        [Command("sunday")]
        public async Task Sunday(CommandContext ctx)
        {
            await DayGif(ctx, DayOfWeek.Sunday).ConfigureAwait(false);
        }
        [Command("day")]
        public async Task Day(CommandContext ctx)
        {
            var timeUtc = DateTime.UtcNow;
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
            await DayGif(ctx, easternTime.DayOfWeek).ConfigureAwait(false);
        }
        private async Task DayGif(CommandContext ctx, DayOfWeek dayOfWeek)
        {
            try
            {
                switch (dayOfWeek)
                {
                    case DayOfWeek.Monday:
                        await ctx.Channel.SendMessageAsync($"https://tenor.com/view/lazy-cat-stairs-monday-gif-15789725").ConfigureAwait(false);
                        break;
                    case DayOfWeek.Tuesday:
                        await ctx.Channel.SendMessageAsync($"https://tenor.com/view/three-amigos-taco-tuesday-dance-gif-14760901").ConfigureAwait(false);
                        break;
                    case DayOfWeek.Wednesday:
                        await ctx.Channel.SendMessageAsync($"https://tenor.com/view/its-chinese-wednesday-funny-asian-chinese-kid-gif-16305229").ConfigureAwait(false);
                        break;
                    case DayOfWeek.Thursday:
                        await ctx.Channel.SendMessageAsync($"https://tenor.com/view/excited-friday-tomorrow-yay-end-of-the-week-gif-5319510").ConfigureAwait(false);
                        break;
                    case DayOfWeek.Friday:
                        await ctx.Channel.SendMessageAsync($"https://cdn.discordapp.com/attachments/268196447667617803/716023791985229844/friday.mov").ConfigureAwait(false);
                        break;
                    case DayOfWeek.Saturday:
                        await ctx.Channel.SendMessageAsync($"https://tenor.com/view/saturday-dance-old-dancing-party-hard-gif-11712974").ConfigureAwait(false);
                        break;
                    case DayOfWeek.Sunday:
                        await ctx.Channel.SendMessageAsync($"https://tenor.com/view/holiday-weekend-sunday-back-to-work-gif-10666503").ConfigureAwait(false);
                        break;
                }
            }
            catch(Exception e)
            {
                AegisLog.Log(e.Message, e);
            }
            ctx.Message.DeleteAfter(3);
        }
    }
}
