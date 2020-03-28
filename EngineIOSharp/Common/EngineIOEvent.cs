namespace EngineIOSharp.Common
{
    public static class EngineIOEvent
    {
        public static readonly string OPEN = "open";
        public static readonly string ERROR = "error";
        public static readonly string CLOSE = "close";

        public static readonly string PACKET = "packet";
        public static readonly string MESSAGE = "message";

        public static readonly string PACKET_CREATE = "packetCreate";
        public static readonly string FLUSH = "flush";
        public static readonly string DRAIN = "drain";

        public static readonly string UPGRADE = "upgrade";
        public static readonly string UPGRADE_ERROR = "upgradeError";

        public static readonly string REQUEST_HEADERS = "requestHeaders";
        public static readonly string RESPONSE_HEADERS = "responseHeaders";
    }
}
