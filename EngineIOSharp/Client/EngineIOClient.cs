using EngineIOSharp.Abstract;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using WebSocketSharp;

namespace EngineIOSharp.Client
{
    public partial class EngineIOClient : EngineIOConnection
    {
        private WebSocket Client = null;

        public string SocketID { get; private set; }

        public string URI { get; private set; }

        public uint AutoReconnect { get; set; }

        public bool IsAlive
        {
            get
            {
                return Client?.IsAlive ?? false;
            }
        }

        public EngineIOClient(WebSocketScheme Scheme, string Host, int Port, string SocketID = null, uint AutoReconnect = 0) 
        {
            string URI = string.Format("{0}://{1}:{2}/engine.io/?EIO=3&transport=websocket", Scheme, Host, Port);

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

        private void Initialize(string URI, uint AutoReconnect)
        {
            this.URI = URI;
            this.AutoReconnect = AutoReconnect;
        }

        private void Initialize()
        {
            Client = new WebSocket(URI);

            Client.OnOpen += OnWebsocketOpen;
            Client.OnClose += OnWebsocketClose;
            Client.OnMessage += OnWebsocketMessage;
            Client.OnError += OnWebsocketError;
        }

        public void Connect()
        {
            if (Client == null)
            {
                Initialize();
            }

            Client.Connect();
        }

        public override void Close()
        {
            Client?.Close();
            Client = null;
        }

        internal override void Send(EngineIOPacket Packet)
        {
            if (IsAlive && Packet != null)
            {
                object Encoded = Packet.Encode();

                if (Packet.IsBinary)
                {
                    Client.Send(Encoded as byte[]);
                }
                else if (Packet.IsText)
                {
                    Client.Send(Encoded as string);
                }
            }
        }
    }
}
