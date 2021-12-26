using EmitterSharp;
using EngineIOSharp.Client.Transport;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Common.Static;
using SimpleThreadMonitor;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineIOSharp.Client
{
    public partial class EngineIOClient : Emitter<EngineIOClient, string, object>, IDisposable
    {
        private static bool PriorWebsocketSuccess = false;

        public EngineIOClientOption Option { get; private set; }
        public EngineIOHandshake Handshake { get; private set; }
        public EngineIOReadyState ReadyState { get; private set; }

        private EngineIOTransport Transport = null;

        private readonly Queue<EngineIOPacket> PacketBuffer = new Queue<EngineIOPacket>();
        private readonly object BufferMutex = new object();
        private int PreviousBufferSize = 0;

        private bool Flushing = false;
        private bool Upgrading = false;

        public EngineIOClient(EngineIOClientOption Option)
        {
            this.Option = Option;
            ReadyState = EngineIOReadyState.CLOSED;
        }

        public EngineIOClient Connect()
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

            return this;
        }

        public EngineIOClient Close()
        {
            if (ReadyState == EngineIOReadyState.OPENING || ReadyState == EngineIOReadyState.OPEN)
            {
                ReadyState = EngineIOReadyState.CLOSING;

                void Close()
                {
                    this.OnClose("Forced close");
                    Transport.Close();
                }

                void CleanUpAndClose()
                {
                    Off(Event.UPGRADE, CleanUpAndClose);
                    Off(Event.UPGRADE_ERROR, CleanUpAndClose);

                    Close();
                }

                void OnClose()
                {
                    if (Upgrading)
                    {
                        Once(Event.UPGRADE, CleanUpAndClose);
                        Once(Event.UPGRADE_ERROR, CleanUpAndClose);
                    }
                    else
                    {
                        Close();
                    }
                }

                if (PacketBuffer.Count > 0)
                {
                    Once(Event.DRAIN, OnClose);
                }
                else
                {
                    OnClose();
                }
            }

            return this;
        }

        public void Dispose()
        {
            Close();
        }

        private void SetTransport(EngineIOTransport Transport)
        {
            if (this.Transport != null)
            {
                this.Transport.Off();
            }

            this.Transport = Transport
                .On(EngineIOTransport.Event.DRAIN, OnDrain)
                .On(EngineIOTransport.Event.PACKET, (Packet) => OnPacket(Packet as EngineIOPacket))
                .On(EngineIOTransport.Event.ERROR, (Exception) => OnError(Exception as Exception))
                .On(EngineIOTransport.Event.CLOSE, () => OnClose("Transport close."))
                .Open();
        }

        private void Probe()
        {
            EngineIOWebSocket Transport = new EngineIOWebSocket(Option);
            bool Failed = PriorWebsocketSuccess = false;

            void OnTransportOpen()
            {
                string Message = "probe";

                Transport.Send(EngineIOPacket.CreatePingPacket(Message));
                Transport.Once(EngineIOTransport.Event.PACKET, (Packet) =>
                {
                    if (!Failed)
                    {
                        EngineIOPacket Temp = Packet is EngineIOPacket ? Packet as EngineIOPacket : EngineIOPacket.CreateClosePacket();

                        if (Temp.Type == EngineIOPacketType.PONG && Temp.Data.Equals(Message))
                        {
                            Upgrading = true;
                            Emit(Event.UPGRADING, Transport);

                            PriorWebsocketSuccess = true;

                            (this.Transport as EngineIOPolling).Pause(() =>
                            {
                                if (!Failed && ReadyState != EngineIOReadyState.CLOSED)
                                {
                                    CleanUp();

                                    SetTransport(Transport);
                                    Transport.Send(EngineIOPacket.CreateUpgradePacket());

                                    Emit(Event.UPGRADE, Transport);
                                    Upgrading = false;

                                    Flush();
                                }
                            });
                        }
                        else
                        {
                            Emit(Event.UPGRADE_ERROR, new EngineIOException("Probe error"));
                        }
                    }
                });
            }

            void OnTransportClose()
            {
                OnTransportError(new EngineIOException("Transport closed"));
            }

            void OnTransportError(object Exception)
            {
                string Message = "Probe error";
                Exception = Exception is Exception ? new EngineIOException(Message, Exception as Exception) : new EngineIOException(Message);

                FreezeTransport();
                Emit(Event.UPGRADE_ERROR, Exception as Exception);
            }

            void FreezeTransport()
            {
                if (!Failed)
                {
                    Failed = true;
                    CleanUp();

                    Transport.Close();
                    Transport = null;
                }
            }

            void OnClose()
            {
                OnError(new EngineIOException("Client closed"));
            }

            void OnUpgrade()
            {
                if (!(Transport is EngineIOWebSocket))
                {
                    FreezeTransport();
                }
            }

            void CleanUp()
            {
                Transport.Off(EngineIOTransport.Event.OPEN, OnTransportOpen);
                Transport.Off(EngineIOTransport.Event.ERROR, OnTransportError);
                Transport.Off(EngineIOTransport.Event.CLOSE, OnTransportClose);

                Off(Event.CLOSE, OnClose);
                Off(Event.UPGRADING, OnUpgrade);
            }

            Transport.Once(EngineIOTransport.Event.OPEN, OnTransportOpen);
            Transport.Once(EngineIOTransport.Event.ERROR, OnTransportError);
            Transport.Once(EngineIOTransport.Event.CLOSE, OnTransportClose);

            Once(Event.CLOSE, OnClose);
            Once(Event.UPGRADING, OnUpgrade);

            Transport.Open();
        }

        private void Flush()
        {
            if (ReadyState != EngineIOReadyState.CLOSED && !Upgrading && PacketBuffer.Count > 0 && !Flushing)
            {
                Flushing = true;

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    try
                    {
                        while (!Transport.Writable)
                        {
                            Thread.Sleep(0);
                        }

                        SimpleMutex.Lock(BufferMutex, () => Transport.Send(PacketBuffer.ToArray()));
                        PreviousBufferSize = PacketBuffer.Count;

                        Emit(Event.FLUSH);
                        Flushing = false;
                    }
                    catch (Exception Exception)
                    {
                        OnError(Exception);
                    }
                });
            }
        }

        private void OnOpen()
        {
            ReadyState = EngineIOReadyState.OPEN;
            PriorWebsocketSuccess = Transport is EngineIOWebSocket;

            Emit(Event.OPEN);
            Flush();

            if (ReadyState == EngineIOReadyState.OPEN && Option.Upgrade && Option.WebSocket && Transport is EngineIOPolling)
            {
                foreach (string Upgrade in Handshake.Upgrades)
                {
                    if (EngineIOHttpManager.IsWebSocket(Upgrade))
                    {
                        Probe();
                        break;
                    }
                }
            }
        }

        private void OnClose(string Message, Exception Description = null)
        {
            if (ReadyState != EngineIOReadyState.CLOSED)
            {
                Transport.Off(EngineIOTransport.Event.CLOSE);
                Transport.Close();
                Transport.Off();

                ReadyState = EngineIOReadyState.CLOSED;
                Handshake = null;

                Emit(Event.CLOSE, new EngineIOException(Message, Description));

                PacketBuffer.Clear();
                PreviousBufferSize = 0;
            }
        }

        private void OnPacket(EngineIOPacket Packet)
        {
            if (ReadyState != EngineIOReadyState.CLOSED)
            {
                Emit(Event.PACKET, Packet);

                switch (Packet.Type)
                {
                    case EngineIOPacketType.OPEN:
                        Emit(Event.HANDSHAKE);

                        Handshake = new EngineIOHandshake(Packet.Data);
                        Option.Query.Add("sid", Handshake.SID);

                        OnOpen();
                        break;

                    case EngineIOPacketType.PING:
                        Send(EngineIOPacket.CreatePongPacket(Packet.Data));
                        break;

                    case EngineIOPacketType.PONG:
                        break;

                    case EngineIOPacketType.MESSAGE:
                        Emit(Event.MESSAGE, Packet);
                        break;

                    case EngineIOPacketType.UNKNOWN:
                        OnError(new EngineIOException(string.Format("Parse error : {0}", Packet.Data)));
                        break;
                }
            }
        }

        private void OnDrain()
        {
            while (PreviousBufferSize > 0)
            {
                PacketBuffer.Dequeue();
                PreviousBufferSize--;
            }

            if (PacketBuffer.Count == 0)
            {
                Emit(Event.DRAIN);
            }
            else
            {
                Flush();
            }
        }

        private void OnError(Exception Exception)
        {
            PriorWebsocketSuccess = false;

            Emit(Event.ERROR, Exception);
            OnClose("Transport error", Exception);
        }
    }
}
