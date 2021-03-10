﻿using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.CustomCrawler;
using AegisLiveBot.DAL.Models.Fun;
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

        public DbSet<ServerSetting> ServerSettings { get; set; }
        public DbSet<LiveUser> LiveUsers { get; set; }
        public DbSet<RoastMsg> RoastMsgs { get; set; }
        public DbSet<CustomReplyDb> CustomReplies { get; set; }
    }
}
