using EngineIOSharp.Common.Packet;

namespace EngineIOSharp.Abstract
{
    partial class EngineIOConnection
    {
        public void Send(string Data)
        {
            Send(EngineIOPacket.CreateMessagePacket(Data));
        }

        public void Send(byte[] RawData)
        {
            Send(EngineIOPacket.CreateMessagePacket(RawData));
        }

        internal abstract void Send(EngineIOPacket Packet);
    }
}
