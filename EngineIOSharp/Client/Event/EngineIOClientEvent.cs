using EngineIOSharp.Common.Type.Abstract;

namespace EngineIOSharp.Client.Event
{
    public class EngineIOClientEvent : EngineIOType<EngineIOClientEvent, string>
    {
        private EngineIOClientEvent(string Data) : base(Data) { }

        public static readonly EngineIOClientEvent OPEN = new EngineIOClientEvent("open");
        public static readonly EngineIOClientEvent CLOSE = new EngineIOClientEvent("close");
        public static readonly EngineIOClientEvent MESSAGE = new EngineIOClientEvent("message");
        public static readonly EngineIOClientEvent UPGRADE = new EngineIOClientEvent("upgrade");

        public static readonly EngineIOClientEvent ERROR = new EngineIOClientEvent("error");

        public static readonly EngineIOClientEvent DRAIN = new EngineIOClientEvent("drain");
        public static readonly EngineIOClientEvent FLUSH = new EngineIOClientEvent("flush");

        public static readonly EngineIOClientEvent PACKET = new EngineIOClientEvent("packet");
        public static readonly EngineIOClientEvent PACKET_CREATE = new EngineIOClientEvent("packetCreate");
    }
}
