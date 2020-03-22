using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        public void Send(string Data, Action Callback = null)
        {
            Send(EngineIOPacket.CreateMessagePacket(Data), Callback);
        }

        public void Send(byte[] RawData, Action Callback = null)
        {
            Send(EngineIOPacket.CreateMessagePacket(RawData), Callback);
        }

        internal void Send(EngineIOPacket Packet, Action Callback = null)
        {
            if (IsAlive && Packet != null)
            {
                if (Packet.IsText)
                {
                    WebSocketClient.Send(Packet.Encode() as string);
                }
                else if (Packet.IsBinary)
                {
                    WebSocketClient.Send(Packet.Encode() as byte[]);
                }

                if (Packet.IsText || Packet.IsBinary)
                {
                    Callback?.Invoke();
                    CallEventHandler(EngineIOEvent.FLUSH);
                }
            }
        }
    }
}
