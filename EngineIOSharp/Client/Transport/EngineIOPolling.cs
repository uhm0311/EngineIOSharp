using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using EngineIOSharp.Common.Static;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;

namespace EngineIOSharp.Client.Transport
{
    internal class EngineIOPolling : EngineIOTransport
    {
        private CookieContainer Cookies = new CookieContainer();
        private bool Polling = false;

        public EngineIOPolling(EngineIOClientOption Option) : base(Option) 
        {
            On(Event.OPEN, Poll);
        }

        private void Poll()
        {
            Polling = true;

        }

        private void Pause()
        {
            ReadyState = EngineIOReadyState.PAUSING;

            void Pause()
            {

            }
        }

        protected override void OpenInternal()
        {
            try
            {
                foreach (EngineIOPacket Packet in EngineIOPacket.Decode(CreateRequest().GetResponse() as HttpWebResponse))
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
                            Emit(Event.POLL);

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
            catch (Exception Exception)
            {
                OnError("Error", Exception);
            }
        }

        protected override void CloseInternal()
        {
            throw new NotImplementedException();
        }

        protected override void SendInternal(IEnumerable<EngineIOPacket> Packets)
        {
            throw new NotImplementedException();
        }

        private HttpWebRequest CreateRequest(HttpMethod Method = HttpMethod.GET)
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

            return Request;
        }

        private enum HttpMethod
        {
            GET,
            POST
        };
    }
}
