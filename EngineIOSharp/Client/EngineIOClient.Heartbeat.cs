using EngineIOSharp.Common.Packet;
using SimpleThreadMonitor;
using System.Timers;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private readonly object PingMutex = new object();
        private readonly object PongMutex = new object();

        private Timer PingTimer = null;
        private Timer PongTimer = null;

        private ulong Pong = 0;

        private void StartHeartbeat()
        {
            SimpleMutex.Lock(PingMutex, () =>
            {
                if (PingTimer == null && Handshake != null)
                {
                    ulong PingInterval = Handshake.PingInterval;

                    PingTimer = new Timer(PingInterval / 2.0);
                    PingTimer.Elapsed += (_, __) =>
                    {
                        SimpleMutex.Lock(PingMutex, () =>
                        {
                            PingTimer.Interval = PingInterval;
                            Send(EngineIOPacket.CreatePingPacket());

                            SimpleMutex.Lock(PongMutex, () =>
                            {
                                if (PongTimer == null && Handshake != null)
                                {
                                    PongTimer = new Timer(Handshake.PingTimeout);
                                    PongTimer.Elapsed += (___, ____) =>
                                    {
                                        SimpleMutex.Lock(PongMutex, () =>
                                        {
                                            if (Pong > 0)
                                            {
                                                Pong = 0;
                                            }
                                            else
                                            {
                                                OnClose("Heartbeat timeout");
                                            }
                                        });
                                    };

                                    PongTimer.AutoReset = false;
                                }

                                if (!PongTimer.Enabled)
                                {
                                    PongTimer.Start();
                                }
                            });
                        });
                    };

                    PingTimer.AutoReset = true;
                    PingTimer.Start();
                }
            });
        }

        private void StopHeartbeat()
        {
            SimpleMutex.Lock(PingMutex, () =>
            {
                PingTimer?.Stop();
                PingTimer = null;
            });

            SimpleMutex.Lock(PongMutex, () =>
            {
                PongTimer?.Stop();
                PongTimer = null;
            });
        }
    }
}
