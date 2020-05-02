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
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace AegisLiveBot.Web.Commands.Fun
{
    public class TaflCommands : BaseCommandModule
    {
        private readonly DbService _db;

        public TaflCommands(DbService db)
        {
            _db = db;
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
                var responseMsg = response.Result.Content.ToLower();
                response.Result.DeleteAfter(3);
                if(responseMsg == "accept")
                {
                    break;
                } else
                {
                    --tries;
                    if(tries == 0)
                    {
                        await ctx.Channel.SendMessageAsync($"The game was not accepted.").ConfigureAwait(false);
                        return;
                    }
                }
            }
            try
            {
                var taflService = new TaflService(ctx.Channel, ctx.Member, otherUser, ctx.Client);
                taflService.Start();
            } catch(Exception e)
            {
                await ctx.Channel.SendMessageAsync(e.Message).ConfigureAwait(false);
            }
        }
    }
}
