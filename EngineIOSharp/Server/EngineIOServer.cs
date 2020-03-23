using EngineIOSharp.Client;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Server.Event;
using System;
using System.Net;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    public partial class EngineIOServer : IDisposable
    {
        private readonly object ServerMutex = new object();

        public WebSocketServer WebSocketServer { get; private set; }

        public int PingInterval { get; private set; }
        public int PingTimeout { get; private set; }

        public EngineIOClient[] Clients { get { return ClientList.ToArray(); } }
        public int ClientsCount { get { return ClientList.Count; } }

        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }

        public bool IsListening
        {
            get { return WebSocketServer.IsListening; }
        }

        public bool IsSecure
        {
            get { return WebSocketServer.IsSecure; }
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

            Initialize();
        }

        private void Initialize()
        {
            Action<LogData, string> LogOutput = WebSocketServer?.Log?.Output ?? ((_, __) => { });
            WebSocketServer = new WebSocketServer(IPAddress, Port, IsWebSocketSecure);

            WebSocketServer.Log.Output = LogOutput;
            WebSocketServer.AddWebSocketService("/engine.io/", () => new EngineIOBehavior((EngineIOClient Client, string SocketID) =>
            {
                Monitor.Enter(ClientMutex);
                {
                    if (!HeartbeatMutex.ContainsKey(Client))
                    {
                        Client.Send(EngineIOPacket.CreateOpenPacket(SocketID, PingInterval, PingTimeout));
                        ClientList.Add(Client);

                        StartHeartbeat(Client);
                        CallEventHandler(EngineIOServerEvent.CONNECTION, Client);
                    }
                }
                Monitor.Exit(ClientMutex);
            }));
        }

        public void Start()
        {
            Monitor.Enter(ServerMutex);
            {
                if (!IsListening)
                {
                    WebSocketServer.Start();
                }
            }
            Monitor.Exit(ServerMutex);
        }

        public void Close()
        {
            Monitor.Enter(ServerMutex);
            {
                if (IsListening)
                {
                    Monitor.Enter(ClientMutex);
                    {
                        foreach (EngineIOClient Client in ClientList)
                        {
                            Client.Close();
                        }

                        WebSocketServer.Stop();
                        Initialize();
                    }
                    Monitor.Exit(ClientMutex);
                }
            }
            Monitor.Exit(ServerMutex);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
