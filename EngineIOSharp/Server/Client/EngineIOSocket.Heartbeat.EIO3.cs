using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using SimpleThreadMonitor;
using System.Threading;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        private EngineIOTimeout EIO3PingTimer = null;
        private EngineIOTimeout EIO3PongTimer = null;

        private void StopEIO3Hertbeat()
        {
            if (Transport.Protocol == 3)
            {
                StopEIO3PingTimer();
                StopEIO3PongTimer();
            }
        }

        private void StartEIO3PingTimer()
        {
            if (Transport.Protocol == 3)
            {
                SimpleMutex.Lock(PingMutex, () =>
                {
                    EIO3PingTimer = new EngineIOTimeout(() =>
                    {
                        Send(EngineIOPacket.CreatePingPacket());
                        ResetEIO3PongTimer(Server.Option.PingTimeout);
                    }, Server.Option.PingInterval * 1.1);
                });
            }
        }

        private void StopEIO3PingTimer()
        {
            if (Transport.Protocol == 3)
            {
                SimpleMutex.Lock(PingMutex, () => EIO3PingTimer?.Stop());
            }
        }

        private void ResetEIO3PingTimer()
        {
            if (Transport.Protocol == 3)
            {
                StopEIO3PingTimer();
                StartEIO3PingTimer();
            }
        }

        private void StartEIO3PongTimer(ulong PingTimeout)
        {
            if (Transport.Protocol == 3)
            {
                SimpleMutex.Lock(PongMutex, () =>
                {
                    EIO3PongTimer = new EngineIOTimeout(() =>
                    {
                        SimpleMutex.Lock(PongMutex, () =>
                        {
                            if (ReadyState != EngineIOReadyState.CLOSED)
                            {
                                if (Pong > 0)
                                {
                                    Pong = 0;
                                    ThreadPool.QueueUserWorkItem((_) => StartEIO3PongTimer(PingTimeout));
                                }
                                else
                                {
                                    OnClose("Ping timeout");
                                }
                            }
                        });
                    }, PingTimeout);
                });
            }
        }

        private void StopEIO3PongTimer()
        {
            if (Transport.Protocol == 3)
            {
                SimpleMutex.Lock(PongMutex, () => EIO3PongTimer?.Stop());
            }
        }

        private void ResetEIO3PongTimer(ulong PingTimeout)
        {
            if (Transport.Protocol == 3)
            {
                StopEIO3PongTimer();
                StartEIO3PongTimer(PingTimeout);
            }
        }
    }
}
