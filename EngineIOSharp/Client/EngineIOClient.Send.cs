using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using SimpleThreadMonitor;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        public EngineIOClient Send(string Data, Action Callback = null)
        {
            Send(EngineIOPacket.CreateMessagePacket(Data ?? string.Empty), Callback);

            return this;
        }

        public EngineIOClient Send(byte[] RawData, Action Callback = null)
        {
            Send(EngineIOPacket.CreateMessagePacket(RawData ?? new byte[0]), Callback);

            return this;
        }

        internal void Send(EngineIOPacket Packet, Action Callback = null)
        {
            if (ReadyState == EngineIOReadyState.OPENING || ReadyState == EngineIOReadyState.OPEN)
            {
                Emit(Event.PACKET_CREATE, Packet);
                SimpleMutex.Lock(BufferMutex, () => PacketBuffer.Enqueue(Packet));

                Once(Event.FLUSH, Callback);
                Flush();
            }
        }
    }
}
