using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;

namespace EngineIOSharp.Server.Client.Transport
{
    internal class EngineIOWebSocket : EngineIOTransport
    {
        private readonly Semaphore Semaphore;
        private readonly WebSocket Client;

        internal EngineIOWebSocket(WebSocketContext Context, int Protocol) : base(Protocol)
        {
            Semaphore = new Semaphore(0, 1);
            Semaphore.Release();

            Client = Context.WebSocket;
            Client.OnMessage += OnWebSocketMessage;
            Client.OnClose += OnWebSocketClose;
            Client.OnError += OnWebSocketError;
            Client.Log.Output = EngineIOLogger.WebSocket;

            ForceBase64 = Protocol == 4 || EngineIOHttpManager.IsBase64Forced(Context.QueryString);
            Writable = true;
        }

        private void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            NameValueCollection Headers = new NameValueCollection();
            Emit(Event.HEADERS, Headers);

            if (Headers.Count > 0)
            {
                Dictionary<string, string> CustomHeaders = new Dictionary<string, string>();

                foreach (string Key in Headers.AllKeys)
                {
                    CustomHeaders[Key] = Headers[Key];
                }

                Client.CustomHeaders = CustomHeaders;
            }

            EngineIOPacket Packet = EngineIOPacket.CreateNoopPacket();

            if (e.IsText)
            {
                string Data = e.Data;

                if (Data.StartsWith("b"))
                {
                    Packet = EngineIOPacket.DecodeBase64String(Data, Protocol);
                }
                else
                {
                    Packet = EngineIOPacket.Decode(Data);
                }
            }
            else if (e.IsBinary)
            {
                Packet = EngineIOPacket.Decode(e.RawData);
            }

            if (Packet.Type != EngineIOPacketType.CLOSE)
            {
                OnPacket(Packet);
            }
            else
            {
                Close();
            }
        }

        private void OnWebSocketClose(object sender, CloseEventArgs e)
        {
            OnClose();
        }

        private void OnWebSocketError(object sender, ErrorEventArgs e)
        {
            OnError(e.Message, e.Exception);
        }

        protected override void CloseInternal(Action Callback)
        {
            Client.Close();
            Callback?.Invoke();
        }

        internal override EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            if (Packets != null)
            {
                Writable = false;

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    try
                    {
                        Semaphore.WaitOne();

                        foreach (EngineIOPacket Packet in Packets)
                        {
                            object EncodedPacket = Packet.Encode(EngineIOTransportType.websocket, ForceBase64, Protocol: Protocol);

                            if (EncodedPacket is string)
                            {
                                Client.Send(EncodedPacket as string);
                            }
                            else if (EncodedPacket is byte[])
                            {
                                Client.Send(EncodedPacket as byte[]);
                            }
                        }

                        Semaphore.Release();
                        Writable = true;

                        Emit(Event.DRAIN);
                    }
                    catch (Exception Exception)
                    {
                        OnError("WebSocket not sent.", Exception);
                    }
                });
            }

            return this;
        }
    }
}
