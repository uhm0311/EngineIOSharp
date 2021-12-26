namespace EngineIOSharp.Server.Client
{
    partial class EngineIOSocket
    {
        private readonly object PingMutex = new object();
        private readonly object PongMutex = new object();

        private ulong Pong = 0;
    }
}
