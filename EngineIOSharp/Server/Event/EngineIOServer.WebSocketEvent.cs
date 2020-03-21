using EngineIOSharp.Client;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Concurrent;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly ConcurrentDictionary<WebSocket, EngineIOClient> ClientList = new ConcurrentDictionary<WebSocket, EngineIOClient>();

        private readonly object ClientMutex = new object();

        protected override void OnOpen()
        {
            lock (ClientMutex)
            {
                if (ID != null)
                {
                    WebSocketContext Context = Sessions[ID].Context;

                    if (!ClientList.ContainsKey(Sessions[ID].Context.WebSocket))
                    {
                        EngineIOClient Client = new EngineIOClient(Context);
                        ClientList.TryAdd(Sessions[ID].Context.WebSocket, Client);

                        Context.WebSocket.OnMessage += (sender, e) =>
                        {
                            HandleEngineIOPacket(ClientList[sender as WebSocket], EngineIOPacket.Decode(e));
                        };

                        Client.Send(EngineIOPacket.CreateOpenPacket(ID, PingInterval, PingTimeout));
                        StartHeartbeat(Client);

                        lock (ConnectionEventHandlersMutex)
                        {
                            foreach (Action<EngineIOClient> Callback in ConnectionEventHandlers)
                            {
                                Callback?.Invoke(Client);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            lock (ClientMutex)
            {
                if (ID != null)
                {
                    if (ClientList.ContainsKey(Sessions[ID].Context.WebSocket))
                    {
                        ClientList.TryRemove(Sessions[ID].Context.WebSocket, out EngineIOClient Client);
                        Client?.Close();
                    }
                }
            }
        }
    }
}