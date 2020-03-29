using EngineIOSharp.Client;
using EngineIOSharp.Common.Enum;
using System;

namespace EngineIOSharp.Example.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            EngineIOClient client = new EngineIOClient(new EngineIOClientOption(EngineIOScheme.http, "localhost", 1009));
            client.Connect();

            Console.Read();

            /*using (EngineIOClient client = new EngineIOClient(WebSocketScheme.ws, "127.0.0.1", 1009))
            {
                client.On(EngineIOClientEvent.OPEN, () =>
                {
                    Console.WriteLine("Conencted!");
                });

                client.On(EngineIOClientEvent.MESSAGE, (Packet) =>
                {
                    Console.WriteLine("Server : " + Packet.Data);
                });

                client.On(EngineIOClientEvent.CLOSE, () =>
                {
                    Console.WriteLine("Disconnected!");
                });

                client.Connect();

                Console.WriteLine("Input /exit to close program.");
                string line;

                while (!(line = Console.ReadLine())?.Trim()?.ToLower()?.Equals("/exit") ?? false)
                {
                    client.Send(line);
                }
            }*/
        }
    }
}
