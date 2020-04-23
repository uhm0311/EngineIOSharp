﻿using EngineIOSharp.Server;
using System;

namespace EngineIOSharp.Example.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (EngineIOServer server = new EngineIOServer(new EngineIOServerOption(1009, PingInterval: int.MaxValue / 2, PingTimeout: int.MaxValue / 2)))
            {
                Console.WriteLine("Listening on " + server.Option.Port);

                server.OnConnection((socket) =>
                {
                    Console.WriteLine("Client connected!");
                    socket.Send(new byte[] { 0, 1, 2, 3, 4, 5, 6 });

                    socket.OnMessage((packet) =>
                    {
                        Console.WriteLine(packet.Data);
                        socket.Send(packet.Data);
                    });

                    socket.OnClose(() =>
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
