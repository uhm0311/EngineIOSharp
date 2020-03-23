using EngineIOSharp.Client;
using EngineIOSharp.Server.Event;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly ConcurrentDictionary<EngineIOServerEvent, List<Action<EngineIOClient>>> EventHandlers = new ConcurrentDictionary<EngineIOServerEvent, List<Action<EngineIOClient>>>();
        private readonly object EventHandlersMutex = new object();

        public void On(EngineIOServerEvent Event, Action<EngineIOClient> Callback)
        {
            Monitor.Enter(EventHandlersMutex);
            {
                if (Event != null && Callback != null)
                {
                    if (!EventHandlers.ContainsKey(Event))
                    {
                        EventHandlers.TryAdd(Event, new List<Action<EngineIOClient>>());
                    }

                    EventHandlers[Event].Add(Callback);
                }
            }
            Monitor.Exit(EventHandlersMutex);
        }

        public void Off(EngineIOServerEvent Event, Action<EngineIOClient> Callback)
        {
            Monitor.Enter(EventHandlersMutex);
            {
                if (Event != null && Callback != null && EventHandlers.ContainsKey(Event))
                {
                    EventHandlers[Event].Remove(Callback);
                }
            }
            Monitor.Exit(EventHandlersMutex);
        }

        private void CallEventHandler(EngineIOServerEvent Event, EngineIOClient Client)
        {
            Monitor.Enter(EventHandlersMutex);
            {
                if (Event != null && EventHandlers.ContainsKey(Event))
                {
                    foreach (Action<EngineIOClient> Callback in EventHandlers[Event])
                    {
                        Callback?.Invoke(Client);
                    }
                }
            }
            Monitor.Exit(EventHandlersMutex);
        }
    }
}
