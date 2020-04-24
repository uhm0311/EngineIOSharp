namespace EngineIOSharp.Common.Enum.Internal
{
    internal enum EngineIOTransportType
    {
        polling,
        websocket,
    }

    internal static class EngineIOTransportTypeMethods
    {
        public static bool Equals(this EngineIOTransportType TransportType, string TransportName)
        {
            return TransportType.ToString().Equals(TransportName?.Trim()?.ToLower() ?? string.Empty);
        }
    }
}
