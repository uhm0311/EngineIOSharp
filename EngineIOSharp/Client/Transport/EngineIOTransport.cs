using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using System;
using System.Threading;

namespace EngineIOSharp.Client.Transport
{
    internal abstract class EngineIOTransport : Emitter<string, object>
    {
        protected EngineIOClientOption Option { get; private set; }

        public ReadyState ReadyState { get; protected set; }
        public bool Writable { get; protected set; }

        protected EngineIOTransport(EngineIOClientOption Option)
        {
            this.Option = Option;
            ReadyState = ReadyState.CLOSED;
        }

        public EngineIOTransport Open()
        {
            ThreadPool.QueueUserWorkItem((_) => 
            {
                if (ReadyState == ReadyState.CLOSED)
                {
                    ReadyState = ReadyState.OPENING;
                    OpenInternal();
                }
            });

            return this;
        }

        public EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            if ((Packets?.Length ?? 0) > 0)
            {
                ThreadPool.QueueUserWorkItem((_) =>
                {
                    if (ReadyState == ReadyState.OPEN)
                    {
                        try
                        {
                            SendInternal(Packets);
                        }
                        catch (Exception Exception)
                        {
                            EngineIOLogger.E(this, Exception);
                        }
                    }
                    else
                    {
                        EngineIOLogger.E(this, new EngineIOException("Transport is not opened. ReadyState : " + ReadyState));
                    }
                });
            }

            return this;
        }

        public EngineIOTransport Close()
        {
            ThreadPool.QueueUserWorkItem((_) =>
            {
                if (ReadyState == ReadyState.OPENING || ReadyState == ReadyState.OPEN)
                {
                    EmitClose();
                    CloseInternal();
                }
            });

            return this;
        }

        protected EngineIOTransport EmitOpen()
        {
            ReadyState = ReadyState.OPEN;
            Emit(EngineIOEvent.OPEN);

            return this;
        }

        protected EngineIOTransport EmitError(string Message, Exception Description)
        {
            Emit(EngineIOEvent.ERROR, new EngineIOException(Message, Description));

            return this;
        }

        protected EngineIOTransport EmitClose()
        {
            ReadyState = ReadyState.CLOSED;
            Emit(EngineIOEvent.CLOSE);

            return this;
        }

        protected EngineIOTransport EmitPacket(EngineIOPacket Packet)
        {
            Emit(EngineIOEvent.PACKET, Packet);

            return this;
        }

        protected abstract void OpenInternal();

        protected abstract void CloseInternal();

        protected abstract void SendInternal(params EngineIOPacket[] Packets);
    }
}
