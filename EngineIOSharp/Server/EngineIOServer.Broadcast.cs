using EngineIOSharp.Common.Packet;
using EngineIOSharp.Server.Client;
using System;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        public EngineIOServer Broadcast(string Data, Action Callback = null)
        {
            if (!string.IsNullOrEmpty(Data))
            {
                Braodcast(EngineIOPacket.CreateMessagePacket(Data), Callback);
            }

            return this;
        }

        public EngineIOServer Broadcast(byte[] RawData, Action Callback = null)
        {
            if ((RawData?.Length ?? 0) > 0)
            {
                Braodcast(EngineIOPacket.CreateMessagePacket(RawData), Callback);
            }

            return this;
        }

        private void Braodcast(EngineIOPacket Packet, Action Callback)
        {
            foreach (EngineIOSocket Socket in _Clients.Values)
            {
                Socket.Send(Packet);
            }

            Callback?.Invoke();
        }
    }
}
