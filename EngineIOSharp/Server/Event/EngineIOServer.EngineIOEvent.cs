using EngineIOSharp.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using EngineIOSharp.Common.Packet;

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
                    case EngineIOPacketType.CLOSE:
                        Close();
                        break;

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

                    case EngineIOPacketType.PONG:
                        break;
                }
            }
        }
    }
}
