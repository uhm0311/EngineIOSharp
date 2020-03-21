namespace EngineIOSharp.Common
{
    public class EngineIOEvent
    {
        public string Data { get; private set; }

        private EngineIOEvent(string Data)
        {
            this.Data = Data;
        }

        public override string ToString()
        {
            return Data ?? base.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is EngineIOEvent && ToString().Equals(obj.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static readonly EngineIOEvent OPEN = new EngineIOEvent("open");
        public static readonly EngineIOEvent CLOSE = new EngineIOEvent("close");
        public static readonly EngineIOEvent MESSAGE = new EngineIOEvent("message");
        public static readonly EngineIOEvent ERROR = new EngineIOEvent("error");

        internal static readonly EngineIOEvent PING = new EngineIOEvent("ping");
        internal static readonly EngineIOEvent PONG = new EngineIOEvent("pong");
    }
}
