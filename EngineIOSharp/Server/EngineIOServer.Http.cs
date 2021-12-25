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
        private void Verify(HttpListenerRequest Request, Action<EngineIOException> Callback)
        {
            EngineIOException Return = null;
            bool AllowHttpRequest = false;

            try
            {
                if ((Return = Verify(Request.QueryString, Request.Headers, EngineIOTransportType.polling)) == null)
                {
                    string SID = EngineIOHttpManager.GetSID(Request.QueryString);
                    bool Contains = _Clients.ContainsKey(SID);

                    if (string.IsNullOrEmpty(SID) || Contains)
                    {
                        if (Contains && !(_Clients[SID].Transport is EngineIOPolling))
                        {
                            Return = Exceptions.BAD_REQUEST;
                        }
                        else if (EngineIOHttpManager.ParseMethod(Request.HttpMethod) == EngineIOHttpMethod.GET)
                        {
                            if (Option.AllowHttpRequest != null)
                            {
                                AllowHttpRequest = true;
                                Option.AllowHttpRequest(Request, Callback);
                            }
                        }
                        else if (string.IsNullOrEmpty(SID))
                        {
                            Return = Exceptions.BAD_HANDSHAKE_METHOD;
                        }
                    }
                    else
                    {
                        Return = Exceptions.UNKNOWN_SID;
                    }
                }
            }
            catch (Exception Exception)
            {
                EngineIOLogger.Error(this, Return = new EngineIOException("Unknown exception", Exception));
            }
            finally
            {
                if (!AllowHttpRequest)
                {
                    Callback(Return);
                }
            }
        }

        private void OnHttpRequest(object sender, HttpRequestEventArgs e)
        {
            Verify(e.Request, (Exception) =>
            {
                if (Exception == null)
                {
                    string SID = EngineIOHttpManager.GetSID(e.Request.QueryString);

                    if (_Clients.TryGetValue(SID, out EngineIOSocket Client))
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

        private void Handshake(string TransportName, HttpListenerRequest Request, HttpListenerResponse Response)
        {
            void OnError()
            {
                EngineIOHttpManager.SendErrorMessage(Request, Response, Exceptions.BAD_REQUEST);
            }

            try
            {
                if (EngineIOHttpManager.IsPolling(TransportName))
                {
                    EngineIOTransport Transport = new EngineIOPolling(Request);
                    Transport.OnRequest(Request, Response);

                    Handshake(EngineIOSocketID.Generate(), Transport, Request.QueryString["EIO"] == "4" ? 4 : 3);
                }
                else
                {
                    OnError();
                }
            }
            catch (Exception Exception)
            {
                EngineIOLogger.Error(this, Exception);

                OnError();
            }
        }

        internal static class Exceptions
        {
            public static readonly EngineIOException UNKNOWN_TRANSPORT = new EngineIOException("Unknown transport");
            public static readonly EngineIOException UNKNOWN_SID = new EngineIOException("Unknown sid");
            public static readonly EngineIOException BAD_HANDSHAKE_METHOD = new EngineIOException("Bad handshake method");
            public static readonly EngineIOException BAD_REQUEST = new EngineIOException("Bad request");
            public static readonly EngineIOException FORBIDDEN = new EngineIOException("Forbidden");
            public static readonly EngineIOException UNSUPPORTED_PROTOCOL_VERSION = new EngineIOException("Unsupported protocol version");

            private static readonly EngineIOException[] VALUES = new EngineIOException[]
            {
                UNKNOWN_TRANSPORT,
                UNKNOWN_SID,
                BAD_HANDSHAKE_METHOD,
                BAD_REQUEST,
                FORBIDDEN,
                UNSUPPORTED_PROTOCOL_VERSION,
            };

            public static bool Contains(EngineIOException Exception)
            {
                return Array.Exists(VALUES, Element => Element.Equals(Exception));
            }

            public static int IndexOf(EngineIOException Exception)
            {
                return Array.IndexOf(VALUES, Exception as EngineIOException);
            }
        }
    }
}
