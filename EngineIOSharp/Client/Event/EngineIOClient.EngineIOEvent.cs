using EngineIOSharp.Client.Event;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using Newtonsoft.Json.Linq;
using SimpleThreadMonitor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private readonly ConcurrentDictionary<EngineIOClientEvent, List<Delegate>> EventHandlers = new ConcurrentDictionary<EngineIOClientEvent, List<Delegate>>();
        private readonly object EventHandlersMutex = "EventHandlersMutex";

        public void On(EngineIOClientEvent Event, Action Callback)
        {
            On(Event, Callback as Delegate);
        }

        public void On(EngineIOClientEvent Event, Action<EngineIOPacket> Callback)
        {
            On(Event, Callback as Delegate);
        }

        public void Off(EngineIOClientEvent Event, Action Callback)
        {
            Off(Event, Callback as Delegate);
        }

        public void Off(EngineIOClientEvent Event, Action<EngineIOPacket> Callback)
        {
            Off(Event, Callback as Delegate);
        }

        private void On(EngineIOClientEvent Event, Delegate Callback)
        {
            SimpleMutex.Lock(EventHandlersMutex, () =>
            {
                if (Event != null && Callback != null)
                {
                    if (!EventHandlers.ContainsKey(Event))
                    {
                        EventHandlers.TryAdd(Event, new List<Delegate>());
                    }

                    EventHandlers[Event].Add(Callback);
                }
            }, OnEngineIOError);
        }

        private void Off(EngineIOClientEvent Event, Delegate Callback)
        {
            SimpleMutex.Lock(EventHandlersMutex, () =>
            {
                if (Event != null && Callback != null && EventHandlers.ContainsKey(Event))
                {
                    EventHandlers[Event].Remove(Callback);
                }
            }, OnEngineIOError);
        }

        private void HandleEngineIOPacket(EngineIOPacket Packet)
        {
            try
            {
                if (Packet != null)
                {
                    switch (Packet.Type)
                    {
                        case EngineIOPacketType.OPEN:
                            JObject JsonData = JObject.Parse(Packet.Data);

                            SID = JsonData["sid"].ToString();
                            PingInterval = int.Parse(JsonData["pingInterval"].ToString());
                            PingTimeout = int.Parse(JsonData["pingTimeout"].ToString());

                            StartPing();
                            CallEventHandler(EngineIOClientEvent.OPEN);
                            break;

                        case EngineIOPacketType.CLOSE:
                            Close();
                            break;

                        case EngineIOPacketType.PING:
                            Send(EngineIOPacket.CreatePongPacket());

                            CallEventHandler(EngineIOClientEvent.PING_RECEIVE);
                            break;

                        case EngineIOPacketType.PONG:
                            Pong++;

                            CallEventHandler(EngineIOClientEvent.PONG_RECEIVE);
                            break;

                        case EngineIOPacketType.MESSAGE:
                            CallEventHandler(EngineIOClientEvent.MESSAGE, Packet);
                            break;
                    }
                }
            }
            catch (Exception Exception)
            {
                OnEngineIOError(new EngineIOException("Failed to handle packet. " + Packet, Exception));
            }
        }

        protected void HandleOpen(JObject JsonData)
        {
            if (JsonData != null)
            {
                SID = JsonData["sid"].ToString();
                PingInterval = int.Parse(JsonData["pingInterval"].ToString());
                PingTimeout = int.Parse(JsonData["pingTimeout"].ToString());

                StartPing();
                CallEventHandler(EngineIOClientEvent.OPEN);
            }
        }

        private void CallEventHandler(EngineIOClientEvent Event, EngineIOPacket Packet = null)
        {
            SimpleMutex.Lock(EventHandlersMutex, () =>
            {
                if (Event != null && EventHandlers.ContainsKey(Event))
                {
                    foreach (Delegate EventHandler in EventHandlers[Event])
                    {
                        if (EventHandler is Action)
                        {
                            (EventHandler as Action).Invoke();
                        }
                        else if (EventHandler is Action<EngineIOPacket>)
                        {
                            (EventHandler as Action<EngineIOPacket>).Invoke(Packet);
                        }
                    }
                }
            }, (Exception) => { if (!Event.Equals(EngineIOClientEvent.ERROR)) { OnEngineIOError(Exception); } }, true);
        }

        private void OnEngineIOError(Exception Exception)
        {
            CallEventHandler(EngineIOClientEvent.ERROR, EngineIOPacket.CreateErrorPacket(Exception));
        }
    }
}
