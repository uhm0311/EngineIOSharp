using System;
using WebSocketSharp.Server;

namespace EngineIOSharp.Abstract
{
    public abstract partial class EngineIOConnection : WebSocketBehavior, IDisposable
    {
        public int PingInterval { get; protected set; }
        public int PingTimeout { get; protected set; }

        public void Dispose()
        {
            Close();
        }

        public abstract void Close();
    }
}
