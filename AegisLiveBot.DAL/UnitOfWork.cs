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

        private IServerSettingRepository _serverSettings;
        public IServerSettingRepository ServerSettings => _serverSettings ?? (_serverSettings = new ServerSettingRepository(_context));

        private ILiveUserRepository _liveUsers;
        public ILiveUserRepository LiveUsers => _liveUsers ?? (_liveUsers = new LiveUserRepository(_context));

        private IRoastMsgRepository _roastMsgs;
        public IRoastMsgRepository RoastMsgs => _roastMsgs ?? (_roastMsgs = new RoastMsgRepository(_context));

        private ICustomReplyRepository _customReplies;
        public ICustomReplyRepository CustomReplies => _customReplies ?? (_customReplies = new CustomReplyRepository(_context));

        private IInhouseRepository _inhouses;
        public IInhouseRepository Inhouses => _inhouses ?? (_inhouses = new InhouseRepository(_context));

        private IInhousePlayerStatRepository _inhousePlayerStats;
        public IInhousePlayerStatRepository InhousePlayerStats => _inhousePlayerStats ?? (_inhousePlayerStats = new InhousePlayerStatRepository(_context));

        private IMatchHistoryRepository _matchHistories;
        public IMatchHistoryRepository MatchHistories => _matchHistories ?? (_matchHistories = new MatchHistoryRepository(_context));

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
