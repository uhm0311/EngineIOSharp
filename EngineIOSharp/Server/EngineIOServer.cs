using EmitterSharp;
using EngineIOSharp.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    public class EngineIOServer : Emitter<EngineIOServer, string, object>, IDisposable
    {
        private readonly HttpServer Server;
        private readonly ConcurrentDictionary<string, EngineIOClient> _Clients = new ConcurrentDictionary<string, EngineIOClient>();

        public IDictionary<string, EngineIOClient> Clients { get { return new Dictionary<string, EngineIOClient>(_Clients); } }
        public int ClientsCount { get { return _Clients.Count; } }

        public EngineIOServerOption Option { get; private set; }

        public EngineIOServer(EngineIOServerOption Option)
        {
            Server = new HttpServer(Option.Port, Option.Secure);
            
            if ((this.Option = Option).Secure)
            {
                Server.SslConfiguration.ServerCertificate = Option.ServerCertificate;
                Server.SslConfiguration.ClientCertificateValidationCallback = Option.ClientCertificateValidationCallback;
            }
        }

        public EngineIOServer Start()
        {
            Server.Start();

            return this;
        }

        public EngineIOServer Stop()
        {
            foreach (EngineIOClient Client in _Clients.Values)
            {
                Client.Close();
            }

            Server.Stop();

            return this;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
