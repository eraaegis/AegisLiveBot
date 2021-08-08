using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AegisLiveBot.Core.Common
{
    public static class Extension
    {
        public static void DeleteAfter(this DiscordMessage msg, int s)
        {
            Task.Run(async () =>
            {
                await Task.Delay(s * 1000).ConfigureAwait(false);
                await msg.DeleteAsync().ConfigureAwait(false);
            });
        }

        public static async Task SendFileAsync(DiscordChannel ch, string imagePath, string content = null)
        {
            var discordMsg = new DiscordMessageBuilder {};
            if (content != null)
            {
                discordMsg.Content = content;
            }
            var imageStream = File.OpenRead(imagePath);
            discordMsg = discordMsg.WithFile(imageStream);
            await ch.SendMessageAsync(discordMsg).ConfigureAwait(false);
        }
    }
}
