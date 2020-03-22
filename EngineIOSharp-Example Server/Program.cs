using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EngineIOSharp.Server;
using EngineIOSharp.Common;

namespace EngineIOSharp.Example.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 1009;

            using (EngineIOServer server = new EngineIOServer(port))
            {
                Console.WriteLine("Listening on " + port);

                server.OnConnection((client) =>
                {
                    Console.WriteLine("Client connected!");

                    client.On(EngineIOEvent.MESSAGE, (message) =>
                    {
                        Console.WriteLine("Client : " + message.Data);
                        client.Send(message.Data);
                    });

                    client.On(EngineIOEvent.CLOSE, () =>
                    {
                        Console.WriteLine("Client disconnected!");
                    });
                });

                server.Start();

                Console.Read();
            }
        }
    }
}
