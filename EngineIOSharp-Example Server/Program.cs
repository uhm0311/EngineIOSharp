using Newtonsoft.Json.Linq;
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
        static WebSocketServer server = new WebSocketServer(1009);

        static void Main(string[] args)
        {
            server.AddWebSocketService<Program>("/engine.io/");
            server.Start();

            Console.Read();
            server.Stop();
        }

        protected override void OnOpen()
        {
            WebSocket temp = Sessions[ID].Context.WebSocket;
            JObject jObject = new JObject()
            {
                ["sid"] = ID,
                ["pingTimeout"] = 10000,
                ["pingInterval"] = 1000,
                ["upgrades"] = new JArray(),
            };

            temp.Send("0" + jObject);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Sessions[ID].Context.WebSocket.Send("3");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            
        }

        protected override void OnClose(CloseEventArgs e)
        {
            object temp = ID;
        }
    }
}
