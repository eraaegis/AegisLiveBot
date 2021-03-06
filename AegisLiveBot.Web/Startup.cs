﻿using AegisLiveBot.Core.Services;
using AegisLiveBot.Core.Services.Streaming;
using AegisLiveBot.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AegisLiveBot.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = new SqliteConnectionStringBuilder("Data Source=database.db");
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

            services.AddDbContext<Context>(options =>
            {
                options.UseSqlite(builder.ToString(),
                    x => x.MigrationsAssembly("AegisLiveBot.DAL.Migrations"));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            var bot = new LiveBot(services);
            services.AddSingleton(bot);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
