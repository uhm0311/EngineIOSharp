using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        public EngineIOClient OnOpen(Action Callback)
        {
            return On(Event.OPEN, Callback);
        }

        public EngineIOClient OnClose(Action Callback)
        {
            return On(Event.CLOSE, Callback);
        }

        public EngineIOClient OnClose(Action<Exception> Callback)
        {
            return On(Event.CLOSE, (Exception) => Callback(Exception as Exception));
        }

        public EngineIOClient OnMessage(Action<EngineIOPacket> Callback)
        {
            return On(Event.MESSAGE, (Packet) => Callback(Packet as EngineIOPacket));
        }

        public static class Event
        {
            public static readonly string OPEN = "open";
            public static readonly string HANDSHAKE = "handshake";

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
