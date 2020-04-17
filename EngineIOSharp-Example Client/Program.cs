using EngineIOSharp.Client;
using EngineIOSharp.Common.Enum;
using System;
using System.Text;

namespace EngineIOSharp.Example.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            using (EngineIOClient client = new EngineIOClient(new EngineIOClientOption(EngineIOScheme.http, "localhost", 1009)))
            {
                client.OnOpen(() =>
                {
                    Console.WriteLine("Conencted!");
                });

                client.OnMessage((Packet) =>
                {
                    Console.WriteLine("Server : " + Packet.Data);
                });

                client.OnClose(() =>
                {
                    Console.WriteLine("Disconnected!");
                });

                client.Connect();

                Console.WriteLine("Input /exit to close program.");
                string line;

                while (!(line = Console.ReadLine())?.Trim()?.ToLower()?.Equals("/exit") ?? false)
                {
                    client.Send(line);
                    client.Send(line);
                    //client.Send(new byte[] { 0, 1, 2, 3, 4, 5 });
                }
            }
        }
    }
}
