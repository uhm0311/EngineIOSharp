using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using SimpleThreadMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        public EngineIOSocket Send(string Data, Action Callback = null)
        {
            if (!string.IsNullOrEmpty(Data))
            {
                Send(EngineIOPacket.CreateMessagePacket(Data), Callback);
            }

            return this;
        }

        public EngineIOSocket Send(byte[] RawData, Action Callback = null)
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
                SimpleMutex.Lock(BufferMutex, () => PacketBuffer.Enqueue(Packet));

                Once(Event.FLUSH, Callback);
                Flush();
            }
        }
    }
}
