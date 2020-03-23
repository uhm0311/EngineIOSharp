using EngineIOSharp.Client.Event;
using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private int AutoReconnectionCount = 0;

        private void OnWebsocketOpen(object sender, EventArgs e)
        {
        }

        private void OnWebsocketClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            CallEventHandler(EngineIOClientEvent.CLOSE);

            if (AutoReconnect > AutoReconnectionCount)
            {
                AutoReconnectionCount++;
                Connect();
            }
        }

        private void OnWebsocketError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            CallEventHandler(EngineIOClientEvent.ERROR, EngineIOPacket.CreateErrorPacket(e.Message, e.Exception));
        }

        private void OnWebsocketMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            HandleEngineIOPacket(EngineIOPacket.Decode(e));
        }
    }
}
