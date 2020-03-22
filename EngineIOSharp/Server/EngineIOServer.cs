using EngineIOSharp.Abstract;
using EngineIOSharp.Client;
using System.Linq;
using System.Net;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    public partial class EngineIOServer : EngineIOConnection
    {
        private readonly object ServerMutex = new object();

        public WebSocketServer WebSocketServer { get; private set; }

        public EngineIOClient[] Clients { get { return ClientList.Values.ToArray(); } }
        public int ClientsCount { get { return ClientList.Values.Count; } }

        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }

        public bool IsListening
        {
            get { return WebSocketServer?.IsListening ?? false; }
        }

        public bool IsSecure
        {
            get { return WebSocketServer?.IsSecure ?? IsWebSocketSecure; }
        }

        private bool IsWebSocketSecure = false;

        public EngineIOServer(int Port, bool IsSecure = false, int PingInterval = 25000, int PingTimeout = 5000)
        {
            Initialize(IPAddress.Any, Port, IsSecure, PingInterval, PingTimeout);
        }

        public EngineIOServer(IPAddress IPAddress, int Port, bool IsSecure = false, int PingInterval = 25000, int PingTimeout = 5000)
        {
            Initialize(IPAddress, Port, IsSecure, PingInterval, PingTimeout);
        }

        private void Initialize(IPAddress IPAddress, int Port, bool IsSecure, int PingInterval, int PingTimeout)
        {
            this.IPAddress = IPAddress;
            this.Port = Port;
            IsWebSocketSecure = IsSecure;

            this.PingInterval = PingInterval;
            this.PingTimeout = PingTimeout;
        }

        public void Start()
        {
            lock (ServerMutex)
            {
                if (WebSocketServer == null)
                {
                    WebSocketServer = new WebSocketServer(IPAddress, Port, IsWebSocketSecure);
                    WebSocketServer.Log.Output = (_, __) => { };
                }

                WebSocketServer.AddWebSocketService("/engine.io/", () => new WebSocketEvent
                (
                    PingInterval,
                    PingTimeout,
                    SocketIDList,
                    ClientList,
                    ClientMutex,
                    ConnectionEventHandlers,
                    ConnectionEventHandlersMutex,
                    StartHeartbeat,
                    StopHeartbeat,
                    HandleEngineIOPacket
                ));

                WebSocketServer.Start();
            }
        }

        public override void Close()
        {
            lock (ServerMutex)
            {
                WebSocketServer?.Stop();
                WebSocketServer = null;
            }
        }
    }
}
