using EmitterSharp;
using EngineIOSharp.Common;
using EngineIOSharp.Common.Enum;
using EngineIOSharp.Common.Packet;
using EngineIOSharp.Server.Client.Transport;
using SimpleThreadMonitor;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EngineIOSharp.Server.Client
{
    public partial class EngineIOSocket : Emitter<EngineIOSocket, string, object>, IDisposable
    {
        public string SID { get; private set; }
        public EngineIOServer Server { get; private set; }

        internal bool Upgrading { get; private set; }
        public bool Upgraded { get; private set; }

        public EngineIOReadyState ReadyState { get; private set; }
        internal EngineIOTransport Transport { get; private set; }

        private readonly Queue<EngineIOPacket> PacketBuffer = new Queue<EngineIOPacket>();
        private readonly object BufferMutex = new object();

        private readonly Queue<Action> Cleanup = new Queue<Action>();

        private readonly Queue<Action> PacketCallback = new Queue<Action>();
        private readonly Queue<Queue<Action>> SentCallback = new Queue<Queue<Action>>();

        internal EngineIOSocket(string SID, EngineIOServer Server, EngineIOTransport Transport)
        {
            this.SID = SID;
            this.Server = Server;

            Upgrading = false;
            Upgraded = false;

            ReadyState = EngineIOReadyState.OPENING;

            SetTransport(Transport);
            OnOpen();
        }

        public void Close()
        {
            Close(true);
        }

        internal void Close(bool Discard)
        {
            if (ReadyState == EngineIOReadyState.OPEN)
            {
                ReadyState = EngineIOReadyState.CLOSING;

                if (PacketBuffer.Count > 0)
                {
                    Once(Event.DRAIN, () => CloseTransport(Discard));
                }
                else
                {
                    CloseTransport(Discard);
                }
            }
        }

        private void CloseTransport(bool Discard)
        {
            if (Discard)
            {
                Transport.Discard();
            }

            Transport.Close(() => OnClose("Forced close"));
        }

        public void Dispose()
        {
            Close(true);
        }

        private void SetTransport(EngineIOTransport Transport)
        {
            this.Transport = Transport
                .On(EngineIOTransport.Event.DRAIN, Flush)
                .On(EngineIOTransport.Event.DRAIN, OnDrain)
                .On(EngineIOTransport.Event.PACKET, (Packet) => OnPacket(Packet as EngineIOPacket))
                .Once(EngineIOTransport.Event.ERROR, (Exception) => OnError(Exception as Exception))
                .Once(EngineIOTransport.Event.CLOSE, (Argument) =>
                {
                    if (Argument is object[] Temp)
                    {
                        OnClose(Temp[0] as string, Temp[1] as Exception);
                    }
                    else
                    {
                        OnClose("Transport closed");
                    }
                });

            Cleanup.Enqueue(() => this.Transport.Off());
        }

        internal void UpgradeTransport(EngineIOWebSocket Transport)
        {
            void OnTransportPacket(object Argument)
            {
                EngineIOPacket Packet = Argument as EngineIOPacket;

                if (Packet.Type == EngineIOPacketType.PING)
                {
                    Transport.Send(EngineIOPacket.CreatePongPacket(Packet.Data));
                    Emit(Event.UPGRADING);

                    ResetCheckTimer();
                }
                else
                {
                    Cleanup();

                    if (Packet.Type == EngineIOPacketType.UPGRADE && ReadyState != EngineIOReadyState.CLOSED)
                    {
                        this.Transport.Discard();
                        Upgraded = true;

                        ClearTransport();
                        SetTransport(Transport);

                        Emit(Event.UPGRADE);

                        ResetEIO3PingTimer();
                        Flush();

                        if (ReadyState == EngineIOReadyState.CLOSING)
                        {
                            this.Transport.Close(() =>
                            {
                                this.OnClose("Forced close.");
                            });
                        }
                    }
                    else
                    {
                        Transport.Close();
                    }
                }
            }

            void Cleanup()
            {
                Upgrading = false;

                StopCheckTimer();
                StopUpgradeTimer();

                Transport.Off(EngineIOTransport.Event.PACKET, OnTransportPacket);
                Transport.Off(EngineIOTransport.Event.CLOSE, OnTransportClose);
                Transport.Off(EngineIOTransport.Event.ERROR, OnTransportError);
            }

            void OnTransportError(object Exception)
            {
                EngineIOLogger.Error(this, Exception as Exception);

                Cleanup();
                Transport.Close();
            }

            void OnTransportClose()
            {
                OnTransportError(new EngineIOException("Transport closed."));
            }

            void OnClose()
            {
                OnTransportError(new EngineIOException("Socket closed."));
            }

            Upgrading = true;
            StartUpgradeTimer(() =>
            {
                Cleanup();

                if (Transport.ReadyState == EngineIOReadyState.OPEN)
                {
                    Transport.Close();
                }
            });

            Transport.On(EngineIOTransport.Event.PACKET, OnTransportPacket);
            Transport.Once(EngineIOTransport.Event.CLOSE, OnTransportClose);
            Transport.Once(EngineIOTransport.Event.ERROR, OnTransportError);

            Once(Event.CLOSE, OnClose);
        }

        private void ClearTransport()
        {
            while (Cleanup.Count > 0)
            {
                Cleanup.Dequeue()?.Invoke();
            }

            Transport.Close();
            StopEIO3PingTimer();
        }

        private void Flush()
        {
            if (ReadyState != EngineIOReadyState.CLOSED && Transport.Writable && !Upgrading && PacketBuffer.Count > 0)
            {
                EngineIOPacket[] Packets = null;

                SimpleMutex.Lock(BufferMutex, () =>
                {
                    Packets = PacketBuffer.ToArray();
                    PacketBuffer.Clear();
                });

                if ((Packets?.Length ?? 0) > 0)
                {
                    Emit(Event.FLUSH, Packets);
                    Server.Emit(EngineIOServer.Event.FLUSH, Packets);

                    SentCallback.Enqueue(PacketCallback);
                    PacketCallback.Clear();

                    Transport.Send(Packets);

                    Emit(Event.DRAIN, Packets);
                    Server.Emit(EngineIOServer.Event.DRAIN, Packets);
                }
            }
        }

        private void OnOpen()
        {
            ReadyState = EngineIOReadyState.OPEN;
            EngineIOServerOption Option = Server.Option;

            Transport.SID = SID;
            Send(EngineIOPacket.CreateOpenPacket(SID, Option.PingInterval, Option.PingTimeout, Option.WebSocket && Option.AllowUpgrade));

            object InitialData = Option.InitialData;

            if (InitialData is string)
            {
                Send(InitialData as string);
            }
            else if (InitialData is byte[])
            {
                Send(InitialData as byte[]);
            }

            Emit(Event.OPEN);

            StartEIO4Heartbeat();
            ResetEIO3PingTimer();
        }

        private void OnClose(string Message, Exception Description = null)
        {
            if (ReadyState != EngineIOReadyState.CLOSED)
            {
                ReadyState = EngineIOReadyState.CLOSED;

                StopEIO4Heartbeat();
                StopEIO3Hertbeat();
                StopCheckUpgradeTimers();

                PacketCallback.Clear();
                SentCallback.Clear();

                ClearTransport();
                Emit(Event.CLOSE, new object[] { Message, Description });

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    Thread.Sleep(0);
                    SimpleMutex.Lock(BufferMutex, PacketBuffer.Clear);
                });
            }
        }

        private void OnPacket(EngineIOPacket Packet)
        {
            if (ReadyState == EngineIOReadyState.OPEN)
            {
                Emit(Event.PACKET, Packet);

                ResetEIO3PongTimer(Server.Option.PingInterval + Server.Option.PingTimeout);
                ResetEIO3PingTimer();

                switch (Packet.Type)
                {
                    case EngineIOPacketType.PING:
                        if (Transport.Protocol == 3)
                        {
                            Send(EngineIOPacket.CreatePongPacket(Packet.Data));
                            Emit(Event.HEARTBEAT);
                        }
                        else
                        {
                            OnError(new EngineIOException("Invalid heartbeat direction."));
                        }

                        break;

                    case EngineIOPacketType.PONG:
                        if (Transport.Protocol != 3)
                        {
                            SimpleMutex.Lock(PongMutex, () => Pong++);
                            Emit(Event.HEARTBEAT);
                        }
                        else
                        {
                            OnError(new EngineIOException("Invalid heartbeat direction."));
                        }

                        break;

                    case EngineIOPacketType.MESSAGE:
                        Emit(Event.MESSAGE, Packet);
                        break;

                    case EngineIOPacketType.UNKNOWN:
                        OnClose(string.Format("Parse error : {0}", Packet.Data));
                        break;
                }
            }
        }

        private void OnDrain()
        {
            if (SentCallback.Count > 0)
            {
                Queue<Action> Callback = SentCallback.Dequeue();

                while (Callback.Count > 0)
                {
                    Callback.Dequeue()?.Invoke();
                }
            }
        }

        private void OnError(Exception Exception)
        {
            OnClose("Transport error.", Exception);
        }
    }
}
