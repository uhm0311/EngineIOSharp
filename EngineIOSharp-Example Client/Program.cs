namespace EngineIOSharp.Example.Client
{
    class Program
    {
        static void Main(string[] args)
        {
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
