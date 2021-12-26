using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using System;
using WebSocketSharp.Net;

namespace EngineIOSharp.Server.Client.Transport
{
    internal abstract class EngineIOTransport : Emitter<EngineIOTransport, string, object>
    {
        internal string SID { get; set; }
        public int Protocol { get; private set; }
        public EngineIOReadyState ReadyState { get; protected set; }

        public bool Discarded { get; protected set; }
        public bool Writable { get; protected set; }
        protected bool ForceBase64 { get; set; }

        protected EngineIOTransport(int Protocol)
        {
            this.Protocol = Protocol;

            ReadyState = EngineIOReadyState.OPEN;
            Discarded = false;
            Writable = false;
        }

        internal void Discard()
        {
            Discarded = true;
        }

        internal void Close(Action Callback = null)
        {
            if (ReadyState != EngineIOReadyState.CLOSING && ReadyState != EngineIOReadyState.CLOSED)
            {
                ReadyState = EngineIOReadyState.CLOSING;
                CloseInternal(Callback);
            }
        }

        internal virtual EngineIOTransport OnRequest(HttpListenerRequest Request, HttpListenerResponse Response)
        {
            return this;
        }

        protected EngineIOTransport OnError(string Message, Exception Description)
        {
            EngineIOException Exception = new EngineIOException("Transport error : " + Message, Description);

            EngineIOLogger.Error(this, Exception);
            Emit(Event.ERROR, Exception);

            return this;
        }

        protected EngineIOTransport OnPacket(EngineIOPacket Packet)
        {
            Emit(Event.PACKET, Packet);

            return this;
        }

        protected EngineIOTransport OnClose()
        {
            ReadyState = EngineIOReadyState.CLOSED;
            Emit(Event.CLOSE);

            return this;
        }

        protected abstract void CloseInternal(Action Callback);

        internal abstract EngineIOTransport Send(params EngineIOPacket[] Packets);

        internal static class Event
        {
            internal static readonly string CLOSE = "close";

            internal static readonly string PACKET = "packet";
            internal static readonly string DRAIN = "drain";

            internal static readonly string HEADERS = "headers";
            internal static readonly string ERROR = "error";
        }
    }
}
