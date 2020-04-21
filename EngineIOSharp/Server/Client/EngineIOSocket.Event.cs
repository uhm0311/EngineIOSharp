using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        public static class Event
        {
            public static readonly string OPEN = "open";
            public static readonly string HEARTBEAT = "heartbeat";

            public static readonly string ERROR = "error";
            public static readonly string CLOSE = "close";

            public static readonly string PACKET = "packet";
            public static readonly string MESSAGE = "message";

            public static readonly string PACKET_CREATE = "packetCreate";
            public static readonly string FLUSH = "flush";
            public static readonly string DRAIN = "drain";

            public static readonly string UPGRADE = "upgrade";
            public static readonly string UPGRADING = "upgrading";
            public static readonly string UPGRADE_ERROR = "upgradeError";
        }
    }
}
