using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Net;

namespace EngineIOSharp.Server.Client.Transport
{
    internal class EngineIOPolling : EngineIOTransport
    {
        public static readonly string Name = "polling";

        private readonly Semaphore Semaphore = new Semaphore(0, 1);

        private HttpListenerRequest Request;
        private HttpListenerResponse Response;

        private Action ShouldClose;

        internal EngineIOPolling()
        {
            Semaphore.Release();
        }

        protected override void CloseInternal(Action Callback)
        {
            throw new NotImplementedException();
        }

        internal override EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            Semaphore.WaitOne();

            ThreadPool.QueueUserWorkItem((_) =>
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

                    foreach (EngineIOPacket Packet in Packets)
                    {
                        Send(Packet);
                    }
                }

                Semaphore.Release();
            });

            return this;
        }

        private void Send(EngineIOPacket Packet)
        {
            using (Response)
            {
                try
                {
                    Response.ContentType = "text/plain; charset=UTF-8";
                    Response.Headers["Content-Encoding"] = "gzip";

                    using (MemoryStream Stream = new MemoryStream())
                    {
                        using (GZipStream GZipStream = new GZipStream(Stream, CompressionMode.Compress))
                        using (StreamWriter Writer = new StreamWriter(GZipStream))
                        {
                            Writer.Write(Packet.Encode(true) as string);
                        }

                        byte[] RawData = Stream.ToArray();
                        Response.ContentLength64 = RawData.Length;

                        using (Response.OutputStream)
                        {
                            Response.OutputStream.Write(RawData, 0, RawData.Length);
                        }
                    }
                }
                catch
                {
                    CloseResponse(Response);
                }
                finally
                {
                    Semaphore.Release();
                    Cleanup();
                }
            }
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
            if (this.Request == null)
            {
                this.Request = Request;
                this.Response = Response;

                Writable = true;
                Emit(Event.DRAIN);

                if (Writable && ShouldClose != null)
                {

                }
            }
            else
            {
                EngineIOLogger.Error("Overlap from client", new Exception());

                CloseResponse(Response);
            }
        }

        private void OnRequestClose()
        {
            OnError("Poll connection closed prematurely.", new Exception());
        }

        private void Cleanup()
        {
            Request = null;
            Response = null;
        }

        private void CloseResponse(HttpListenerResponse Response)
        {
            using (Response)
            {
                Response.StatusCode = 500;
            }
        }

        private void OnDataRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            throw new NotImplementedException();
        }
    }
}
