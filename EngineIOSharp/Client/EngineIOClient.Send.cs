using EngineIOSharp.Client.Event;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        public void Send(string Data, Action Callback = null)
        {
            if (!string.IsNullOrEmpty(Data))
            {
                Send(EngineIOPacket.CreateMessagePacket(Data), Callback);
            }
        }

        public void Send(byte[] RawData, Action Callback = null)
        {
            if ((RawData?.Length ?? 0) > 0)
            {
                Send(EngineIOPacket.CreateMessagePacket(RawData), Callback);
            }
        }

        internal void Send(EngineIOPacket Packet, Action Callback = null)
        {
            try
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
                        CallEventHandler(EngineIOClientEvent.FLUSH);

                        if (Packet.Type == EngineIOPacketType.PING)
                        {
                            CallEventHandler(EngineIOClientEvent.PING_SEND);
                        }
                        else if (Packet.Type == EngineIOPacketType.PONG)
                        {
                            CallEventHandler(EngineIOClientEvent.PONG_SEND);
                        }
                    }
                }
            }
            catch (Exception Exception)
            {
                OnEngineIOError(new EngineIOException("Failed to send packet. " + Packet, Exception));
            }
        }
    }
}
