using EngineIOSharp.Client;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Server.Event;
using SimpleThreadMonitor;
using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly List<EngineIOClient> ClientList = new List<EngineIOClient>();
        private readonly object ClientMutex = "ClientMutex";

        private EngineIOBehavior CreateBehavior()
        {
            return new EngineIOBehavior((EngineIOClient Client, string SocketID) =>
            {
                SimpleMutex.Lock(ClientMutex, () =>
                {
                    if (!HeartbeatMutex.ContainsKey(Client))
                    {
                        Client.Send(EngineIOPacket.CreateOpenPacket(SocketID, PingInterval, PingTimeout));
                        ClientList.Add(Client);

                        StartHeartbeat(Client);
                        CallEventHandler(EngineIOServerEvent.CONNECTION, Client);
                    }
                });
            });
        }

        private class EngineIOBehavior : WebSocketBehavior
        {
            private readonly Action<EngineIOClient, string> Initializer;

            internal EngineIOBehavior(Action<EngineIOClient, string> Initializer)
            {
                this.Initializer = Initializer;
            }

            protected override void OnOpen()
            {
                if (ID != null && Sessions[ID]?.Context != null)
                {
                    Initializer(new EngineIOClient(Sessions[ID].Context), ID);
                }
            }
        }
    }
}