using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private readonly ConcurrentDictionary<EngineIOEvent, List<Delegate>> EventHandlers = new ConcurrentDictionary<EngineIOEvent, List<Delegate>>();

        public void On(EngineIOEvent Event, Action Callback)
        {
            On(Event, Callback as Delegate);
        }

        public void On(EngineIOEvent Event, Action<EngineIOPacket> Callback)
        {
            On(Event, Callback as Delegate);
        }

        public void Off(EngineIOEvent Event, Action Callback)
        {
            Off(Event, Callback as Delegate);
        }

        public void Off(EngineIOEvent Event, Action<EngineIOPacket> Callback)
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

        private void HandleEngineIOPacket(EngineIOPacket Packet)
        {
            if (Packet != null)
            {
                switch (Packet.Type)
                {
                    case EngineIOPacketType.OPEN:
                        JObject JsonData = JObject.Parse(Packet.Data);

                        SocketID = JsonData["sid"].ToString();
                        PingInterval = int.Parse(JsonData["pingInterval"].ToString());
                        PingTimeout = int.Parse(JsonData["pingTimeout"].ToString());

                        StartPing();
                        CallEventHandler(EngineIOEvent.OPEN);
                        break;

                    case EngineIOPacketType.CLOSE:
                        Close();
                        break;

                    case EngineIOPacketType.PING:
                        Send(EngineIOPacket.CreatePongPacket());

                        CallEventHandler(EngineIOEvent.PING);
                        break;

                    case EngineIOPacketType.PONG:
                        Pong++;

                        CallEventHandler(EngineIOEvent.PONG);
                        break;

                    case EngineIOPacketType.MESSAGE:
                        CallEventHandler(EngineIOEvent.MESSAGE, Packet);
                        break;
                }
            }
        }

        protected void HandleOpen(JObject JsonData)
        {
            if (JsonData != null)
            {
                SocketID = JsonData["sid"].ToString();
                PingInterval = int.Parse(JsonData["pingInterval"].ToString());
                PingTimeout = int.Parse(JsonData["pingTimeout"].ToString());

                StartPing();
                CallEventHandler(EngineIOEvent.OPEN);
            }
        }

        private void CallEventHandler(EngineIOEvent Event, EngineIOPacket Packet = null)
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
        }
    }
}
