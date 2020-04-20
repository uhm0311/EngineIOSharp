using System;
using WebSocketSharp;

namespace EngineIOSharp.Common
{
    public static class EngineIOLogger
    {
        public static bool DoWrite = true;

        public readonly static Action<LogData, string> WebSocket = (data, message) =>
        {
            if (DoWrite)
            {
                Console.WriteLine("{0} : {1}", data, message);
            }
        };

        public readonly static Action<object, Exception> Error = (sender, e) =>
        {
            if (DoWrite)
            {
                Console.WriteLine("{0} : {1}", sender, e);
            }
        };
    }
}
