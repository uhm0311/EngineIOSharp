using EngineIOSharp.Client;
using EngineIOSharp.Common.Packet;
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
        private readonly object EventHandlersMutex = new object();

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

        private void HandleEngineIOPacket(EngineIOClient Client, EngineIOPacket Packet)
        {
            SimpleMutex.Lock(ClientMutex, () =>
            {
                if (Client != null && Packet != null)
                {
                    switch (Packet.Type)
                    {
                        case EngineIOPacketType.UPGRADE:
                            SIDList.Remove(Client.SID);
                            break;
                    }
                }
            });
        }
    }
}
