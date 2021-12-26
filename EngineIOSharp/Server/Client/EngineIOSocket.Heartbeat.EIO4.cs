using EngineIOSharp.Common.Packet;
using SimpleThreadMonitor;
using Timer = System.Timers.Timer;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        private Timer EIO4PingTimer = null;
        private Timer EIO4PongTimer = null;

        private void StartEIO4Heartbeat()
        {
            if (Transport.Protocol == 4)
            {
                SimpleMutex.Lock(PingMutex, () =>
                {
                    if (EIO4PingTimer == null)
                    {
                        EIO4PingTimer = new Timer(Server.Option.PingInterval);
                        EIO4PingTimer.Elapsed += (_, __) =>
                        {
                            SimpleMutex.Lock(PingMutex, () =>
                            {
                                Send(EngineIOPacket.CreatePingPacket());

                                SimpleMutex.Lock(PongMutex, () =>
                                {
                                    if (EIO4PongTimer == null)
                                    {
                                        EIO4PongTimer = new Timer(Server.Option.PingTimeout);
                                        EIO4PongTimer.Elapsed += (___, ____) =>
                                        {
                                            SimpleMutex.Lock(PongMutex, () =>
                                            {
                                                if (Pong > 0)
                                                {
                                                    Pong = 0;
                                                }
                                                else
                                                {
                                                    Close();
                                                }
                                            });
                                        };

                                        EIO4PongTimer.AutoReset = false;
                                    }

                                    if (!EIO4PongTimer.Enabled)
                                    {
                                        EIO4PongTimer.Start();
                                    }
                                });
                            });
                        };

                        EIO4PingTimer.AutoReset = true;
                        EIO4PingTimer.Start();
                    }
                });
            }
        }

        private void StopEIO4Heartbeat()
        {
            if (Transport.Protocol == 4)
            {
                SimpleMutex.Lock(PingMutex, () =>
                {
                    EIO4PingTimer?.Stop();
                    EIO4PingTimer = null;
                });

                SimpleMutex.Lock(PongMutex, () =>
                {
                    EIO4PongTimer?.Stop();
                    EIO4PongTimer = null;
                });
            }
        }
    }
}
