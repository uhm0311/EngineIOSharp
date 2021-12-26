using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace EngineIOSharp.Client.Transport
{
    internal class EngineIOPolling : EngineIOTransport
    {
        private readonly Dictionary<EngineIOHttpMethod, Semaphore> Semaphores = new Dictionary<EngineIOHttpMethod, Semaphore>();

        private readonly CookieContainer Cookies = new CookieContainer();
        private bool Polling = false;

        public EngineIOPolling(EngineIOClientOption Option) : base(Option) 
        {
            Semaphores.Add(EngineIOHttpMethod.GET, new Semaphore(0, 1));
            Semaphores.Add(EngineIOHttpMethod.POST, new Semaphore(0, 1));

            foreach (Semaphore Semaphore in Semaphores.Values)
            {
                Semaphore.Release();
            }
        }

        public void Pause(Action Callback)
        {
            byte Total = 0;
            ReadyState = EngineIOReadyState.PAUSING;

            void Pause()
            {
                ReadyState = EngineIOReadyState.PAUSED;
                Callback();
            }

            void OnPause()
            {
                if (--Total == 0)
                {
                    Pause();
                }
            }

            if (Polling || !Writable)
            {
                if (Polling)
                {
                    Total++;
                    Once(Event.POLL_COMPLETE, OnPause);
                }

                if (!Writable)
                {
                    Total++;
                    Once(Event.DRAIN, OnPause);
                }
            }
            else
            {
                Pause();
            }
        }

        private void Poll()
        {
            Polling = true;

            Request(ErrorCallback: (Exception) => OnError("Poll error", Exception));
            Emit(Event.POLL);
        }

        protected override void OpenInternal()
        {
            Request();
        }

        protected override void CloseInternal()
        {
            void OnClose() => Send(EngineIOPacket.CreateClosePacket());

            if (ReadyState == EngineIOReadyState.OPEN)
            {
                OnClose();
            }
            else
            {
                Once(Event.OPEN, OnClose);
            }
        }

        protected override void SendInternal(EngineIOPacket Packet)
        {
            if (Packet != null)
            {
                Request(EngineIOHttpMethod.POST, Packet.Encode(EngineIOTransportType.polling, Option.ForceBase64, Protocol: Protocol), (Exception) => OnError("Post error", Exception));
            }
        }

        private void Request(EngineIOHttpMethod Method = EngineIOHttpMethod.GET, object EncodedPacket = null, Action<Exception> ErrorCallback = null)
        {
            Semaphores[Method].WaitOne();

            try
            {
                HttpWebRequest Request = CreateRequest(Method);

                if (EncodedPacket != null)
                {
                    using (Stream Stream = Request.GetRequestStream())
                    {
                        byte[] RawData = null;

                        if (EncodedPacket is string)
                        {
                            Request.Headers["Content-Encoding"] = "utf8";
                            RawData = Encoding.UTF8.GetBytes(EncodedPacket as string);
                        }
                        else if (EncodedPacket is byte[])
                        {
                            RawData = EncodedPacket as byte[];
                        }

                        if (RawData != null)
                        {
                            Request.ContentType = EncodedPacket is string ? "text/plain" : "application/octet-stream";
                            Stream.Write(RawData, 0, RawData.Length);
                        }
                    }
                }

                HttpWebResponse Response = null;
                Exception ResponseException = null;

                try
                {
                    Response = Request.GetResponse() as HttpWebResponse;
                }
                catch (Exception Exception)
                {
                    ResponseException = Exception;
                }
                finally
                {
                    Semaphores[Method].Release();
                }

                if (ResponseException != null)
                {
                    throw ResponseException;
                }
                else if (Response != null)
                {
                    HandleResponse(Method, Response);
                }
            }
            catch (Exception Exception)
            {
                if (ErrorCallback == null)
                {
                    OnError("Error", Exception);
                }
                else
                {
                    ErrorCallback(Exception);
                }
            }
        }

        private HttpWebRequest CreateRequest(EngineIOHttpMethod Method)
        {
            StringBuilder URL = new StringBuilder();
            URL.Append(string.Format("{0}://{1}:{2}{3}", Option.Scheme, Option.Host, Option.Port, Option.Path)).Append('?');

            foreach (string Key in new List<string>(Option.Query.Keys))
            {
                URL.Append(Key).Append('=').Append(Option.Query[Key]).Append('&');
            }

            if (Option.ForceBase64)
            {
                URL.Append("b64=1&");
            }

            URL.Append("transport=polling");

            if (Option.TimestampRequests ?? true)
            {
                URL.Append(string.Format("&{0}={1}", Option.TimestampParam, EngineIOTimestamp.Generate()));
            }

            HttpWebRequest Request = WebRequest.Create(URL.ToString()) as HttpWebRequest;
            Request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Request.Timeout = Option.PollingTimeout == 0 ? Timeout.Infinite : Option.PollingTimeout;
            Request.ServicePoint.Expect100Continue = false;
            Request.Method = Method.ToString();
            Request.CookieContainer = Cookies;
            Request.KeepAlive = false;

            if (Option.WithCredentials)
            {
#if !NET40
                Request.ServerCertificateValidationCallback = Option.ServerCertificateValidationCallback;
#endif

                if (Option.ClientCertificates != null)
                {
                    Request.ClientCertificates = Option.ClientCertificates;
                }
            }

            if (Option.ExtraHeaders.Count > 0)
            {
                foreach (string Key in new List<string>(Option.ExtraHeaders.Keys))
                {
                    try
                    {
                        bool IsAutorization = Key.ToLower().Trim().Equals("authorization");

                        if (!IsAutorization || (IsAutorization && Option.WithCredentials))
                        {
                            Request.Headers.Add(Key, Option.ExtraHeaders[Key]);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return Request;
        }

        private void HandleResponse(EngineIOHttpMethod Method, HttpWebResponse Response)
        {
            using (Response)
            {
                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    if (Option.WithCredentials)
                    {
                        Cookies.Add(Response.Cookies);
                    }

                    if (Method == EngineIOHttpMethod.GET)
                    {
                        List<EngineIOPacket> Packets = new List<EngineIOPacket>();

                        if (Response != null)
                        {
                            if (Response.StatusCode == HttpStatusCode.OK)
                            {
                                Packets.AddRange(EngineIOPacket.Decode(Response.GetResponseStream(), EngineIOHttpManager.IsBinary(Response.ContentType), Protocol));
                            }
                            else
                            {
                                Packets.Add(EngineIOPacket.CreateErrorPacket());
                            }
                        }

                        if (Packets.Count > 0)
                        {
                            foreach (EngineIOPacket Packet in Packets)
                            {
                                if (Packet.Type != EngineIOPacketType.CLOSE)
                                {
                                    if (ReadyState == EngineIOReadyState.OPENING)
                                    {
                                        OnOpen();
                                    }

                                    OnPacket(Packet);

                                    if (Packet.Type == EngineIOPacketType.OPEN)
                                    {
                                        Writable = true;
                                    }
                                }
                                else
                                {
                                    OnClose();
                                }
                            }

                            if (ReadyState != EngineIOReadyState.CLOSED)
                            {
                                Polling = false;
                                Emit(Event.POLL_COMPLETE);

                                if (ReadyState == EngineIOReadyState.OPEN)
                                {
                                    ThreadPool.QueueUserWorkItem((_) => Poll());
                                }
                            }
                        }
                    }
                    else if (Method == EngineIOHttpMethod.POST)
                    {
                        Writable = true;
                        Emit(Event.DRAIN);
                    }
                }
            }
        }
    }
}
