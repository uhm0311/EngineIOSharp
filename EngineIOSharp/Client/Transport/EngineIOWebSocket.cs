using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using EngineIOScheme = EngineIOSharp.Common.Enum.Internal.EngineIOScheme;

namespace EngineIOSharp.Client.Transport
{
    internal class EngineIOWebSocket : EngineIOTransport
    {
        private readonly WebSocket WebSocket;

        public EngineIOWebSocket(EngineIOClientOption Option) : base(Option)
        {
            StringBuilder URL = new StringBuilder();
            URL.Append(string.Format("{0}://{1}:{2}{3}", (EngineIOScheme)(Option.Scheme + 2), Option.Host, Option.Port, Option.Path)).Append('?');

            foreach (string Key in new List<string>(Option.Query.Keys))
            {
                URL.Append(Key).Append('=').Append(Option.Query[Key]).Append('&');
            }

            URL.Append("transport=websocket");

            if (Option.TimestampRequests ?? false)
            {
                URL.Append(string.Format("&{0}={1}", Option.TimestampParam, EngineIOTimestamp.Generate()));
            }

            WebSocket = new WebSocket(URL.ToString(), Option.WebSocketSubprotocols)
            {
                Compression = CompressionMethod.Deflate,
                CustomHeaders = Option.ExtraHeaders,
            };

            if (Option.WithCredentials && WebSocket.IsSecure)
            {
                WebSocket.SslConfiguration.ServerCertificateValidationCallback = Option.ServerCertificateValidationCallback;
                WebSocket.SslConfiguration.ClientCertificateSelectionCallback = Option.ClientCertificateSelectionCallback;

                if (Option.ClientCertificates != null)
                {
                    WebSocket.SslConfiguration.ClientCertificates = Option.ClientCertificates;
                }
            }

            WebSocket.OnOpen += OnWebSocketOpen;
            WebSocket.OnClose += OnWebSocketClose;
            WebSocket.OnMessage += OnWebSocketMessage;
            WebSocket.OnError += OnWebSocketError;
            WebSocket.Log.Output = EngineIOLogger.WebSocket;
        }

        private void OnWebSocketOpen(object sender, EventArgs e)
        {
            OnOpen();
        }

        private void OnWebSocketClose(object sender, CloseEventArgs e)
        {
            OnClose();
        }

        private void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            if (e.IsText || e.IsBinary)
            {
                EngineIOPacket Packet;

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
                else
                {
                    Packet = EngineIOPacket.Decode(e.RawData);
                }

                if (Packet.Type != EngineIOPacketType.CLOSE)
                {
                    OnPacket(Packet);

                    if (Packet.Type == EngineIOPacketType.OPEN)
                    {
                        Writable = true;
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        private void OnWebSocketError(object sender, ErrorEventArgs e)
        {
            OnError(e.Message, e.Exception);
            Writable = false;
        }

        protected override void CloseInternal()
        {
            if (WebSocket.IsAlive)
            {
                WebSocket.Close();
            }
        }

        protected override void OpenInternal()
        {
            if (!WebSocket.IsAlive)
            {
                WebSocket.Connect();
            }
        }

        protected override void SendInternal(EngineIOPacket Packet)
        {
            if (Packet != null)
            {
                object EncodedPacket = Packet.Encode(EngineIOTransportType.websocket, Option.ForceBase64, Protocol: Protocol);

                if (EncodedPacket is string)
                {
                    WebSocket.Send(EncodedPacket as string);
                }
                else if (EncodedPacket is byte[])
                {
                    WebSocket.Send(EncodedPacket as byte[]);
                }

                Emit(Event.FLUSH);
                Emit(Event.DRAIN);
            }
        }
    }
}