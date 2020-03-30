using EngineIOSharp.Common.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineIOSharp.Client
{
    partial class EngineIOClient
    {
        public EngineIOClient OnOpen(Action Callback)
        {
            return On(Event.OPEN, Callback) as EngineIOClient;
        }

        public EngineIOClient OnClose(Action Callback)
        {
            return On(Event.CLOSE, Callback) as EngineIOClient;
        }

        public EngineIOClient OnClose(Action<Exception> Callback)
        {
            return On(Event.CLOSE, (Exception) => Callback(Exception as Exception)) as EngineIOClient;
        }

        public EngineIOClient OnMessage(Action<EngineIOPacket> Callback)
        {
            return On(Event.MESSAGE, (Packet) => Callback(Packet as EngineIOPacket)) as EngineIOClient;
        }


    }
}
