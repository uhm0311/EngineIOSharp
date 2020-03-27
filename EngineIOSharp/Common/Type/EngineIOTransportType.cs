using EngineIOSharp.Common.Type.Abstract;

namespace EngineIOSharp.Common.Type
{
    internal class EngineIOTransportType : EngineIOType<EngineIOTransportType, string>
    {
        private EngineIOTransportType(string Data) : base(Data) { }

        public static readonly EngineIOTransportType POLLING = new EngineIOTransportType("polling");
        public static readonly EngineIOTransportType WEBSOCKET = new EngineIOTransportType("websocket");
    }
}
