using EngineIOSharp.Client;
using System.Threading;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        public void Broadcast(string Data)
        {
            if (!string.IsNullOrEmpty(Data))
            {
                Monitor.Enter(ClientMutex);
                {
                    foreach (EngineIOClient Client in ClientList)
                    {
                        Client.Send(Data);
                    }
                }
                Monitor.Exit(ClientMutex);
            }
        }

        public void Broadcast(byte[] RawData)
        {
            if ((RawData?.Length ?? 0) > 0)
            {
                Monitor.Enter(ClientMutex);
                {
                    foreach (EngineIOClient Client in ClientList)
                    {
                        Client.Send(RawData);
                    }
                }
                Monitor.Exit(ClientMutex);
            }
        }
    }
}
