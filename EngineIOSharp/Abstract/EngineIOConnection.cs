using EngineIOSharp.Common;
using EngineIOSharp.Common.Action;
using EngineIOSharp.Common.Manager;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EngineIOSharp.Abstract
{
    public abstract partial class EngineIOConnection : IDisposable
    {
        private readonly ConcurrentDictionary<EngineIOEvent, List<Delegate>> EventHandlers = new ConcurrentDictionary<EngineIOEvent, List<Delegate>>();
        private EngineIOHeartbeatManager HeartbeatManager = null;

        internal EngineIOConnection()
        {

        }

        public void Dispose()
        {
            StopHeartbeat();
            Close();
        }

        public void On(EngineIOEvent Event, Action Callback)
        {
            On(Event, Callback as Delegate);
        }

        public void On(EngineIOEvent Event, EngineIOAction Callback)
        {
            On(Event, Callback as Delegate);
        }

        public void Off(EngineIOEvent Event, Action Callback)
        {
            Off(Event, Callback as Delegate);
        }

        public void Off(EngineIOEvent Event, EngineIOAction Callback)
        {
            Off(Event, Callback as Delegate);
        }

        private void On(EngineIOEvent Event, Delegate Callback)
        {
            if (Event != null && Callback != null)
            {
                if (!EventHandlers.ContainsKey(Event))
                {
                    EventHandlers.TryAdd(Event, new List<Delegate>());
                }

                EventHandlers[Event].Add(Callback);
            }
        }

        private void Off(EngineIOEvent Event, Delegate Callback)
        {
            if (Event != null && Callback != null && EventHandlers.ContainsKey(Event))
            {
                EventHandlers[Event].Remove(Callback);
            }
        }

        protected void CallEventHandler(EngineIOEvent Event, EngineIOPacket Packet = null)
        {
            if (Event != null && EventHandlers.ContainsKey(Event))
            {
                foreach (Delegate EventHandler in EventHandlers[Event])
                {
                    if (EventHandler is Action)
                    {
                        (EventHandler as Action).Invoke();
                    }
                    else if (EventHandler is EngineIOAction)
                    {
                        (EventHandler as EngineIOAction).Invoke(Packet);
                    }
                }
            }
        }

        protected void StartHeartbeat(int PingInterval, int PingTimeout)
        {
            if (HeartbeatManager == null)
            {
                HeartbeatManager = new EngineIOHeartbeatManager(this, PingInterval, PingTimeout);
                HeartbeatManager.Start();
            }
        }

        private void StopHeartbeat()
        {
            HeartbeatManager?.Stop();
            HeartbeatManager = null;
        }

        public abstract void Close();
    }
}
