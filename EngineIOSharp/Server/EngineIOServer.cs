using EngineIOSharp.Abstract;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    public partial class EngineIOServer : EngineIOConnection
    {
        private WebSocketServer Server = null;
        private readonly object ServerMutex = new object();

        public bool IsSecure
        {
            get { return Server?.IsSecure ?? false; }
        }

        public EngineIOServer(int PingInterval = 25000, int PingTimeout = 5000)
        {
            this.PingInterval = PingInterval;
            this.PingTimeout = PingTimeout;
        }

        public void Open(IPAddress IPAddress, int Port, bool IsSecure)
        {
            lock (ServerMutex)
            {
                if (Server == null)
                {
                    Server = new WebSocketServer(IPAddress, Port, IsSecure);
                    Server.Start();
                }
            }
        }

        public override void Close()
        {
            lock (ServerMutex)
            {
                Server?.Stop();
                Server = null;
            }
        }
    }
}
