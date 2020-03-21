using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EngineIOSharp.Client;
using EngineIOSharp.Common;

namespace EngineIOSharp.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            EngineIOClient client = new EngineIOClient(WebSocketScheme.ws, "127.0.0.1", 1009);

            client.On(EngineIOEvent.OPEN, () =>
            {
                Console.WriteLine("Conencted!");
            });

            client.On(EngineIOEvent.MESSAGE, (Packet) =>
            {
                Console.WriteLine("Echo : " + Packet.Data);
            });

            client.On(EngineIOEvent.CLOSE, () =>
            {
                Console.WriteLine("Disconnected!");
            });

            client.Connect();
            Console.WriteLine("Input /exit to close connection.");

            string line;
            while (!(line = Console.ReadLine()).Equals("/exit"))
            {
                client.Send(line);
            }

            client.Close();

            Console.WriteLine("Press any key to continue...");
            Console.Read();
        }
    }
}
