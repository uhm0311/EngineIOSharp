using EngineIOSharp.Client.Event;
using EngineIOSharp.Server;
using EngineIOSharp.Server.Event;
using System;

namespace EngineIOSharp.Example.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (EngineIOServer server = new EngineIOServer(1009, 30000000, 30000000))
            {
                Console.WriteLine("Listening on " + server.Port);

                server.On(EngineIOServerEvent.CONNECTION, (client) =>
                {
                    Console.WriteLine("Client connected!");

                    client.On(EngineIOClientEvent.MESSAGE, (message) =>
                    {
                        Console.WriteLine("Client : " + message.Data);
                        client.Send(message.Data);
                    });

                    client.On(EngineIOClientEvent.CLOSE, () =>
                    {
                        Console.WriteLine("Client disconnected!");
                    });
                });

                server.Start();

                Console.WriteLine("Input /exit to exit program.");
                string line;

                while (!(line = Console.ReadLine())?.Trim()?.ToLower()?.Equals("/exit") ?? false)
                {
                    server.Broadcast(line);
                }
            }
        }
    }
}
