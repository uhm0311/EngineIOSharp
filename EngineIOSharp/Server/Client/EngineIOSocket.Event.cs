using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        public EngineIOSocket OnClose(Action Callback)
        {
            return On(Event.CLOSE, Callback);
        }

        public EngineIOSocket OnClose(Action<string, Exception> Callback)
        {
            return On(Event.CLOSE, (Arguments) =>
            {
                object[] Temp = Arguments as object[];
                Callback(Temp[0] as string, Temp[1] as Exception);
            });
        }

        public EngineIOSocket OnMessage(Action<EngineIOPacket> Callback)
        {
            return On(Event.MESSAGE, (Packet) => Callback(Packet as EngineIOPacket));
        }

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
