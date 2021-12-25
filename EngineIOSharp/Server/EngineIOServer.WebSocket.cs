using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Static;
using EngineIOSharp.Server.Client;
using EngineIOSharp.Server.Client.Transport;
using System;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private EngineIOBehavior CreateBehavior()
        {
            return new EngineIOBehavior(OnWebSocket);
        }

        private void OnWebSocket(WebSocketContext Context)
        {
            Verify(Context, (Exception) =>
            {
                if (Exception == null)
                {
                    Handshake(EngineIOHttpManager.GetTransport(Context.QueryString), Context);
                }
                else
                {
                    Context.WebSocket.Close(CloseStatusCode.Abnormal, Exception.Message);
                }
            });
        }

        private void Verify(WebSocketContext Context, Action<EngineIOException> Callback)
        {
            EngineIOException Return = null;
            bool AllowWebSocket = false;

            try
            {
                if ((Return = Verify(Context.QueryString, Context.Headers, EngineIOTransportType.websocket)) == null)
                {
                    string SID = EngineIOHttpManager.GetSID(Context.QueryString);

                    if (!string.IsNullOrEmpty(SID))
                    {
                        if (_Clients.TryGetValue(SID, out EngineIOSocket Socket))
                        {
                            if (Socket.Transport is EngineIOPolling && Option.AllowUpgrade && Option.WebSocket && !(Socket.Upgrading || Socket.Upgraded))
                            {
                                if (!(Socket.Upgrading || Socket.Upgraded))
                                {
                                    Socket.UpgradeTransport(new EngineIOWebSocket(Context, EngineIOHttpManager.GetProtocol(Context.QueryString)));
                                    AllowWebSocket = true;
                                }
                                else
                                {
                                    Return = Exceptions.BAD_REQUEST;
                                }
                            }
                            else
                            {
                                Return = Exceptions.BAD_REQUEST;
                            }
                        }
                        else
                        {
                            Return = Exceptions.UNKNOWN_SID;
                        }
                    }
                    else
                    {
                        if (Option.AllowWebSocket != null)
                        {
                            AllowWebSocket = true;
                            Option.AllowWebSocket(Context, Callback);
                        }
                    }
                }
            }
            catch (Exception Exception)
            {
                EngineIOLogger.Error(this, Return = new EngineIOException("Unknown exception", Exception));
            }
            finally
            {
                if (!AllowWebSocket)
                {
                    Callback(Return);
                }
            }
        }

        private void Handshake(string TransportName, WebSocketContext Context)
        {
            void OnError()
            {
                Context.WebSocket.Close(CloseStatusCode.Abnormal, Exceptions.BAD_REQUEST.Message);
            }

            try
            {
                if (EngineIOHttpManager.IsWebSocket(TransportName))
                {
                    Handshake(Context.QueryString["sid"] ?? EngineIOSocketID.Generate(), new EngineIOWebSocket(Context, EngineIOHttpManager.GetProtocol(Context.QueryString)));
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

        private class EngineIOBehavior : WebSocketBehavior
        {
            private readonly Action<WebSocketContext> Initializer;

            internal EngineIOBehavior(Action<WebSocketContext> Initializer)
            {
                this.Initializer = Initializer;
            }

            protected override void OnOpen()
            {
                if (ID != null && Sessions[ID]?.Context != null)
                {
                    Initializer(Sessions[ID].Context);
                }
            }
        }
    }
}
