using AegisLiveBot.DAL.Models.Inhouse;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface IInhouseRepository : IRepository<InhouseDb>
    {
        void SetByInhouseQueue(InhouseQueue inhouseQueue);
        void UnsetByChannelId(ulong channelId);
    }
}
