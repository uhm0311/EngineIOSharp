using EngineIOSharp.Common.Type.Abstract;

namespace EngineIOSharp.Server.Event
{
    public class EngineIOServerEvent : EngineIOType<EngineIOServerEvent, string>
    {
        private EngineIOServerEvent(string Data) : base(Data) { }

        public static readonly EngineIOServerEvent CONNECTION = new EngineIOServerEvent("connection");
    }
}
