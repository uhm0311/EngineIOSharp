using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineIOSharp.Client.Transport
{
    internal class EngineIOWebSocket : EngineIOTransport
    {
        public EngineIOWebSocket(EngineIOClientOption Option) : base(Option)
        {
        }

        protected override void CloseInternal()
        {
            throw new NotImplementedException();
        }

        protected override void OpenInternal()
        {
            throw new NotImplementedException();
        }

        protected override void SendInternal(EngineIOPacket Packets)
        {
            throw new NotImplementedException();
        }
    }
}
