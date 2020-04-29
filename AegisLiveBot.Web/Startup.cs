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
            var path = new SqliteConnectionStringBuilder("Data Source=database.db");
            services.AddDbContext<Context>(options =>
            {
                options.UseSqlite(Path.Combine(AppContext.BaseDirectory, path.DataSource),
                    x => x.MigrationsAssembly("AegisLiveBot.DAL.Migrations"));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            services.AddScoped<ITestDBService, TestDBService>();
            services.AddScoped<IServerSettingService, ServerSettingService>();
            services.AddScoped<ILiveUserService, LiveUserService>();

            var serviceProvider = services.BuildServiceProvider();

            var bot = new LiveBot(serviceProvider);

            services.AddSingleton(bot);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
    }
}
