using AegisLiveBot.Core.Common;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands
{
    public class SearchCommands : BaseCommandModule
    {
        [Command("lmgtfy")]
        public async Task Lmgtfy(CommandContext ctx, params string[] s)
        {
            var search = string.Join("+", s);
            await ctx.Channel.SendMessageAsync($"https://lmgtfy.com/?q={search}").ConfigureAwait(false);
        }
    }
}
