using AegisLiveBot.Core.Common;
using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Streaming;
using AegisLiveBot.DAL;
using AegisLiveBot.DAL.Repository;
using AegisLiveBot.Web;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands
{
    public class StreamingCommands : BaseCommandModule
    {
        private readonly DbService _db;

        public StreamingCommands(DbService db)
        {
            _db = db;
        }

        [Command("setstreamingrole")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetStreamingRole(CommandContext ctx, DiscordRole discordRole = null)
        {
            using (var uow = _db.UnitOfWork())
            {
                var roleId = discordRole != null ? discordRole.Id : 0;
                uow.ServerSettings.SetStreamingRole(ctx.Guild.Id, roleId);
                await uow.SaveAsync().ConfigureAwait(false); ;
                await ctx.Channel.SendMessageAsync($"Live role has been set to {discordRole.Name}").ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }

        [Command("getstreamingrole")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task GetStreamingRole(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var serverSetting = uow.ServerSettings.GetOrAddByGuildId(ctx.Guild.Id);
                var role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Id == serverSetting.RoleId).Value;
                if (role == null)
                {
                    await ctx.Channel.SendMessageAsync($"Live role has not been set!").ConfigureAwait(false);
                } else
                {
                    await ctx.Channel.SendMessageAsync($"Live role is: {role.Name}").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("settwitchchannel")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetTwitchChannel(CommandContext ctx, DiscordChannel ch = null)
        {
            using (var uow = _db.UnitOfWork())
            {
                var chId = ch != null ? ch.Id : 0;
                uow.ServerSettings.SetTwitchChannel(ctx.Guild.Id, chId);
                await uow.SaveAsync().ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"Twitch Discord channel has been set to {ch.Mention}").ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("gettwitchchannel")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task GetTwitchChannel(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var serverSetting = uow.ServerSettings.GetOrAddByGuildId(ctx.Guild.Id);
                var ch = ctx.Guild.Channels.FirstOrDefault(x => x.Value.Id == serverSetting.TwitchChannelId).Value;
                if(ch == null)
                {
                    await ctx.Channel.SendMessageAsync($"Twitch Discord channel has not been set!").ConfigureAwait(false);
                } else
                {
                    await ctx.Channel.SendMessageAsync($"Twitch Discord channel: {ch.Mention}").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("toggletwitchchannel")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ToggleTwitchChannel(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var result = uow.ServerSettings.ToggleTwitchChannel(ctx.Guild.Id);
                await uow.SaveAsync().ConfigureAwait(false);
                var msg = result ? "on" : "off";
                await ctx.Channel.SendMessageAsync($"Streams alert is now {msg}.");
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("toggleprioritymode")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task TogglePriorityMode(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var result = uow.ServerSettings.TogglePriorityMode(ctx.Guild.Id);
                await uow.SaveAsync().ConfigureAwait(false);
                var msg = result ? "on" : "off";
                await ctx.Channel.SendMessageAsync($"Priority mode is now {msg}.");
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("addliveuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task AddLiveUser(CommandContext ctx, DiscordMember user, string twitchName)
        {
            using (var uow = _db.UnitOfWork())
            {
                uow.LiveUsers.UpdateTwitchName(ctx.Guild.Id, user.Id, twitchName);
                await uow.SaveAsync().ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"{user.DisplayName} has been registered for streaming role with twitch name {twitchName}.").ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }

        [Command("removeliveuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task RemoveLiveUser(CommandContext ctx, DiscordMember user)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    uow.LiveUsers.RemoveByGuildIdUserId(ctx.Guild.Id, user.Id);
                    await uow.SaveAsync().ConfigureAwait(false);
                    await ctx.Channel.SendMessageAsync($"{user.DisplayName} has been unregistered for streaming role.").ConfigureAwait(false);
                } catch(Exception e)
                {
                    await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }

        [Command("listliveuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ListLiveUser(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var liveUsers = uow.LiveUsers.GetAllByGuildId(ctx.Guild.Id);
                if (liveUsers.Count() == 0)
                {
                    await ctx.Channel.SendMessageAsync($"No users currently registered for streaming role!").ConfigureAwait(false);
                } else
                {
                    var msg = $"Users registered to streaming role (Star indicates priority):\n";
                    for (var i = 0; i < liveUsers.Count(); ++i)
                    {
                        var liveUser = liveUsers.ElementAt(i);
                        var user = await ctx.Guild.GetMemberAsync(liveUser.UserId).ConfigureAwait(false);
                        var priority = liveUser.PriorityUser ? "*" : "";
                        msg += $"{priority}{i + 1}. {user.DisplayName}, Stream: {liveUser.TwitchName}\n";
                    }
                    await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("togglepriorityuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task TogglePriorityUser(CommandContext ctx, DiscordUser user)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    var result = uow.LiveUsers.TogglePriorityUser(ctx.Guild.Id, user.Id);
                    await uow.SaveAsync().ConfigureAwait(false);
                    var msg = result ? "now" : "no longer";
                    await ctx.Channel.SendMessageAsync($"{user.Username} is {msg} a priority user.").ConfigureAwait(false);
                } catch(RepositoryException e)
                {
                    await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }
        [Command("togglealertuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ToggleAlertUser(CommandContext ctx, DiscordUser user)
        {
            using (var uow = _db.UnitOfWork())
            {
                try
                {
                    var result = uow.LiveUsers.ToggleAlertUser(ctx.Guild.Id, user.Id);
                    await uow.SaveAsync().ConfigureAwait(false);
                    var msg = result ? "now" : "no longer";
                    await ctx.Channel.SendMessageAsync($"Twitch live alert {msg} set for {user.Username}.").ConfigureAwait(false);
                } catch(RepositoryException e)
                {
                    await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
                }
            }
        }
    }
}
