using EngineIOSharp.Common;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp.Net;

namespace EngineIOSharp.Server
{
    public class EngineIOServerOption
    {
        public ushort Port { get; private set; }
        public bool Secure { get; private set; }
        public string Path { get; private set; }

        public ulong PingTimeout { get; private set; }
        public ulong PingInterval { get; private set; }
        public ulong UpgradeTimeout { get; private set; }

        public bool Polling { get; private set; }
        public bool WebSocket { get; private set; }
        public bool AllowUpgrade { get; private set; }

        public bool UseCookie { get; private set; }
        public string SIDName { get; private set; }
        internal IDictionary<string, string> Cookie { get; private set; }

        public Action<HttpListenerRequest, Action<EngineIOException>> AllowRequest { get; private set; }
        public object InitialData { get; private set; }

        public X509Certificate2 ServerCertificate { get; private set; }
        public RemoteCertificateValidationCallback ClientCertificateValidationCallback { get; private set; }

        /// <summary>
        /// Options for Engine.IO server.
        /// </summary>
        /// <param name="Port">Port to listen.</param>
        /// <param name="Path">Path to listen.</param>
        /// <param name="Secure">Whether to secure connections.</param>
        /// <param name="PingTimeout">How many ms without a pong packet to consider the connection closed.</param>
        /// <param name="PingInterval">How many ms before sending a new ping packet.</param>
        /// <param name="UpgradeTimeout">How many ms before an uncompleted transport upgrade is cancelled</param>
        /// <param name="Polling">Whether to accept polling transport.</param>
        /// <param name="WebSocket">Whether to accept websocket transport.</param>
        /// <param name="AllowUpgrade">Whether to allow transport upgrade.</param>
        /// <param name="UseCookie">Whether to use cookie.</param>
        /// <param name="SIDName">Name of sid cookie.</param>
        /// <param name="Cookie">Configuration of the cookie that contains the client sid to send as part of handshake response headers. This cookie might be used for sticky-session.</param>
        /// <param name="AllowRequest">A function that receives a given handshake or upgrade request as its first parameter, and can decide whether to continue or not.</param>
        /// <param name="InitialData">An optional packet which will be concatenated to the handshake packet emitted by Engine.IO.</param>
        /// <param name="ServerCertificate">The certificate used to authenticate the server.</param>
        /// <param name="ClientCertificateValidationCallback">Callback used to validate the certificate supplied by the client.</param>
        public EngineIOServerOption(ushort Port, string Path = "/engine.io", bool Secure = false, ulong PingTimeout = 5000, ulong PingInterval = 25000, ulong UpgradeTimeout = 10000, bool Polling = true, bool WebSocket = true, bool AllowUpgrade = true, bool UseCookie = true, string SIDName = "io", IDictionary<string, string> Cookie = null, Action<HttpListenerRequest, Action<EngineIOException>> AllowRequest = null, object InitialData = null, X509Certificate2 ServerCertificate = null, RemoteCertificateValidationCallback ClientCertificateValidationCallback = null)
        {
            this.Port = Port;
            this.Path = EngineIOOption.PolishPath(Path);
            this.Secure = Secure;

            this.PingTimeout = PingTimeout;
            this.PingInterval = PingInterval;
            this.UpgradeTimeout = UpgradeTimeout;

            this.Polling = Polling;
            this.WebSocket = WebSocket;
            this.AllowUpgrade = AllowUpgrade;

            this.UseCookie = UseCookie;
            this.SIDName = SIDName;
            this.Cookie = new Dictionary<string, string>();

            this.AllowRequest = AllowRequest;
            this.InitialData = InitialData;

            this.ServerCertificate = ServerCertificate;
            this.ClientCertificateValidationCallback = ClientCertificateValidationCallback ?? EngineIOOption.DefaultCertificateValidationCallback;

            if (UseCookie)
            {
                this.Cookie["Path"] = this.Path;
                this.Cookie["HttpOnly"] = "";
                this.Cookie["SameSite"] = "Lax";

                if (Cookie != null)
                {
                    foreach (string Key in Cookie.Keys)
                    {
                        this.Cookie[Key] = Cookie[Key] ?? this.Cookie[Key];
                    }
                }
            }
        }
    }
}
