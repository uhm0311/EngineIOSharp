using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Static;
using EngineIOSharp.Server.Client;
using EngineIOSharp.Server.Client.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    public partial class EngineIOServer : Emitter<EngineIOServer, string, object>, IDisposable
    {
        private readonly HttpServer Server;
        private readonly ConcurrentDictionary<string, EngineIOSocket> _Clients = new ConcurrentDictionary<string, EngineIOSocket>();

        public IDictionary<string, EngineIOSocket> Clients { get { return new Dictionary<string, EngineIOSocket>(_Clients); } }
        public int ClientsCount { get { return _Clients.Count; } }

        public EngineIOServerOption Option { get; private set; }

        public EngineIOServer(EngineIOServerOption Option)
        {
            Server = new HttpServer(Option.Port, Option.Secure);
            Server.OnGet += OnHttpRequest;
            Server.OnPost += OnHttpRequest;
            
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
            foreach (EngineIOSocket Client in _Clients.Values)
            {
                Client.Close(true);
            }

            Server.Stop();

            return this;
        }

        public void Dispose()
        {
            Stop();
        }

        private void Handshake(string TransportName, HttpListenerRequest Request, HttpListenerResponse Response)
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    string SID = EngineIOSocketID.Generate();
                    EngineIOTransport Transport = null;

                    if (TransportName.Equals(EngineIOPolling.Name))
                    {
                        Transport = new EngineIOPolling();
                    }
                    else if (TransportName.Equals(EngineIOWebSocket.Name))
                    {

                    }
                    else
                    {

                    }

                    if (Transport != null)
                    {

                    }
                }
                catch (Exception Exception)
                {
                    EngineIOLogger.Error(this, Exception);

                    EngineIOHttpManager.SendErrorMessage(Request, Response, Exceptions.BAD_REQUEST);
                }
            });
        }
    }
}
