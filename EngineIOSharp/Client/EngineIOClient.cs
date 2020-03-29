using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmitterSharp;
using EngineIOSharp.Client.Transport;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using Newtonsoft.Json.Linq;

namespace EngineIOSharp.Client
{
    public partial class EngineIOClient : Emitter<string, EngineIOPacket>
    {
        public EngineIOClientOption Option { get; private set; }
        public EngineIOHandshake Handshake { get; private set; }
        public EngineIOReadyState ReadyState { get; private set; }

        private EngineIOTransport Transport = null;

        private readonly Queue<EngineIOPacket> Buffer = new Queue<EngineIOPacket>();
        private int PreviousBufferSize = 0;

        private bool Upgrading = false;
        private bool PriorWebsocketSuccess = false;

        public EngineIOClient(EngineIOClientOption Option)
        {
            this.Option = Option;
            ReadyState = EngineIOReadyState.CLOSED;
        }

        public void Connect()
        {
            if (ReadyState == EngineIOReadyState.CLOSED)
            {
                EngineIOTransport Transport;
                ReadyState = EngineIOReadyState.OPENING;

                if (Option.WebSocket && (!Option.Polling || (Option.RemeberUpgrade && PriorWebsocketSuccess)))
                {
                    Transport = new EngineIOWebSocket(Option);
                }
                else
                {
                    Transport = new EngineIOPolling(Option);
                }

                SetTransport(Transport);
            }
        }

        private void SetTransport(EngineIOTransport Transport)
        {
            if (this.Transport != null)
            {
                this.Transport.Off();
            }

            this.Transport = (Transport
                .On(EngineIOTransport.Event.DRAIN, OnDrain)
                .On(EngineIOTransport.Event.PACKET, (Packet) => OnPacket(Packet as EngineIOPacket))
                .On(EngineIOTransport.Event.ERROR, (Exception) => OnError(Exception as Exception))
                .On(EngineIOTransport.Event.CLOSE, () => OnClose("Transport close.")) as EngineIOTransport)
                .Open();
        }

        private void Flush()
        {
            if (ReadyState != EngineIOReadyState.CLOSED && Transport.Writable && !Upgrading && Buffer.Count > 0)
            {
                Transport.Send(Buffer);
                PreviousBufferSize = Buffer.Count;

                Emit(Event.FLUSH);
            }
        }

        private void OnOpen()
        {
            ReadyState = EngineIOReadyState.OPEN;
            PriorWebsocketSuccess = Transport is EngineIOWebSocket;

            Emit(Event.OPEN);
            
        }

        private void OnDrain()
        {
            while (PreviousBufferSize > 0)
            {
                Buffer.Dequeue();
                PreviousBufferSize--;
            }

            if (Buffer.Count == 0)
            {
                Emit(Event.DRAIN);
            }
            else
            {
                Flush();
            }
        }

        private void OnPacket(EngineIOPacket Packet)
        {
            if (ReadyState != EngineIOReadyState.CLOSED)
            {
                switch (Packet.Type)
                {
                    case EngineIOPacketType.OPEN:
                        Emit(Event.HANDSHAKE);
                        Handshake = new EngineIOHandshake(Packet.Data);

                        OnOpen();
                        break;
                }
            }
        }

        private void OnError(Exception Exception)
        {

        }

        private void OnClose(string Message, Exception Description = null)
        {

        }

        public static class Event
        {
            public static readonly string OPEN = "open";
            public static readonly string HANDSHAKE = "handshake";

            public static readonly string ERROR = "error";
            public static readonly string CLOSE = "close";

            public static readonly string PACKET = "packet";
            public static readonly string MESSAGE = "message";

            public static readonly string PACKET_CREATE = "packetCreate";
            public static readonly string FLUSH = "flush";
            public static readonly string DRAIN = "drain";

            public static readonly string UPGRADE = "upgrade";
            public static readonly string UPGRADING = "upgrading";
            public static readonly string UPGRADE_ERROR = "upgradeError";
        }
    }
}
