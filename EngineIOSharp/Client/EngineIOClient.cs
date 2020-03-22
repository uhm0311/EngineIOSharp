using EngineIOSharp.Abstract;
using EngineIOSharp.Common.Enum;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;

namespace EngineIOSharp.Client
{
    public partial class EngineIOClient : EngineIOConnection
    {
        private readonly string URIFormat = "{0}://{1}:{2}/engine.io/?EIO=3&transport=websocket";

        public WebSocket WebSocketClient { get; private set; }

        public string SocketID { get; private set; }
        public string URI { get; private set; }

        public uint AutoReconnect { get; set; }
        public bool IsAlive
        {
            get
            {
                return WebSocketClient?.IsAlive ?? false;
            }
        }

        public EngineIOClient(WebSocketScheme Scheme, string Host, int Port, string SocketID = null, uint AutoReconnect = 0) 
        {
            string URI = string.Format(URIFormat, Scheme, Host, Port);

            if (!string.IsNullOrWhiteSpace(SocketID))
            {
                URI += string.Format("&sid={0}", SocketID);
            }

            Initialize(URI, AutoReconnect);
        }

        public EngineIOClient(string URI, uint AutoReconnect = 0)
        {
            Initialize(URI, AutoReconnect);
        }

        internal EngineIOClient(WebSocketContext Context)
        {
            URI = string.Format(URIFormat, Context.IsSecureConnection ? WebSocketScheme.wss : WebSocketScheme.ws, Context.UserEndPoint.Address, Context.UserEndPoint.Port);
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
            this.WebSocketClient = Client;
            this.WebSocketClient.Log.Output = (_, __) => { };

            Client.OnOpen += OnWebsocketOpen;
            Client.OnClose += OnWebsocketClose;
            Client.OnMessage += OnWebsocketMessage;
            Client.OnError += OnWebsocketError;
        }

        private void Initialize()
        {
            Initialize(new WebSocket(URI));
        }

        public void Connect()
        {
            if (WebSocketClient == null)
            {
                Initialize();
            }

            WebSocketClient.Connect();
        }

        public override void Close()
        {
            WebSocketClient?.Close();
            WebSocketClient = null;

            StopHeartbeat();
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
            return SocketID?.GetHashCode() ?? base.GetHashCode();
        }
    }
}
