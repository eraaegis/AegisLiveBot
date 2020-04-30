using AegisLiveBot.DAL;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AegisLiveBot.Core.Services
{
    public class DbService
    {
        private readonly DbContextOptions<Context> options;
        private readonly DbContextOptions<Context> migrateOptions;

        public DbService()
        {
            var builder = new SqliteConnectionStringBuilder("Data Source=database.db");
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

            var optionsBuilder = new DbContextOptionsBuilder<Context>();
            optionsBuilder.UseSqlite(builder.ToString(),
                    x => x.MigrationsAssembly("AegisLiveBot.DAL.Migrations"));
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options = optionsBuilder.Options;


            optionsBuilder = new DbContextOptionsBuilder<Context>();
            optionsBuilder.UseSqlite(builder.ToString(),
                    x => x.MigrationsAssembly("AegisLiveBot.DAL.Migrations"));
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            migrateOptions = optionsBuilder.Options;
        }

        public Context GetContext()
        {
            var context = new Context(options);
            if (context.Database.GetPendingMigrations().Any())
            {
                var migrate = new Context(migrateOptions);
                migrate.Database.Migrate();
                migrate.SaveChanges();
                migrate.Dispose();
            }
            context.Database.SetCommandTimeout(60);
            var conn = context.Database.GetDbConnection();
            conn.Open();

            using (var com = conn.CreateCommand())
            {
                com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF";
                com.ExecuteNonQuery();
            }
            return context;
        }
        public IUnitOfWork UnitOfWork()
        {
            return new UnitOfWork(GetContext());
        }
    }
}
