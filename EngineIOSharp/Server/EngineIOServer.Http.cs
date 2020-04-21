using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Static;
using EngineIOSharp.Server.Client;
using EngineIOSharp.Server.Client.Transport;
using System;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private void Verify(HttpListenerRequest Request, bool Upgrade, Action<EngineIOException> Callback)
        {
            EngineIOException Return = null;
            bool AllowRequest = Option.AllowRequest != null;

            try
            {
                bool IsPolling = EngineIOHttpManager.IsPolling(Request) && Option.Polling;
                bool IsWebSocket = EngineIOHttpManager.IsWebSocket(Request) && Option.WebSocket;

                if (IsPolling || IsWebSocket)
                {
                    if (EngineIOHttpManager.IsValidHeader(Request.Headers["Origin"]?.Trim() ?? string.Empty))
                    {
                        string SID = EngineIOHttpManager.GetSID(Request.QueryString);
                        bool Contains = _Clients.ContainsKey(SID);

                        if (string.IsNullOrEmpty(SID) || Contains)
                        {
                            if (Contains)
                            {
                                EngineIOTransport Transport = _Clients[SID].Transport;

                                if (!Upgrade && !(Transport is EngineIOPolling && IsPolling) && !(Transport is EngineIOWebSocket && IsWebSocket))
                                {
                                    Return = Exceptions.BAD_REQUEST;
                                }
                            }
                            else if (EngineIOHttpManager.ParseMethod(Request.HttpMethod) == EngineIOHttpMethod.GET)
                            {
                                Option.AllowRequest?.Invoke(Request, Callback);
                            }
                            else
                            {
                                Return = Exceptions.BAD_HANDSHAKE_METHOD;
                            }
                        }
                        else if (!Contains)
                        {
                            Return = Exceptions.UNKNOWN_SID;
                        }
                    }
                    else
                    {
                        Return = Exceptions.BAD_REQUEST;
                    }
                }
                else
                {
                    Return = Exceptions.UNKNOWN_TRANSPORT;
                }
            }
            catch (Exception Exception)
            {
                EngineIOLogger.Error(this, Return = new EngineIOException("Unknown exception", Exception));
            }

            if (!AllowRequest)
            {
                Callback(Return);
            }
        }

        private void OnHttpRequest(object sender, HttpRequestEventArgs e)
        {
            Verify(e.Request, false, (Exception) =>
            {
                if (Exception == null)
                {
                    if (_Clients.TryGetValue(EngineIOHttpManager.GetSID(e.Request.QueryString), out EngineIOSocket Client))
                    {
                        Client.Transport.OnRequest(e.Request, e.Response);
                    }
                    else
                    {
                        Handshake(EngineIOHttpManager.GetTransport(e.Request.QueryString), e.Request, e.Response);
                    }
                }
                else
                {
                    EngineIOHttpManager.SendErrorMessage(e.Request, e.Response, Exception);
                }
            });
        }

        internal static class Exceptions
        {
            private static readonly EngineIOException[] VALUES = new EngineIOException[]
            {
                UNKNOWN_TRANSPORT,
                BAD_REQUEST,
                UNKNOWN_SID,
                BAD_HANDSHAKE_METHOD,
            };

            public static readonly EngineIOException UNKNOWN_TRANSPORT = new EngineIOException("Unknown transport");
            public static readonly EngineIOException BAD_REQUEST = new EngineIOException("Bad request");
            public static readonly EngineIOException UNKNOWN_SID = new EngineIOException("Unknown sid");
            public static readonly EngineIOException BAD_HANDSHAKE_METHOD = new EngineIOException("Bad handshake method");

            public static bool Contains(Exception Exception)
            {
                return Array.Exists(VALUES, Element => Element.Equals(Exception));
            }

            public static int IndexOf(Exception Exception)
            {
                if (Exception is EngineIOException)
                {
                    return Array.IndexOf(VALUES, Exception as EngineIOException);
                }

                return VALUES.Length;
            }
        }
    }
}
