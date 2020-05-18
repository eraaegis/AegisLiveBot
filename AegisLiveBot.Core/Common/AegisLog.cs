using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AegisLiveBot.Core.Common
{
    public static class AegisLog
    {
        private static readonly string logFolderPath = Path.Combine(AppContext.BaseDirectory, "Log");
        private const string logFile = "log.txt";

        public static void Log(string logMessage, Exception e = null)
        {
            Directory.CreateDirectory(logFolderPath);
            using(StreamWriter w = File.AppendText(Path.Combine(logFolderPath, logFile))){
                w.Write("\r\nLog Entry: ");
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine("  :");
                w.WriteLine($"  :{logMessage}");
                w.WriteLine("-------------------------------");
                if (e != null)
                {
                    w.WriteLine($"Stack trace :{e.StackTrace}");
                    w.WriteLine("-------------------------------");
                }
            }
        }
    }
}
