using EngineIOSharp.Client;
using EngineIOSharp.Client.Event;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Timer = System.Timers.Timer;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly ConcurrentDictionary<EngineIOClient, object> HeartbeatMutex = new ConcurrentDictionary<EngineIOClient, object>();
        private readonly ConcurrentDictionary<EngineIOClient, Timer> HeartbeatTimer = new ConcurrentDictionary<EngineIOClient, Timer>();

        private readonly ConcurrentDictionary<EngineIOClient, ulong> Heartbeat = new ConcurrentDictionary<EngineIOClient, ulong>();

        private void StartHeartbeat(EngineIOClient Client)
        {
            LockHeartbeat(Client, () =>
            {
                if (!HeartbeatTimer.ContainsKey(Client))
                {
                    Client.On(EngineIOClientEvent.PING_SEND, () =>
                    {
                        Monitor.Enter(ClientMutex);
                        {
                            if (!Heartbeat.ContainsKey(Client))
                            {
                                Heartbeat.TryAdd(Client, 0);
                            }

                            Heartbeat[Client]++;
                            Client?.Send(EngineIOPacket.CreatePongPacket());
                        }
                        Monitor.Exit(ClientMutex);
                    });

                    Client.On(EngineIOClientEvent.CLOSE, () =>
                    {
                        Monitor.Enter(ClientMutex);
                        {
                            ClientList.Remove(Client);
                            StopHeartbeat(Client);
                        }
                        Monitor.Exit(ClientMutex);
                    });

                    Timer TempTimer = new Timer(PingInterval + PingTimeout);
                    TempTimer.Elapsed += (sender, e) =>
                    {
                        Monitor.Enter(ClientMutex);
                        {
                            LockHeartbeat(Client, () =>
                            {
                                if (Heartbeat.ContainsKey(Client) && Heartbeat[Client] > 0)
                                {
                                    Heartbeat[Client] = 0;
                                }
                                else
                                {
                                    Client?.Close();
                                }
                            });
                        }
                        Monitor.Exit(ClientMutex);
                    };

                    TempTimer.AutoReset = true;
                    TempTimer.Start();

                    HeartbeatTimer.TryAdd(Client, TempTimer);
                }
            });
        }

        private void StopHeartbeat(EngineIOClient Client)
        {
            LockHeartbeat(Client, () =>
            {
                if (HeartbeatTimer.ContainsKey(Client))
                {
                    HeartbeatTimer.TryRemove(Client, out Timer TempTimer);
                    Heartbeat.TryRemove(Client, out ulong __);

                    TempTimer.Stop();
                }
            });

            HeartbeatMutex.TryRemove(Client, out object _);
        }

        private void LockHeartbeat(EngineIOClient Client, Action Callback)
        {
            if (Client != null)
            {
                if (!HeartbeatMutex.ContainsKey(Client))
                {
                    HeartbeatMutex.TryAdd(Client, new object());
                }

                Monitor.Enter(HeartbeatMutex[Client]);
                {
                    Callback?.Invoke();
                }
                Monitor.Exit(HeartbeatMutex[Client]);
            }
        }
    }
}
