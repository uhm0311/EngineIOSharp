using EngineIOSharp.Client;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly List<Action<EngineIOClient>> ConnectionEventHandlers = new List<Action<EngineIOClient>>();
        private readonly object ConnectionEventHandlersMutex = new object();

        public void OnConnection(Action<EngineIOClient> Callback)
        {
            lock (ConnectionEventHandlersMutex)
            {
                if (Callback != null)
                {
                    ConnectionEventHandlers.Add(Callback);
                }
            }
        }

        public void OffConnection(Action<EngineIOClient> Callback)
        {
            lock (ConnectionEventHandlersMutex)
            {
                if (Callback != null)
                {
                    ConnectionEventHandlers.Remove(Callback);
                }
            }
        }

        private void HandleEngineIOPacket(EngineIOClient Client, EngineIOPacket Packet)
        {
            if (Packet != null)
            {
                switch (Packet.Type)
                {
                    case EngineIOPacketType.PING:
                        lock (ClientMutex)
                        {
                            if (!Heartbeat.ContainsKey(Client))
                            {
                                Heartbeat.TryAdd(Client, 0);
                            }

                            Heartbeat[Client]++;
                            Client?.Send(EngineIOPacket.CreatePongPacket());
                        }
                        break;
                }
            }
        }
    }
}
