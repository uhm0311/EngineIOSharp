using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Net.WebSockets;

namespace EngineIOSharp.Server.Client.Transport
{
    internal class EngineIOWebSocket : EngineIOTransport
    {
        internal EngineIOWebSocket(WebSocketContext Context)
        {
            throw new NotImplementedException();
        }

        protected override void CloseInternal(Action Callback)
        {
            throw new NotImplementedException();
        }

        internal override EngineIOTransport Send(params EngineIOPacket[] Packets)
        {
            throw new NotImplementedException();
        }
    }
}
