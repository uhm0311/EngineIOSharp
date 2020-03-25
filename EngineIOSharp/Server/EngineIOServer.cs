using EngineIOSharp.Client;
using SimpleThreadMonitor;
using System;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    public partial class EngineIOServer : IDisposable
    {
        public const string DefaultServerPath = "/engine.io/";
        private readonly object ServerMutex = "ServerMutex";

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
        private string ServerPath = DefaultServerPath;

        public EngineIOServer(int Port, int PingInterval = 25000, int PingTimeout = 5000, bool IsSecure = false, string ServerPath = DefaultServerPath)
        {
            Initialize(IPAddress.Any, Port, PingInterval, PingTimeout, IsSecure, ServerPath);
        }

        public EngineIOServer(IPAddress IPAddress, int Port, int PingInterval = 25000, int PingTimeout = 5000, bool IsSecure = false, string ServerPath = DefaultServerPath)
        {
            Initialize(IPAddress, Port, PingInterval, PingTimeout, IsSecure, ServerPath);
        }

        private void Initialize(IPAddress IPAddress, int Port, int PingInterval, int PingTimeout, bool IsSecure, string ServerPath)
        {
            this.IPAddress = IPAddress;
            this.Port = Port;

            IsWebSocketSecure = IsSecure;
            this.ServerPath = ServerPath;

            this.PingInterval = PingInterval;
            this.PingTimeout = PingTimeout;

            Initialize();
        }

        private void Initialize()
        {
            Action<LogData, string> LogOutput = WebSocketServer?.Log?.Output ?? ((_, __) => { });
            WebSocketServer = new WebSocketServer(IPAddress, Port, IsWebSocketSecure);

            WebSocketServer.Log.Output = LogOutput;
            WebSocketServer.AddWebSocketService(ServerPath, CreateBehavior);
        }

        public void Start()
        {
            SimpleMutex.Lock(ServerMutex, () =>
            {
                if (!IsListening)
                {
                    WebSocketServer.Start();
                }
            });
        }

        public void Close()
        {
            SimpleMutex.Lock(ServerMutex, () =>
            {
                if (IsListening)
                {
                    SimpleMutex.Lock(ClientMutex, () =>
                    {
                        foreach (EngineIOClient Client in ClientList)
                        {
                            Client.Close();
                        }
                    });

                    WebSocketServer.Stop();
                }
            });
        }

        public void Dispose()
        {
            Close();
        }
    }
}
