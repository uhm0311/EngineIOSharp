using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Server.Client.Transport;
using SimpleThreadMonitor;
using System;
using System.Threading;
using Timer = System.Timers.Timer;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        private readonly object CheckMutex = new object();
        private readonly object UpgradeMutex = new object();

        private readonly object PingMutex = new object();
        private readonly object PongMutex = new object();

        private Timer CheckTimer = null;
        private EngineIOTimeout UpgradeTimer = null;

        private EngineIOTimeout PingTimer = null;
        private EngineIOTimeout PongTimer = null;

        private ulong Pong = 0;

        private void StopTimers()
        {
            StopPingTimer();
            StopPongTimer();

            StopCheckTimer();
            StopUpgradeTimer();
        }

        private void StartPingTimer()
        {
            SimpleMutex.Lock(PingMutex, () =>
            {
                PingTimer = new EngineIOTimeout(() =>
                {
                    Send(EngineIOPacket.CreatePingPacket());
                    ResetPongTimer(Server.Option.PingTimeout);
                }, Server.Option.PingInterval * 1.1);
            });
        }

        private void StopPingTimer()
        {
            SimpleMutex.Lock(PingMutex, () => PingTimer?.Stop());
        }

        private void ResetPingTimer()
        {
            StopPingTimer();
            StartPingTimer();
        }

        private void StartPongTimer(ulong PingTimeout)
        {
            SimpleMutex.Lock(PongMutex, () =>
            {
                PongTimer = new EngineIOTimeout(() =>
                {
                    SimpleMutex.Lock(PongMutex, () =>
                    {
                        if (ReadyState != EngineIOReadyState.CLOSED)
                        {
                            if (Pong > 0)
                            {
                                Pong = 0;
                                ThreadPool.QueueUserWorkItem((_) => StartPongTimer(PingTimeout));
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

        private void StopPongTimer()
        {
            SimpleMutex.Lock(PongMutex, () => PongTimer?.Stop());
        }

        private void ResetPongTimer(ulong PingTimeout)
        {
            StopPongTimer();
            StartPongTimer(PingTimeout);
        }

        private void StartCheckTimer()
        {
            SimpleMutex.Lock(CheckMutex, () =>
            {
                CheckTimer = new Timer(100);
                CheckTimer.Elapsed += (_, __) =>
                {
                    if (Transport is EngineIOPolling && Transport.Writable)
                    {
                        Transport.Send(EngineIOPacket.CreateNoopPacket());
                    }
                };

                CheckTimer.AutoReset = true;
                CheckTimer.Start();
            });
        }

        private void StopCheckTimer()
        {
            SimpleMutex.Lock(CheckMutex, () => CheckTimer?.Stop());
        }

        private void ResetCheckTimer()
        {
            StopCheckTimer();
            StartCheckTimer();
        }

        private void StartUpgradeTimer(Action Callback)
        {
            SimpleMutex.Lock(CheckMutex, () => UpgradeTimer = new EngineIOTimeout(Callback, Server.Option.UpgradeTimeout));
        }

        private void StopUpgradeTimer()
        {
            SimpleMutex.Lock(UpgradeMutex, () => UpgradeTimer?.Stop());
        }
    }
}
