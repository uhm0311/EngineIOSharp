using EngineIOSharp.Common.Type.Abstract;

namespace EngineIOSharp.Common.Type
{
    internal class EngineIOTransport : EngineIOType<EngineIOTransport, string>
    {
        private EngineIOTransport(string Data) : base(Data) { }

        public static readonly EngineIOTransport POLLING = new EngineIOTransport("polling");
        public static readonly EngineIOTransport WEBSOCKET = new EngineIOTransport("websocket");
    }
}
