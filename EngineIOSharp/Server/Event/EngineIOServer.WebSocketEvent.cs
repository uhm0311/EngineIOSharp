using EngineIOSharp.Client;
using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        private readonly List<EngineIOClient> ClientList = new List<EngineIOClient>();
        private readonly object ClientMutex = new object();

        private class EngineIOBehavior : WebSocketBehavior
        {
            private readonly Action<EngineIOClient, string> Initializer;

            internal EngineIOBehavior(Action<EngineIOClient, string> Initializer)
            {
                this.Initializer = Initializer;
            }

            protected override void OnOpen()
            {
                if (ID != null && Sessions[ID]?.Context != null)
                {
                    Initializer(new EngineIOClient(Sessions[ID].Context), ID);
                }
            }
        }
    }
}