using EngineIOSharp.Common;

namespace EngineIOSharp.Server.Event
{
    public class EngineIOServerEvent : EngineIOEvent<EngineIOServerEvent>
    {
        private EngineIOServerEvent(string Data) : base(Data) { }

        public static readonly EngineIOServerEvent CONNECTION = new EngineIOServerEvent("connection");
    }
}
