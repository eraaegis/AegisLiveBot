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
        public async Task SetStreamingRole(CommandContext ctx, DiscordRole discordRole)
        {
            await _serverSettingService.SetOrReplaceRole(ctx.Guild.Id, discordRole.Id);
        }

        [Command("getstreamingrole")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task GetStreamingRole(CommandContext ctx)
        {
            var serverSetting = await _serverSettingService.GetServerSetting(ctx.Guild.Id);
            if (serverSetting == null)
            {
                await ctx.Channel.SendMessageAsync($"no role u dum fuck").ConfigureAwait(false);
                return;
            }
            var role = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Id == serverSetting.RoleId);
            await ctx.Channel.SendMessageAsync($"role is: {role.Value.Mention}").ConfigureAwait(false);
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
                msg += $"{i+1}. {user.DisplayName}, Stream: {liveUser.TwitchName}";
            }
            await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
        }
    }
}
