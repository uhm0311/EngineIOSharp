using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using System;
using System.Threading;

namespace EngineIOSharp.Client.Transport
{
    internal abstract class EngineIOTransport : Emitter<EngineIOTransport, string, object>
    {
        private readonly Semaphore Semaphore = new Semaphore(0, 1);

        protected EngineIOClientOption Option { get; private set; }

        public EngineIOReadyState ReadyState { get; protected set; }
        public bool Writable { get; protected set; }

        public const int Protocol = 4;

        protected EngineIOTransport(EngineIOClientOption Option)
        {
            this.Option = Option;

            ReadyState = EngineIOReadyState.CLOSED;
            Writable = false;

            Semaphore.Release();
        }

        internal EngineIOTransport Open()
        {
            ThreadPool.QueueUserWorkItem((_) => 
            {
                try
                {
                    if (ReadyState == EngineIOReadyState.CLOSED)
                    {
                        ReadyState = EngineIOReadyState.OPENING;
                        OpenInternal();
                    }
                }
                catch (Exception Exception)
                {
                    OnError("Transport not opned normally.", Exception);
                }
            });

            return this;
        }

        internal EngineIOTransport Close()
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    if (ReadyState == EngineIOReadyState.OPENING || ReadyState == EngineIOReadyState.OPEN)
                    {
                        CloseInternal();
                        OnClose();
                    }
                }
                catch (Exception Exception)
                {
                    OnError("Transport not closed normally.", Exception);
                }
            });

            return this;
        }

        internal EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            if (Packets != null)
            {
                Writable = false;

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    try
                    {
                        Semaphore.WaitOne();

                        if (ReadyState == EngineIOReadyState.OPEN)
                        {
                            try
                            {
                                foreach (EngineIOPacket Packet in Packets)
                                {
                                    SendInternal(Packet);
                                }
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

                        Semaphore.Release();
                        Writable = true;
                    }
                    catch (Exception Exception)
                    {
                        OnError("Transport not sent.", Exception);
                    }
                });
            }

            return this;
        }

        protected EngineIOTransport OnOpen()
        {
            ReadyState = EngineIOReadyState.OPEN;
            Emit(Event.OPEN);

            if (Option.Query.ContainsKey("sid"))
            {
                Writable = true;
            }

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

        protected EngineIOTransport OnError(string Message, Exception Description)
        {
            EngineIOException Exception = new EngineIOException(Message, Description);

            EngineIOLogger.Error(this, Exception);
            Emit(Event.ERROR, Exception);

            return this;
        }

        protected abstract void OpenInternal();

        protected abstract void CloseInternal();

        protected abstract void SendInternal(EngineIOPacket Packet);

        internal static class Event
        {
            public static readonly string OPEN = "open";
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

            public static readonly string ERROR = "error";
        }
    }
}
