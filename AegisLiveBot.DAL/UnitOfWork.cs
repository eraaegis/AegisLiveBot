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
        private ITestDBRepository _testDBs;
        public ITestDBRepository TestDBs => _testDBs ?? (_testDBs = new TestDBRepository(_context));
        private IServerSettingRepository _serverSettings;
        public IServerSettingRepository ServerSettings => _serverSettings ?? (_serverSettings = new ServerSettingRepository(_context));
        private ILiveUserRepository _liveUsers;
        public ILiveUserRepository LiveUsers => _liveUsers ?? (_liveUsers = new LiveUserRepository(_context));
        private IRoastMsgRepository _roastMsgs;
        public IRoastMsgRepository RoastMsgs => _roastMsgs ?? (_roastMsgs = new RoastMsgRepository(_context));

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
