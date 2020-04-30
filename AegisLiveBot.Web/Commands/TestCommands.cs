using AegisLiveBot.Core.Services;
using AegisLiveBot.DAL;
using AegisLiveBot.DAL.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Commands
{
    public class TestCommands : BaseCommandModule
    {
        private readonly Context _context;
        private readonly DbService _db;

        public TestCommands(Context context, DbService db)
        {
            _context = context;
            _db = db;
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Pong").ConfigureAwait(false);
        }

        [Command("gettestdb")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task GetTestDB(CommandContext ctx, string name)
        {
            var testDBs = new TestDBService(_context);
            var testDB = await testDBs.GetTestDBByName(name).ConfigureAwait(false);
            if (testDB == null)
            {
                await ctx.Channel.SendMessageAsync($"u dum bitch").ConfigureAwait(false);
                return;
            }
            await ctx.Channel.SendMessageAsync($"lol here u go: {testDB.Name}, {testDB.Value}").ConfigureAwait(false);
        }

        [Command("addtestdb")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task AddTestDB(CommandContext ctx, string name, int value)
        {
            var testDBs = new TestDBService(_context);
            await testDBs.AddTestDb(name, value);
        }
    }
}
