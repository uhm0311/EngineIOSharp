using EngineIOSharp.Client;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly ConcurrentDictionary<string, WebSocket> SocketIDList = new ConcurrentDictionary<string, WebSocket>();

        private readonly ConcurrentDictionary<WebSocket, EngineIOClient> ClientList = new ConcurrentDictionary<WebSocket, EngineIOClient>();
        private readonly object ClientMutex = new object();

        protected override void OnOpen()
        {
            lock (ClientMutex)
            {
                if (ID != null)
                {
                    WebSocketContext Context = Sessions[ID].Context;

                    if (!ClientList.ContainsKey(Context.WebSocket))
                    {
                        EngineIOClient Client = new EngineIOClient(Context);

                        SocketIDList.TryAdd(ID, Context.WebSocket);
                        ClientList.TryAdd(Context.WebSocket, Client);

                        Context.WebSocket.OnMessage += (sender, e) =>
                        {
                            try { HandleEngineIOPacket(ClientList[sender as WebSocket], EngineIOPacket.Decode(e)); }
                            catch { }
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
                if (ID != null && SocketIDList.ContainsKey(ID))
                {
                    SocketIDList.TryRemove(ID, out WebSocket WebSocket);
                    ClientList.TryRemove(WebSocket, out EngineIOClient Client);

                    StopHeartbeat(Client);
                    Client?.Close();
                }
            }
        }
    }
}