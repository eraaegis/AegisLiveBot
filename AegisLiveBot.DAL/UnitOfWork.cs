using AegisLiveBot.DAL.Repository;
using AegisLiveBot.DAL.Repository.Implementation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        public Context _context { get; }
        private ITestDBRepository _testDBRepository;
        public ITestDBRepository TestDBRespository => _testDBRepository ?? (_testDBRepository = new TestDBRepository(_context));

        public UnitOfWork(Context context)
        {
            _context = context;
        }
        public void Save()
        {
            _context.SaveChanges();
        }
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        private bool disposed = false;
        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
