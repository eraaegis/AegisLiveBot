using AegisLiveBot.DAL.Models;
using AegisLiveBot.DAL.Models.Inhouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class InhouseRepository : Repository<InhouseDb>, IInhouseRepository
    {
        public InhouseRepository(Context context) : base(context) { }

        public void SetByInhouseQueue(InhouseQueue inhouseQueue)
        {
            var oldInhouseDb = _dbset.FirstOrDefault(x => x.ChannelId == inhouseQueue.ChannelId);
            if (oldInhouseDb != null)
            {
                oldInhouseDb.TopEmoji = inhouseQueue.Emojis[PlayerRole.Top];
                oldInhouseDb.JglEmoji = inhouseQueue.Emojis[PlayerRole.Jgl];
                oldInhouseDb.MidEmoji = inhouseQueue.Emojis[PlayerRole.Mid];
                oldInhouseDb.BotEmoji = inhouseQueue.Emojis[PlayerRole.Bot];
                oldInhouseDb.SupEmoji = inhouseQueue.Emojis[PlayerRole.Sup];
                oldInhouseDb.FillEmoji = inhouseQueue.Emojis[PlayerRole.Fill];
                _dbset.Update(oldInhouseDb);
                return;
            }

            var inhouseDb = new InhouseDb
            {
                ChannelId = inhouseQueue.ChannelId,
                TopEmoji = inhouseQueue.Emojis[PlayerRole.Top],
                JglEmoji = inhouseQueue.Emojis[PlayerRole.Jgl],
                MidEmoji = inhouseQueue.Emojis[PlayerRole.Mid],
                BotEmoji = inhouseQueue.Emojis[PlayerRole.Bot],
                SupEmoji = inhouseQueue.Emojis[PlayerRole.Sup],
                FillEmoji = inhouseQueue.Emojis[PlayerRole.Fill]
            };
            _dbset.Add(inhouseDb);
        }
        public void UnsetByChannelId(ulong channelId)
        {
            var inhouseDb = _dbset.FirstOrDefault(x => x.ChannelId == channelId);
            if (inhouseDb == null)
            {
                return;
            }

            Delete(inhouseDb);
        }
    }
}
