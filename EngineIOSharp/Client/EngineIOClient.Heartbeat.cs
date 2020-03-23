using EngineIOSharp.Common.Packet;
using System.Threading;
using Timer = System.Timers.Timer;

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
            Monitor.Enter(PingMutex);
            {
                if (PingTimer == null)
                {
                    PingTimer = new Timer(1000);
                    PingTimer.Elapsed += (sender, e) =>
                    {
                        Monitor.Enter(PingMutex);
                        {
                            PingTimer.Interval = PingInterval;

                            Send(EngineIOPacket.CreatePingPacket());
                            StartPong();
                        }
                        Monitor.Exit(PingMutex);
                    };

                    PingTimer.AutoReset = true;
                    PingTimer.Start();
                }
            }
            Monitor.Exit(PingMutex);
        }

        private void StartPong()
        {
            Monitor.Enter(PongMutex);
            {
                if (PongTimer == null)
                {
                    PongTimer = new Timer(PingTimeout);
                    PongTimer.Elapsed += (sender, e) =>
                    {
                        Monitor.Enter(PongMutex);
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
                        Monitor.Exit(PongMutex);
                    };

                    PongTimer.AutoReset = false;
                }

                if (!PongTimer.Enabled)
                {
                    PongTimer.Start();
                }
            }
            Monitor.Exit(PongMutex);
        }

        private void StopHeartbeat()
        {
            Monitor.Enter(PingMutex);
            {
                PingTimer?.Stop();
                PingTimer = null;
            }
            Monitor.Exit(PingMutex);

            Monitor.Enter(PongMutex);
            {
                PongTimer?.Stop();
                PongTimer = null;
            }
            Monitor.Exit(PongMutex);
        }
    }
}
