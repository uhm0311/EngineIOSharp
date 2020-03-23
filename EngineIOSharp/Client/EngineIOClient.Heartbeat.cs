using EngineIOSharp.Common.Packet;
using SimpleThreadMonitor;
using System.Timers;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private readonly object PingMutex = "PingMutex";
        private readonly object PongMutex = "PongMutex";

        private Timer PingTimer = null;
        private Timer PongTimer = null;

        private ulong Pong = 0;

        private void StartPing()
        {
            SimpleMutex.Lock(PingMutex, () =>
            {
                if (PingTimer == null)
                {
                    PingTimer = new Timer(1000);
                    PingTimer.Elapsed += (sender, e) =>
                    {
                        SimpleMutex.Lock(PingMutex, () =>
                        {
                            PingTimer.Interval = PingInterval;

                            Send(EngineIOPacket.CreatePingPacket());
                            StartPong();
                        }, OnEngineIOError);
                    };

                    PingTimer.AutoReset = true;
                    PingTimer.Start();
                }
            }, OnEngineIOError);
        }

        private void StartPong()
        {
            SimpleMutex.Lock(PongMutex, () =>
            {
                if (PongTimer == null)
                {
                    PongTimer = new Timer(PingTimeout);
                    PongTimer.Elapsed += (sender, e) =>
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
                        }, OnEngineIOError);
                    };

                    PongTimer.AutoReset = false;
                }

                if (!PongTimer.Enabled)
                {
                    PongTimer.Start();
                }
            }, OnEngineIOError);
        }

        private void StopHeartbeat()
        {
            SimpleMutex.Lock(PingMutex, () =>
            {
                PingTimer?.Stop();
                PingTimer = null;
            }, OnEngineIOError);

            SimpleMutex.Lock(PongMutex, () =>
            {
                PongTimer?.Stop();
                PongTimer = null;
            }, OnEngineIOError);
        }
    }
}
