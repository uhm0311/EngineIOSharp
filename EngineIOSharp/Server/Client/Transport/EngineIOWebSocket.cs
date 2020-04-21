using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineIOSharp.Server.Client.Transport
{
    internal class EngineIOWebSocket : EngineIOTransport
    {
        public static readonly string Name = "websocket";

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
