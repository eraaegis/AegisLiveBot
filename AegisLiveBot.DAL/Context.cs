using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Streaming;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options) { }

        public DbSet<TestDB> TestDBs { get; set; }
        public DbSet<ServerSetting> ServerSettings { get; set; }
        public DbSet<LiveUser> LiveUsers { get; set; }
    }
}
