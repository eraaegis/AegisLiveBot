using AegisLiveBot.DAL;
using AegisLiveBot.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Services
{
    public interface ITestDBService
    {
        Task<TestDB> GetTestDBByName(string name);
        Task AddTestDb(string name, int value);
    }
    public class TestDBService : ITestDBService
    {
        private readonly Context _context;
        public TestDBService(Context context)
        {
            _context = context;
        }
        public async Task<TestDB> GetTestDBByName(string name)
        {
            name = name.ToLower();
            return await _context.TestDBs.FirstOrDefaultAsync(x => x.Name.ToLower() == name).ConfigureAwait(false);
        }
        public async Task AddTestDb(string name, int value)
        {
            await _context.TestDBs.AddAsync(new TestDB { Name = name, Value = value }).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
