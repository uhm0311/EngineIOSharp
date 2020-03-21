using EngineIOSharp.Abstract;
using EngineIOSharp.Common.Packet;
using System;
using System.Timers;

namespace EngineIOSharp.Common.Manager
{
    public class EngineIOHeartbeatManager : IDisposable
    {
        private readonly EngineIOConnection Connection = null;
        private readonly int PingInterval = 0;
        private readonly int PingTimeout = 0;

        private Timer Heartbeat = null;
        private Timer HeartbeatListener = null;

        private ulong Pong = 0;

        internal EngineIOHeartbeatManager(EngineIOConnection Connection, int PingInterval, int PingTimeout)
        {
            this.Connection = Connection;
            this.PingInterval = PingInterval;
            this.PingTimeout = PingTimeout;

            Connection.On(EngineIOEvent.PING, () => Connection.Send(EngineIOPacket.CreatePongPacket()));
            Connection.On(EngineIOEvent.PONG, () => Pong++);
        }

        public void Start()
        {
            if (Heartbeat == null)
            {
                Heartbeat = new Timer(PingInterval);
                Heartbeat.Elapsed += (sender, e) =>
                {
                    Connection.Send(EngineIOPacket.CreatePingPacket());
                    StartHeartbeatListener();
                };

                Heartbeat.AutoReset = true;
                Heartbeat.Start();
            }
        }

        public void Stop()
        {
            Heartbeat?.Stop();
            Heartbeat = null;

            HeartbeatListener?.Stop();
            HeartbeatListener = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private void StartHeartbeatListener()
        {
            if (HeartbeatListener == null)
            {
                HeartbeatListener = new Timer(PingTimeout);
                HeartbeatListener.Elapsed += (sender, e) =>
                {
                    if (Pong > 0)
                    {
                        Pong = 0;
                    }
                    else
                    {
                        Connection.Close();
                    }
                };

                HeartbeatListener.AutoReset = false;
            }

            if (!HeartbeatListener.Enabled)
            {
                HeartbeatListener.Start();
            }
        }
    }
}
