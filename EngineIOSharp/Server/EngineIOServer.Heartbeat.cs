using EngineIOSharp.Client;
using SimpleThreadMonitor;
using System;
using System.Collections.Concurrent;
using System.Timers;

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
                    Timer PingTimer = new Timer(PingInterval);
                    PingTimer.Elapsed += (sender, e) =>
                    {
                        Timer PongTimer = new Timer(PingTimeout * 1.25);
                        PongTimer.Elapsed += (sender2, e2) =>
                        {
                            SimpleMutex.Lock(ClientMutex, () =>
                            {
                                LockHeartbeat(Client, () =>
                                {
                                    if (Heartbeat.ContainsKey(Client) && Heartbeat[Client] > 0)
                                    {
                                        Heartbeat[Client] = 0;
                                    }
                                    else
                                    {
                                        Client.Close();
                                    }
                                });
                            });
                        };

                        PongTimer.AutoReset = false;
                        PongTimer.Start();
                    };

                    PingTimer.AutoReset = true;
                    PingTimer.Start();

                    HeartbeatTimer.TryAdd(Client, PingTimer);
                }
            });
        }

        private void StopHeartbeat(EngineIOClient Client)
        {
            LockHeartbeat(Client, () =>
            {
                if (HeartbeatTimer.ContainsKey(Client))
                {
                    HeartbeatTimer.TryRemove(Client, out Timer PingTimer);
                    Heartbeat.TryRemove(Client, out ulong __);

                    PingTimer.Stop();
                }
            });

            HeartbeatMutex.TryRemove(Client, out object _);
        }

        private void LockHeartbeat(EngineIOClient Client, Action Process)
        {
            if (Client != null)
            {
                if (!HeartbeatMutex.ContainsKey(Client))
                {
                    HeartbeatMutex.TryAdd(Client, new object());
                }

                SimpleMutex.Lock(HeartbeatMutex[Client], Process);
            }
        }
    }
}
