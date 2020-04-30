using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Streaming;
using AegisLiveBot.DAL;
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

namespace AegisLiveBot.Commands
{
    public class StreamingCommands : BaseCommandModule
    {
        private readonly Context _context;
        private ServerSettingService _serverSettingService;
        private LiveUserService _liveUserService;

        public StreamingCommands(Context context)
        {
            _context = context;
            _serverSettingService = new ServerSettingService(_context);
            _liveUserService = new LiveUserService(_context, LiveBot.Client);
        }

        [Command("setstreamingrole")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetStreamingRole(CommandContext ctx, DiscordRole discordRole = null)
        {
            var roleId = discordRole != null ? discordRole.Id : 0;
            await _serverSettingService.SetOrReplaceRole(ctx.Guild.Id, roleId);
        }

        [Command("getstreamingrole")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task GetStreamingRole(CommandContext ctx)
        {
            var serverSetting = await _serverSettingService.GetOrCreateServerSetting(ctx.Guild.Id);
            var role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Id == serverSetting.RoleId).Value;
            if (role == null)
            {
                await ctx.Channel.SendMessageAsync($"Live role has not been set!").ConfigureAwait(false);
                return;
            }
            await ctx.Channel.SendMessageAsync($"Role is: {role.Mention}").ConfigureAwait(false);
        }
        [Command("settwitchchannel")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetTwitchChannel(CommandContext ctx, DiscordChannel ch = null)
        {
            var chId = ch != null ? ch.Id : 0;
            await _serverSettingService.SetOrReplaceTwitchChannel(ctx.Guild.Id, chId).ConfigureAwait(false);
        }
        [Command("toggleprioritymode")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task TogglePriorityMode(CommandContext ctx)
        {
            Console.WriteLine("wtf1");
            var result = await _serverSettingService.TogglePriorityMode(ctx.Guild.Id).ConfigureAwait(false);
            Console.WriteLine("wtf2");
            var msg = result ? "on" : "off";
            Console.WriteLine("wtf3");
            await ctx.Channel.SendMessageAsync($"Priority mode is now {msg}.");
            Console.WriteLine("wtf4");
        }
        [Command("addliveuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task AddLiveUser(CommandContext ctx, DiscordUser user, string twitchName)
        {
            await _liveUserService.AddOrReplaceLiveUser(ctx.Guild.Id, user.Id, twitchName);
        }

        [Command("removeliveuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task RemoveLiveUser(CommandContext ctx, DiscordUser user)
        {
            await _liveUserService.RemoveLiveUser(ctx.Guild.Id, user.Id);
        }

        [Command("listliveuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ListLiveUser(CommandContext ctx)
        {
            var liveUsers = _liveUserService.ListLiveUser(ctx.Guild.Id);
            if(liveUsers.Count() == 0)
            {
                await ctx.Channel.SendMessageAsync($"No users currently registered for streaming role!").ConfigureAwait(false);
                return;
            }
            var msg = $"Users registered to streaming role:\n";
            for(var i = 0; i < liveUsers.Count(); ++i)
            {
                var liveUser = liveUsers.ElementAt(i);
                var user = await ctx.Guild.GetMemberAsync(liveUser.UserId).ConfigureAwait(false);
                msg += $"{i+1}. {user.DisplayName}, Stream: {liveUser.TwitchName}\n";
            }
            await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
        }
        [Command("togglepriorityuser")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task TogglePriorityUser(CommandContext ctx, DiscordUser user)
        {
            var liveUser = await _liveUserService.GetLiveUser(ctx.Guild.Id, user.Id).ConfigureAwait(false);
            if(liveUser == null)
            {
                await ctx.Channel.SendMessageAsync("User is not registered for streaming role!").ConfigureAwait(false);
                return;
            }
            var result = await _liveUserService.TogglePriorityUser(ctx.Guild.Id, user.Id).ConfigureAwait(false);
            var msg = result ? "now" : "no longer";
            await ctx.Channel.SendMessageAsync($"{user.Username} is {msg} a priority user.").ConfigureAwait(false);
        }
    }
}
