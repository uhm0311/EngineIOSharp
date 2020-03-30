using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace EngineIOSharp.Client.Transport
{
    internal class EngineIOPolling : EngineIOTransport
    {
        private readonly CookieContainer Cookies = new CookieContainer();
        private bool Polling = false;

        public EngineIOPolling(EngineIOClientOption Option) : base(Option) 
        {
            
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
            if (Packet.IsBinary || Packet.IsText)
            {
                Writable = false;

                StringBuilder Builder = new StringBuilder();
                Builder.Append((int)EngineIOPacketType.MESSAGE);
                Builder.Append(Packet.IsText ? Packet.Data : Convert.ToBase64String(Packet.RawData));
                Builder.Insert(0, string.Format("{0}:b", Encoding.UTF8.GetByteCount(Builder.ToString())));

                Request(HttpMethod.POST, Builder.ToString(), () =>
                {
                    Writable = true;
                    Emit(Event.DRAIN);
                }, (Exception) => OnError("Post error", Exception));
            }
        }

        private void Request(HttpMethod Method = HttpMethod.GET, string Data = "", Action Callback = null, Action<Exception> ErrorCallback = null)
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    StringBuilder URL = new StringBuilder();
                    URL.Append(string.Format("{0}://{1}:{2}{3}", Option.Scheme, Option.Host, Option.Port, Option.Path)).Append('?');

                    if (Option.Query.Count > 0)
                    {
                        foreach (string Key in Option.Query.Keys)
                        {
                            URL.Append(Key).Append('=').Append(Option.Query[Key]).Append('&');
                        }
                    }

                    URL.Append("b64=1&");
                    URL.Append("transport=polling&");
                    URL.Append(string.Format("{0}={1}", Option.TimestampParam, EngineIOTimestamp.Generate()));

                    HttpWebRequest Request = WebRequest.Create(URL.ToString()) as HttpWebRequest;
                    Request.ServerCertificateValidationCallback = Option.ServerCertificateValidationCallback;
                    Request.Method = Method.ToString();
                    Request.CookieContainer = Cookies;
                    Request.KeepAlive = false;

                    if (Option.ExtraHeaders.Count > 0)
                    {
                        foreach (string Key in Option.ExtraHeaders.Keys)
                        {
                            try { Request.Headers.Add(Key, Option.ExtraHeaders[Key]); }
                            catch { }
                        }
                    }

                    if (Option.ClientCertificates != null)
                    {
                        Request.ClientCertificates = Option.ClientCertificates;
                    }

                    if (!string.IsNullOrWhiteSpace(Data))
                    {
                        using (StreamWriter Writer = new StreamWriter(Request.GetRequestStream()))
                        {
                            Writer.Write(Data);
                        }
                    }

                    HttpWebResponse Response = Request.GetResponse() as HttpWebResponse;

                    if (Response.StatusCode == HttpStatusCode.OK)
                    {
                        Callback?.Invoke();

                        foreach (EngineIOPacket Packet in EngineIOPacket.Decode(Response))
                        {
                            if (Packet.Type != EngineIOPacketType.CLOSE)
                            {
                                if (ReadyState == EngineIOReadyState.OPENING)
                                {
                                    OnOpen();
                                }

                                OnPacket(Packet);

                                if (ReadyState != EngineIOReadyState.CLOSED)
                                {
                                    Polling = false;
                                    Emit(Event.POLL_COMPLETE);

                                    if (ReadyState == EngineIOReadyState.OPEN)
                                    {
                                        Poll();
                                    }
                                }
                            }
                            else
                            {
                                OnClose();
                            }
                        }
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
            });
        }

        private enum HttpMethod
        {
            GET,
            POST
        };
    }
}
