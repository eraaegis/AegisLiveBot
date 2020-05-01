using AegisLiveBot.Core.Common;
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
        private readonly DbService _db;

        public TestCommands(DbService db)
        {
            _db = db;
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Pong").ConfigureAwait(false);
            ctx.Message.DeleteAfter(3);
        }

        [Command("gettestdb")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task GetTestDB(CommandContext ctx, string name)
        {
            using (var uow = _db.UnitOfWork())
            {
                var testDB = uow.TestDBs.GetByName(name);
                if(testDB == null)
                {
                    await ctx.Channel.SendMessageAsync($"Item does not exist!").ConfigureAwait(false);
                } else
                {
                    await ctx.Channel.SendMessageAsync($"Item {testDB.Name} has value {testDB.Value}.").ConfigureAwait(false);
                }
            }
            ctx.Message.DeleteAfter(3);
        }

        [Command("addtestdb")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task AddOrUpdateTestDB(CommandContext ctx, string name, int value)
        {
            using (var uow = _db.UnitOfWork())
            {
                var result = uow.TestDBs.AddOrUpdateByNameAndValue(name, value);
                await uow.SaveAsync().ConfigureAwait(false);
                var msg = "";
                if (result)
                {
                    msg = $"Item {name} with value {value} has been added to the Test database.";
                } else
                {
                    msg = $"Item {name} has been updated to have value {value}.";
                }
                await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }

        [Command("listtestdb")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ListTestDB(CommandContext ctx)
        {
            using (var uow = _db.UnitOfWork())
            {
                var testDBs = uow.TestDBs.GetAll();
                var msg = "";
                var index = 1;
                foreach(var testDB in testDBs)
                {
                    msg += $"{index}. {testDB.Name}: {testDB.Value}\n";
                    ++index;
                }
                await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
            }
            ctx.Message.DeleteAfter(3);
        }
    }
}
