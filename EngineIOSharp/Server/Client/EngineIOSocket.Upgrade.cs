using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Server.Client.Transport;
using SimpleThreadMonitor;
using System;
using Timer = System.Timers.Timer;

namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        private readonly object CheckMutex = new object();
        private readonly object UpgradeMutex = new object();

        private Timer CheckTimer = null;
        private EngineIOTimeout UpgradeTimer = null;

        private void StopCheckUpgradeTimers()
        {
            StopCheckTimer();
            StopUpgradeTimer();
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
