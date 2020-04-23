using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.Text;
using WebSocketSharp.Net;

namespace EngineIOSharp.Server.Client.Transport
{
    internal class EngineIOPolling : EngineIOTransport
    {
        public static readonly string Name = "polling";

        private HttpListenerRequest PollRequest;
        private HttpListenerResponse PollResponse;

        private HttpListenerRequest DataRequest;
        private HttpListenerResponse DataResponse;

        private Action ShouldClose;
        private EngineIOTimeout CloseTimer;

        private readonly string Origin;

        public EngineIOPolling(HttpListenerRequest Request)
        {
            Origin = Request.Headers["Origin"]?.Trim() ?? string.Empty;
        }

        protected override void CloseInternal(Action Callback)
        {
            void OnClose()
            {
                CloseTimer?.Stop();
                Callback?.Invoke();
                this.OnClose();
            }

            DataRequest = null;
            DataResponse?.Close(); DataResponse = null;

            if (Writable)
            {
                Send(EngineIOPacket.CreateClosePacket());
                OnClose();
            }
            else if (Discarded)
            {
                OnClose();
            } 
            else
            {
                ShouldClose = OnClose;
                CloseTimer = new EngineIOTimeout(OnClose, 30 * 1000);
            }
        }

        internal override EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            if (Packets != null)
            {
                bool DoClose = ShouldClose != null;
                Writable = false;

                if (DoClose)
                {
                    ShouldClose();
                    ShouldClose = null;
                }

                StringBuilder EncodedPackets = new StringBuilder();

                foreach (EngineIOPacket Packet in Packets)
                {
                    EncodedPackets.Append(Packet.Encode(true));
                }

                Send(EncodedPackets.ToString());
            }

            return this;
        }

        private void Send(string EncodedPacket, Action<Exception> OnException = null)
        {
            using (PollResponse)
            {
                try
                {
                    byte[] RawData = Encoding.UTF8.GetBytes(EncodedPacket);
                    PollResponse.Headers = SetHeaders(PollResponse.Headers);

                    using (PollResponse.OutputStream)
                    {
                        PollResponse.KeepAlive = false;

                        PollResponse.ContentType = "text/plain; charset=UTF-8";
                        PollResponse.ContentEncoding = Encoding.UTF8;
                        PollResponse.ContentLength64 = RawData.Length;

                        PollResponse.OutputStream.Write(RawData, 0, RawData.Length);
                    }
                }
                catch (Exception Exception)
                {
                    OnException?.Invoke(Exception);
                    CloseResponse(PollResponse);
                }
            }

            CleanupPollRequest();
        }

        private WebHeaderCollection SetHeaders(WebHeaderCollection Headers)
        {
            string UserAgent = EngineIOHttpManager.GetUserAgent(Headers);

            if (UserAgent.Contains(";MSIE") || UserAgent.Contains("Trident/"))
            {
                Headers["X-XSS-Protection"] = "0";
            }

            if (!string.IsNullOrEmpty(Origin))
            {
                Headers["Access-Control-Allow-Credentials"] = "true";
                Headers["Access-Control-Allow-Origin"] = Origin;
            }
            else
            {
                Headers["Access-Control-Allow-Origin"] = "*";
            }

            Emit(Event.HEADERS, Headers);
            return Headers;
        }

        private void CloseResponse(HttpListenerResponse Response)
        {
            try
            {
                if (Response != null)
                {
                    using (Response)
                    {
                        Response.StatusCode = 500;
                    }
                }
            }
            catch
            {

            }
        }

        private void CleanupPollRequest()
        {
            PollRequest = null;
            PollResponse = null;
        }

        private void CleanupDataRequest()
        {
            DataRequest = null;
            DataResponse = null;
        }

        internal override EngineIOTransport OnRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            EngineIOHttpMethod Method = EngineIOHttpManager.ParseMethod(Request.HttpMethod);

            if (Method == EngineIOHttpMethod.GET)
            {
                OnPollRequest(Request, Response);
            }
            else if (Method == EngineIOHttpMethod.POST)
            {
                OnDataRequest(Request, Response);
            }
            else
            {
                CloseResponse(Response);
            }

            return this;
        }

        private void OnPollRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            try
            {
                if (PollRequest == null)
                {
                    PollRequest = Request;
                    PollResponse = Response;

                    Writable = true;
                    Emit(Event.DRAIN);

                    if (Writable && ShouldClose != null)
                    {
                        Send(EngineIOPacket.CreateNoopPacket().Encode(true) as string, OnPollRequestClose);
                    }
                }
                else
                {
                    throw new EngineIOException("Overlap from client");
                }
            }
            catch (Exception Exception)
            {
                CloseResponse(Response);
                OnPollRequestClose(Exception);
            }
        }

        private void OnPollRequestClose(Exception Exception)
        {
            CleanupPollRequest();
            OnError("Poll connection closed prematurely.", Exception);
        }

        private void OnDataRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            using (Response)
            {
                try
                {
                    if (DataRequest == null)
                    {
                        EngineIOPacket[] Packets = EngineIOPacket.Decode(Request);
                        Response.Headers = SetHeaders(Response.Headers);

                        using (Response.OutputStream)
                        {
                            Response.KeepAlive = false;

                            Response.ContentType = "text/html";
                            Response.ContentEncoding = Encoding.UTF8;
                            Response.ContentLength64 = 2;

                            Response.OutputStream.Write(Encoding.UTF8.GetBytes("ok"), 0, 2);
                        }

                        foreach (EngineIOPacket Packet in Packets)
                        {
                            if (Packet.Type != EngineIOPacketType.CLOSE)
                            {
                                OnPacket(Packet);
                            }
                            else
                            {
                                OnClose();
                            }
                        }

                        CleanupDataRequest();
                    }
                    else
                    {
                        throw new EngineIOException("Data request overlap from client");
                    }
                }
                catch (Exception Exception)
                {
                    CloseResponse(Response);
                    OnDataRequestClose(Exception);
                }
            }
        }

        private void OnDataRequestClose(Exception Exception)
        {
            CleanupDataRequest();
            OnError("Data request connection closed prematurely", Exception);
        }
    }
}
