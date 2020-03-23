using EngineIOSharp.Client;
using SimpleThreadMonitor;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        public void Broadcast(string Data)
        {
            if (!string.IsNullOrEmpty(Data))
            {
                SimpleMutex.Lock(ClientMutex, () =>
                {
                    foreach (EngineIOClient Client in ClientList)
                    {
                        Client.Send(Data);
                    }
                });
            }
        }

        public void Broadcast(byte[] RawData)
        {
            if ((RawData?.Length ?? 0) > 0)
            {
                SimpleMutex.Lock(ClientMutex, () =>
                {
                    foreach (EngineIOClient Client in ClientList)
                    {
                        Client.Send(RawData);
                    }
                });
            }
        }
    }
}
