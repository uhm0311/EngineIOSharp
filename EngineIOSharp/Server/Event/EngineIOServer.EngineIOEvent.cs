using EngineIOSharp.Client;
using EngineIOSharp.Server.Event;
using SimpleThreadMonitor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly ConcurrentDictionary<EngineIOServerEvent, List<Action<EngineIOClient>>> EventHandlers = new ConcurrentDictionary<EngineIOServerEvent, List<Action<EngineIOClient>>>();
        private readonly object EventHandlersMutex = "EventHandlersMutex";

        public void On(EngineIOServerEvent Event, Action<EngineIOClient> Callback)
        {
            SimpleMutex.Lock(EventHandlersMutex, () =>
            {
                if (Event != null && Callback != null)
                {
                    if (!EventHandlers.ContainsKey(Event))
                    {
                        EventHandlers.TryAdd(Event, new List<Action<EngineIOClient>>());
                    }

                    EventHandlers[Event].Add(Callback);
                }
            });
        }

        public void Off(EngineIOServerEvent Event, Action<EngineIOClient> Callback)
        {
            SimpleMutex.Lock(EventHandlersMutex, () =>
            {
                if (Event != null && Callback != null && EventHandlers.ContainsKey(Event))
                {
                    EventHandlers[Event].Remove(Callback);
                }
            });
        }

        private void CallEventHandler(EngineIOServerEvent Event, EngineIOClient Client)
        {
            SimpleMutex.Lock(EventHandlersMutex, () =>
            {
                if (Event != null && EventHandlers.ContainsKey(Event))
                {
                    foreach (Action<EngineIOClient> Callback in EventHandlers[Event])
                    {
                        Callback?.Invoke(Client);
                    }
                }
            });
        }
    }
}
