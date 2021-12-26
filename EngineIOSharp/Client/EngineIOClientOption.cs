using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace EngineIOSharp.Client
{
    public class EngineIOClientOption
    {
        public EngineIOScheme Scheme { get; private set; }
        public string Host { get; private set; }
        public ushort Port { get; private set; }
        public ushort PolicyPort { get; private set; }

        public string Path { get; private set; }
        internal IDictionary<string, string> Query { get; private set; }
        
        public bool Upgrade { get; private set; }
        public bool RemeberUpgrade { get; private set; }
        public bool ForceBase64 { get; private set; }

        public bool WithCredentials { get; private set; }
        public bool? TimestampRequests { get; private set; }
        public string TimestampParam { get; private set; }

        public bool Polling { get; private set; }
        public int PollingTimeout { get; private set; }

        public bool WebSocket { get; private set; }
        public string[] WebSocketSubprotocols { get; private set; }

        internal IDictionary<string, string> ExtraHeaders { get; private set; }

        public X509CertificateCollection ClientCertificates { get; private set; }
        public LocalCertificateSelectionCallback ClientCertificateSelectionCallback { get; private set; }
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; private set; }

        /// <summary>
        /// Options for Engine.IO client.
        /// </summary>
        /// <param name="Scheme">Scheme to connect to.</param>
        /// <param name="Host">Host to connect to.</param>
        /// <param name="Port">Port to connect to.</param>
        /// <param name="PolicyPort">Port the policy server listens on.</param>
        /// <param name="Path">Path to connect to.</param>
        /// <param name="Query">Parameters that will be passed for each request to the server.</param>
        /// <param name="Upgrade">Whether the client should try to upgrade the transport.</param>
        /// <param name="RemeberUpgrade">Whether the client should bypass normal upgrade process when previous websocket connection is succeeded.</param>
        /// <param name="ForceBase64">Forces base 64 encoding for transport.</param>
        /// <param name="WithCredentials">Whether to include credentials such as cookies, authorization headers, TLS client certificates, etc. with polling requests.</param>
        /// <param name="TimestampRequests">Whether to add the timestamp with each transport request. Polling requests are always stamped.</param>
        /// <param name="TimestampParam">Timestamp parameter.</param>
        /// <param name="Polling">Whether to include polling transport.</param>
        /// <param name="PollingTimeout">Timeout for polling requests in milliseconds.</param>
        /// <param name="WebSocket">Whether to include websocket transport.</param>
        /// <param name="WebSocketSubprotocols">List of <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers#Subprotocols">websocket subprotocols</see>.</param>
        /// <param name="ExtraHeaders">Headers that will be passed for each request to the server.</param>
        /// <param name="ClientCertificates">The collection of security certificates that are associated with each request.</param>
        /// <param name="ClientCertificateSelectionCallback">Callback used to select the certificate to supply to the server.</param>
        /// <param name="ServerCertificateValidationCallback">Callback method to validate the server certificate.</param>
        public EngineIOClientOption(EngineIOScheme Scheme, string Host, ushort Port, ushort PolicyPort = 843, string Path = "/engine.io", IDictionary<string, string> Query = null, bool Upgrade = true, bool RemeberUpgrade = false, bool ForceBase64 = false, bool WithCredentials = true, bool? TimestampRequests = null, string TimestampParam = "t", bool Polling = true, int PollingTimeout = 0, bool WebSocket = true, string[] WebSocketSubprotocols = null, IDictionary<string, string> ExtraHeaders = null, X509CertificateCollection ClientCertificates = null, LocalCertificateSelectionCallback ClientCertificateSelectionCallback = null, RemoteCertificateValidationCallback ServerCertificateValidationCallback = null)
        {
            this.Scheme = Scheme;
            this.Host = Host;
            this.Port = Port;
            this.PolicyPort = PolicyPort;

            this.Path = EngineIOOption.PolishPath(Path);
            this.Query = new Dictionary<string, string>(Query ?? new Dictionary<string, string>());

            this.Upgrade = Upgrade;
            this.RemeberUpgrade = RemeberUpgrade;
            this.ForceBase64 = ForceBase64;

            this.WithCredentials = WithCredentials;
            this.TimestampRequests = TimestampRequests;
            this.TimestampParam = TimestampParam;

            this.Polling = Polling;
            this.PollingTimeout = PollingTimeout;

            this.WebSocket = WebSocket;
            this.WebSocketSubprotocols = WebSocketSubprotocols ?? new string[0];

            this.ExtraHeaders = new Dictionary<string, string>(ExtraHeaders ?? new Dictionary<string, string>());

            this.ClientCertificates = ClientCertificates;
            this.ClientCertificateSelectionCallback = ClientCertificateSelectionCallback ?? DefaultClientCertificateSelectionCallback;
            this.ServerCertificateValidationCallback = ServerCertificateValidationCallback ?? EngineIOOption.DefaultCertificateValidationCallback;

            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new ArgumentException("Host is not valid.", "Host");
            }

            if (!Polling && !WebSocket)
            {
                throw new ArgumentException("Either Polling or WebSocket must be used as transport.", "Polling, WebSocket");
            }

            if (!this.Query.ContainsKey("EIO"))
            {
                this.Query.Add("EIO", "4");
            }

            if (this.Query.ContainsKey("transport"))
            {
                this.Query.Remove("transport");
            }

            if (this.Query.ContainsKey("j"))
            {
                this.Query.Remove("j");
            }

            if (this.Query.ContainsKey("b64"))
            {
                this.Query.Remove("b64");
            }
        }

        private static X509Certificate DefaultClientCertificateSelectionCallback(object _1, string _2, X509CertificateCollection _3, X509Certificate _4, string[] _5)
        {
            return null;
        }
    }
}
