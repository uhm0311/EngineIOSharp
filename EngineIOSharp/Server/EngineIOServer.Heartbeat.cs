using EngineIOSharp.Client;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    Timer TempTimer = new Timer(PingInterval);
                    TempTimer.Elapsed += (sender, e) =>
                    {
                        LockHeartbeat(Client, () =>
                        {
                            if (!Heartbeat.ContainsKey(Client))
                            {
                                Heartbeat.TryAdd(Client, 0);
                            }

                            if (Heartbeat[Client] > 0)
                            {
                                Heartbeat[Client] = 0;
                            }
                            else
                            {
                                Client?.Close();
                            }
                        });
                    };

                    TempTimer.AutoReset = false;
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
                    TempTimer.Stop();
                }
            });
        }

        private void LockHeartbeat(EngineIOClient Client, Action Callback)
        {
            lock (ClientMutex)
            {
                if (Client != null)
                {
                    if (!HeartbeatMutex.ContainsKey(Client))
                    {
                        HeartbeatMutex.TryAdd(Client, new object());
                    }

                    lock (HeartbeatMutex[Client])
                    {
                        Callback?.Invoke();
                    }
                }
            }
        }
    }
}
