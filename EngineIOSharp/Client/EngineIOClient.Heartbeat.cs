using EngineIOSharp.Common.Packet;
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

        private void StartPing()
        {
            lock (PingMutex)
            {
                if (PingTimer == null)
                {
                    PingTimer = new Timer(1000);
                    PingTimer.Elapsed += (sender, e) =>
                    {
                        lock (PingMutex)
                        {
                            PingTimer.Interval = PingInterval;

                            Send(EngineIOPacket.CreatePingPacket());
                            StartPong();
                        }
                    };

                    PingTimer.AutoReset = true;
                    PingTimer.Start();
                }
            }
        }

        private void StartPong()
        {
            lock (PongMutex)
            {
                if (PongTimer == null)
                {
                    PongTimer = new Timer(PingTimeout);
                    PongTimer.Elapsed += (sender, e) =>
                    {
                        lock (PongMutex)
                        {
                            if (Pong > 0)
                            {
                                Pong = 0;
                            }
                            else
                            {
                                Close();
                            }
                        }
                    };

                    PongTimer.AutoReset = false;
                }

                if (!PongTimer.Enabled)
                {
                    PongTimer.Start();
                }
            }
        }

        private void StopHeartbeat()
        {
            lock (PingMutex)
            {
                PingTimer?.Stop();
                PingTimer = null;
            }

            lock (PongMutex)
            {
                PongTimer?.Stop();
                PongTimer = null;
            }
        }
    }
}
