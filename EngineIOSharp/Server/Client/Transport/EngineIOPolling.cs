using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WebSocketSharp.Net;
using Timer = System.Timers.Timer;

namespace EngineIOSharp.Server.Client.Transport
{
    internal class EngineIOPolling : EngineIOTransport
    {
        private readonly string Origin;

        private readonly Timer ConnectionTimer;
        private readonly Semaphore Semaphore = new Semaphore(0, 1);

        private HttpListenerRequest PollRequest;
        private HttpListenerResponse PollResponse;

        private HttpListenerRequest DataRequest;
        private HttpListenerResponse DataResponse;

        private Action ShouldClose;
        private EngineIOTimeout CloseTimer;

        public EngineIOPolling(HttpListenerRequest Request, int Protocol) : base(Protocol)
        {
            Origin = EngineIOHttpManager.GetOrigin(Request.Headers);
            ForceBase64 = Protocol == 4 || (int.TryParse(Request.QueryString["b64"]?.Trim() ?? string.Empty, out int Base64) && Base64 > 0);

            ConnectionTimer = new Timer(1) { AutoReset = true };
            ConnectionTimer.Elapsed += (_, __) =>
            {
                if (PollResponse?.IsDisconnected ?? false)
                {
                    OnPollRequestClose(new SocketException());
                }
            };

            ConnectionTimer.Start();
        }

        protected override void CloseInternal(Action Callback)
        {
            void OnClose()
            {
                CloseTimer?.Stop();
                Callback?.Invoke();
                this.OnClose();
            }

            if (DataResponse != null)
            {
                try
                {
                    DataResponse.Headers = SetHeaders(DataResponse.Headers);
                    DataResponse.Close();
                }
                catch
                {

                }
            }

            DataRequest = null;
            DataResponse = null;

            if (Writable)
            {
                Send(EngineIOPacket.CreateClosePacket().Encode(EngineIOTransportType.polling, ForceBase64, Protocol: Protocol));
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

            ConnectionTimer.Stop();
        }

        internal override EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            if (Packets != null)
            {
                Writable = false;

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    void SendString()
                    {
                        StringBuilder EncodedPackets = new StringBuilder();

                        for (int i = 0; i < Packets.Length; i++)
                        {
                            EncodedPackets.Append(Packets[i].Encode(EngineIOTransportType.polling, ForceBase64, Protocol: Protocol));

                            if (Protocol == 4 && i < Packets.Length - 1)
                            {
                                EncodedPackets.Append(EngineIOPacket.Seperator);
                            }
                        }

                        Send(EncodedPackets.ToString());
                    }

                    try
                    {
                        bool DoClose = ShouldClose != null;
                        Semaphore.WaitOne();

                        if (DoClose)
                        {
                            ShouldClose();
                            ShouldClose = null;
                        }

                        if (ForceBase64)
                        {
                            SendString();
                        }
                        else
                        {
                            bool IsBinary = false;

                            foreach (EngineIOPacket Packet in Packets)
                            {
                                if (Packet.IsBinary)
                                {
                                    IsBinary = true;
                                    break;
                                }
                            }

                            if (IsBinary)
                            {
                                List<byte> EncodedPackets = new List<byte>();

                                foreach (EngineIOPacket Packet in Packets)
                                {
                                    EncodedPackets.AddRange(Packet.Encode(EngineIOTransportType.polling, ForceBase64, IsBinary, Protocol) as byte[]);
                                }

                                Send(EncodedPackets.ToArray());
                            }
                            else
                            {
                                SendString();
                            }
                        }
                    }
                    catch (Exception Exception)
                    {
                        OnError("Polling not sent.", Exception);
                    }
                });
            }

            return this;
        }

        private void Send(object EncodedPacket, Action<Exception> OnException = null)
        {
            using (PollResponse)
            {
                if (PollResponse != null)
                {
                    try
                    {
                        byte[] RawData = null;

                        if (EncodedPacket is string)
                        {
                            RawData = Encoding.UTF8.GetBytes(EncodedPacket as string);
                        }
                        else if (EncodedPacket is byte[])
                        {
                            RawData = EncodedPacket as byte[];
                        }

                        if ((RawData?.Length ?? 0) > 0)
                        {
                            PollResponse.Headers = SetHeaders(PollResponse.Headers);

                            using (PollResponse.OutputStream)
                            {
                                PollResponse.KeepAlive = false;

                                PollResponse.ContentType = EncodedPacket is string ? "text/plain" : "application/octet-stream";
                                PollResponse.ContentEncoding = EncodedPacket is string ? Encoding.UTF8 : null;
                                PollResponse.ContentLength64 = RawData.Length;

                                PollResponse.OutputStream.Write(RawData, 0, RawData.Length);
                            }
                        }
                    }
                    catch (Exception Exception)
                    {
                        if (!(Exception is NullReferenceException))
                        {
                            OnException?.Invoke(Exception);
                            CloseResponse(PollResponse);
                        }
                    }
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

                    Semaphore.Release();
                    Writable = true;
                    Emit(Event.DRAIN);

                    if (Writable && ShouldClose != null)
                    {
                        Send(EngineIOPacket.CreateNoopPacket().Encode(EngineIOTransportType.polling, ForceBase64, Protocol: Protocol), OnPollRequestClose);
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
                        EngineIOPacket[] Packets = EngineIOPacket.Decode(Request.InputStream, EngineIOHttpManager.IsBinary(Request.ContentType), Protocol);
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
