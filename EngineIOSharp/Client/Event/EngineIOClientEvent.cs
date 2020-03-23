using EngineIOSharp.Common;

namespace EngineIOSharp.Client.Event
{
    public class EngineIOClientEvent : EngineIOEvent<EngineIOClientEvent>
    {
        private EngineIOClientEvent(string Data) : base(Data) { }

        public static readonly EngineIOClientEvent OPEN = new EngineIOClientEvent("open");
        public static readonly EngineIOClientEvent CLOSE = new EngineIOClientEvent("close");
        public static readonly EngineIOClientEvent MESSAGE = new EngineIOClientEvent("message");
        public static readonly EngineIOClientEvent ERROR = new EngineIOClientEvent("error");
        public static readonly EngineIOClientEvent FLUSH = new EngineIOClientEvent("flush");

        public static readonly EngineIOClientEvent PING_SEND = new EngineIOClientEvent("pingSend");
        public static readonly EngineIOClientEvent PING_RECEIVE = new EngineIOClientEvent("pingReceive");

        public static readonly EngineIOClientEvent PONG_SEND = new EngineIOClientEvent("pongSend");
        public static readonly EngineIOClientEvent PONG_RECEIVE = new EngineIOClientEvent("pongReceive");
    }
}
