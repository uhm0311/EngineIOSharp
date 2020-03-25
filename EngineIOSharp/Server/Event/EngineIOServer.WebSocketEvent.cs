using EngineIOSharp.Client;
using EngineIOSharp.Client.Event;
using EngineIOSharp.Common;
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
            return new EngineIOBehavior((EngineIOClient Client) =>
            {
                SimpleMutex.Lock(ClientMutex, () =>
                {
                    if (!HeartbeatMutex.ContainsKey(Client))
                    {
                        Client.On(EngineIOClientEvent.PING_SEND, () =>
                        {
                            SimpleMutex.Lock(ClientMutex, () =>
                            {
                                if (!Heartbeat.ContainsKey(Client))
                                {
                                    Heartbeat.TryAdd(Client, 0);
                                }

                                Heartbeat[Client]++;
                                Client?.Send(EngineIOPacket.CreatePongPacket());
                            });
                        });

                        Client.On(EngineIOClientEvent.CLOSE, () =>
                        {
                            SimpleMutex.Lock(ClientMutex, () =>
                            {
                                ClientList.Remove(Client);
                                StopHeartbeat(Client);
                            });
                        });

                        Client.Send(EngineIOPacket.CreateOpenPacket(Client.SID, PingInterval, PingTimeout));
                        ClientList.Add(Client);

                        StartHeartbeat(Client);
                        CallEventHandler(EngineIOServerEvent.CONNECTION, Client);
                    }
                });
            });
        }

        private class EngineIOBehavior : WebSocketBehavior
        {
            private readonly Action<EngineIOClient> Initializer;

            internal EngineIOBehavior(Action<EngineIOClient> Initializer)
            {
                this.Initializer = Initializer;
            }

            protected override void OnOpen()
            {
                if (ID != null && Sessions[ID]?.Context != null)
                {
                    Initializer(new EngineIOClient(Sessions[ID].Context, Yeast.Key()));
                }
            }
        }
    }
}