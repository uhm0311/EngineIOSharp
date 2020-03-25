using EngineIOSharp.Common.Enum;
using SimpleThreadMonitor;
using System;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;

namespace EngineIOSharp.Client
{
    public partial class EngineIOClient : IDisposable
    {
        private static readonly string URIFormat = "{0}://{1}:{2}/engine.io/?EIO=3&transport=websocket";

        private readonly object ClientMutex = "ClientMutex";

        public WebSocket WebSocketClient { get; private set; }

        public int PingInterval { get; private set; }
        public int PingTimeout { get; private set; }

        public string SID { get; private set; }
        public string URI { get; private set; }

        public uint AutoReconnect { get; set; }
        public bool IsAlive
        {
            get
            {
                return WebSocketClient?.IsAlive ?? false;
            }
        }

        public EngineIOClient(WebSocketScheme Scheme, string Host, int Port, string SID = null, uint AutoReconnect = 0) 
        {
            string URI = string.Format(URIFormat, Scheme, Host, Port);

            if (!string.IsNullOrWhiteSpace(SID))
            {
                URI += string.Format("&sid={0}", this.SID = SID);
            }

            Initialize(URI, AutoReconnect);
        }

        public EngineIOClient(string URI, uint AutoReconnect = 0)
        {
            Initialize(URI, AutoReconnect);
        }

        internal EngineIOClient(WebSocketContext Context, string SID)
        {
            this.SID = SID;

            URI = string.Format(URIFormat, Context.IsSecureConnection ? WebSocketScheme.wss : WebSocketScheme.ws, Context.ServerEndPoint.Address, Context.ServerEndPoint.Port);
            AutoReconnect = 0;

            Initialize(Context.WebSocket);
        }

        private void Initialize(string URI, uint AutoReconnect)
        {
            this.URI = URI;
            this.AutoReconnect = AutoReconnect;

            Initialize();
        }

        private void Initialize(WebSocket Client)
        {
            Action<LogData, string> LogOutput = WebSocketClient?.Log?.Output ?? ((_, __) => { });
            WebSocketClient = Client;

            WebSocketClient.Log.Output = LogOutput;
            WebSocketClient.OnOpen += OnWebsocketOpen;
            WebSocketClient.OnClose += OnWebsocketClose;
            WebSocketClient.OnMessage += OnWebsocketMessage;
            WebSocketClient.OnError += OnWebsocketError;
        }

        private void Initialize()
        {
            Initialize(new WebSocket(URI));
        }

        public void Connect()
        {
            SimpleMutex.Lock(ClientMutex, WebSocketClient.Connect, OnEngineIOError);
        }

        public void Close()
        {
            SimpleMutex.Lock(ClientMutex, () =>
            {
                WebSocketClient?.Close();
                StopHeartbeat();

                Initialize();
            }, OnEngineIOError);
        }

        public void Dispose()
        {
            Close();
        }

        public override bool Equals(object o)
        {
            if (o is EngineIOClient)
            {
                EngineIOClient Temp = o as EngineIOClient;

                return Temp.GetHashCode() == GetHashCode();
            }

            return false;
        }

        public override int GetHashCode()
        {
            return SID?.GetHashCode() ?? base.GetHashCode();
        }
    }
}
