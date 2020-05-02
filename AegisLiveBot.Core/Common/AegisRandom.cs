using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.Core.Common
{
    public static class AegisRandom
    {
        private static readonly Random random = new Random();
        private static readonly object randLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock (randLock)
            {
                return random.Next(min, max);
            }
        }
        public static bool RandomBool()
        {
            lock (randLock)
            {
                var i = random.Next(0, 2);
                if(i == 0)
                {
                    return false;
                } else
                {
                    return true;
                }
            }
        }
    }
}
