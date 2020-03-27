using EngineIOSharp.Client.Event;
using EngineIOSharp.Common.Packet;
using System;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        private int AutoReconnectionCount = 0;

        private void OnWebSocketOpen(object sender, EventArgs e)
        {
        }

        private void OnWebSocketClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            CallEventHandler(EngineIOClientEvent.CLOSE);

            if (AutoReconnect > AutoReconnectionCount)
            {
                AutoReconnectionCount++;
                Connect();
            }
            else
            {
                StopHeartbeat();
            }
        }

        private void OnWebSocketError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            OnEngineIOError(e.Exception);
        }

        private void OnWebSocketMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            HandleEngineIOPacket(EngineIOPacket.Decode(e));
        }
    }
}
