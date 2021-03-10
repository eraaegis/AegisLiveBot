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
        IServerSettingRepository ServerSettings { get; }
        ILiveUserRepository LiveUsers { get; }
        IRoastMsgRepository RoastMsgs { get; }
        ICustomReplyRepository CustomReplies { get; }
        void Save();
        Task SaveAsync();
    }
}
