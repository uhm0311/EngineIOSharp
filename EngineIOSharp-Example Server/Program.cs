using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace EngineIOSharp.Example.Server
{
    class Program : WebSocketBehavior
    {
        static void Main(string[] args)
        {
            WebSocketServer server = new WebSocketServer(1009);
            server.AddWebSocketService<Program>("");
            server.Start();

            Console.Read();
            server.Stop();
        }

        protected override void OnOpen()
        {
            
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            
        }

        protected override void OnError(ErrorEventArgs e)
        {
            
        }

        protected override void OnClose(CloseEventArgs e)
        {
            
        }
    }
}
