using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Static;
using EngineIOSharp.Server.Client;
using EngineIOSharp.Server.Client.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            Server = new HttpServer(Option.Port, Option.Secure) { AutoClose = false };
            Server.OnGet += OnHttpRequest;
            Server.OnPost += OnHttpRequest;
            Server.OnConnect += OnHttpRequest;
            Server.OnDelete += OnHttpRequest;
            Server.OnHead += OnHttpRequest;
            Server.OnOptions += OnHttpRequest;
            Server.OnPatch += OnHttpRequest;
            Server.OnPut += OnHttpRequest;
            Server.OnTrace += OnHttpRequest;
#pragma warning disable CS0618
            Server.AddWebSocketService(Option.Path, CreateBehavior);
#pragma warning restore CS0618

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

        private EngineIOException Verify(NameValueCollection QueryString, NameValueCollection Headers, EngineIOTransportType ExpectedTransportType)
        {
            EngineIOException Exception = Exceptions.UNKNOWN_TRANSPORT;

            if (QueryString["EIO"] == "3" && !Option.AllowEIO3)
            {
                Exception = Exceptions.UNSUPPORTED_PROTOCOL_VERSION;
            }
            else if (EngineIOHttpManager.GetTransport(QueryString).Equals(ExpectedTransportType.ToString()))
            {
                bool IsPolling = EngineIOHttpManager.IsPolling(QueryString) && Option.Polling;
                bool IsWebSocket = EngineIOHttpManager.IsWebSocket(QueryString) && Option.WebSocket;

                if (IsPolling || IsWebSocket)
                {
                    if (EngineIOHttpManager.IsValidHeader(EngineIOHttpManager.GetOrigin(Headers)))
                    {
                        Exception = null;
                    }
                    else
                    {
                        Exception = Exceptions.BAD_REQUEST;
                    }
                }
            }

            return Exception;
        }

        private void Handshake(string SID, EngineIOTransport Transport)
        {
            if (Option.SetCookie)
            {
                Transport.On(EngineIOTransport.Event.HEADERS, (Headers) =>
                {
                    List<string> Cookies = new List<string>();

                    foreach (string Key in Option.Cookies.Keys)
                    {
                        string Cookie = Key;
                        string Value = Option.Cookies[Key];

                        if (!string.IsNullOrWhiteSpace(Value))
                        {
                            Cookie += ('=' + Value);
                        }

                        Cookies.Add(Cookie);
                    }

                    (Headers as NameValueCollection)["Set-Cookie"] = string.Join("; ", Cookies);
                });
            }

            EngineIOSocket Socket = new EngineIOSocket(SID, this, Transport);
            _Clients.TryAdd(SID, Socket.Once(EngineIOSocket.Event.CLOSE, () =>
            {
                _Clients.TryRemove(SID, out _);
            }));

            Emit(Event.CONNECTION, Socket);
        }
    }
}
