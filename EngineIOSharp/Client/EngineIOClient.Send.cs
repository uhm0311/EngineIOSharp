using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        public EngineIOClient Send(string Data, Action Callback = null)
        {
            if (!string.IsNullOrEmpty(Data))
            {
                Send(EngineIOPacket.CreateMessagePacket(Data), Callback);
            }

            return this;
        }

        public EngineIOClient Send(byte[] RawData, Action Callback = null)
        {
            if ((RawData?.Length ?? 0) > 0)
            {
                Send(EngineIOPacket.CreateMessagePacket(RawData), Callback);
            }

            return this;
        }

        internal void Send(EngineIOPacket Packet, Action Callback = null)
        {
            if (ReadyState == EngineIOReadyState.OPENING || ReadyState == EngineIOReadyState.OPEN)
            {
                Emit(Event.PACKET_CREATE, Packet);
                PacketBuffer.Enqueue(Packet);

                Once(Event.FLUSH, Callback);
                Flush();
            }
        }
    }
}
