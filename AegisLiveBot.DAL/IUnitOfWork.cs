using AegisLiveBot.DAL.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.DAL
{
    public interface IUnitOfWork : IDisposable
    {
        Context _context { get; }
        ITestDBRepository TestDBs { get; }
        IServerSettingRepository ServerSettings { get; }
        ILiveUserRepository LiveUsers { get; }
        IRoastMsgRepository RoastMsgs { get; }
        void Save();
        Task SaveAsync();
    }
}
