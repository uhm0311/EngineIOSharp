using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineIOSharp.Client.Transport
{
    internal abstract class EngineIOTransport : Emitter<string, object>
    {
        protected EngineIOClientOption Option { get; private set; }

        public EngineIOReadyState ReadyState { get; protected set; }
        public bool Writable { get; protected set; }

        protected EngineIOTransport(EngineIOClientOption Option)
        {
            this.Option = Option;
            ReadyState = EngineIOReadyState.CLOSED;
        }

        public EngineIOTransport Open()
        {
            ThreadPool.QueueUserWorkItem((_) => 
            {
                if (ReadyState == EngineIOReadyState.CLOSED)
                {
                    ReadyState = EngineIOReadyState.OPENING;
                    OpenInternal();
                }
            });

            return this;
        }

        public EngineIOTransport Send(IEnumerable<EngineIOPacket> Packets)
        {
            if (Packets != null)
            {
                ThreadPool.QueueUserWorkItem((_) =>
                {
                    if (ReadyState == EngineIOReadyState.OPEN)
                    {
                        try
                        {
                            SendInternal(Packets);
                        }
                        catch (Exception Exception)
                        {
                            EngineIOLogger.Error(this, Exception);
                        }
                    }
                    else
                    {
                        EngineIOLogger.Error(this, new EngineIOException("Transport is not opened. ReadyState : " + ReadyState));
                    }
                });
            }

            return this;
        }

        public EngineIOTransport Close()
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                if (ReadyState == EngineIOReadyState.OPENING || ReadyState == EngineIOReadyState.OPEN)
                {
                    OnClose();
                    CloseInternal();
                }
            });

            return this;
        }

        protected EngineIOTransport OnOpen()
        {
            ReadyState = EngineIOReadyState.OPEN;
            Emit(Event.OPEN);

            return this;
        }

        protected EngineIOTransport OnError(string Message, Exception Description)
        {
            Emit(Event.ERROR, new EngineIOException(Message, Description));

            return this;
        }

        protected EngineIOTransport OnClose()
        {
            ReadyState = EngineIOReadyState.CLOSED;
            Emit(Event.CLOSE);

            return this;
        }

        protected EngineIOTransport OnPacket(EngineIOPacket Packet)
        {
            Emit(Event.PACKET, Packet);

            return this;
        }

        protected abstract void OpenInternal();

        protected abstract void CloseInternal();

        protected abstract void SendInternal(IEnumerable<EngineIOPacket> Packets);

        internal static class Event
        {
            public static readonly string OPEN = "open";
            public static readonly string ERROR = "error";
            public static readonly string CLOSE = "close";

            public static readonly string PACKET = "packet";
            public static readonly string MESSAGE = "message";

            public static readonly string PACKET_CREATE = "packetCreate";
            public static readonly string FLUSH = "flush";
            public static readonly string DRAIN = "drain";

            public static readonly string POLL = "poll";
            public static readonly string POLL_COMPLETE = "pollComplete";

            public static readonly string UPGRADE = "upgrade";
            public static readonly string UPGRADE_ERROR = "upgradeError";

            public static readonly string REQUEST_HEADERS = "requestHeaders";
            public static readonly string RESPONSE_HEADERS = "responseHeaders";
        }
    }
}
