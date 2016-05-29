using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudStorageManager
{
    public static class DebugInfo
    {

        private static Action<string> LogEvent;

        public static void ListenLog(Action<string> log)
        {
            LogEvent += log;
        }
        public static void StopListenLog(Action<string> log )
        {
            LogEvent -= log;
        }

        public static void  Log(string text)
        {
                LogEvent(text);
        }
    }
}
