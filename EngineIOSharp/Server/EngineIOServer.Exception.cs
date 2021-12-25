using EngineIOSharp.Common;
using System;

namespace EngineIOSharp.Server
{
    partial class EngineIOServer
    {
        internal static class Exceptions
        {
            public static readonly EngineIOException UNKNOWN_TRANSPORT = new EngineIOException("Unknown transport");
            public static readonly EngineIOException UNKNOWN_SID = new EngineIOException("Unknown sid");
            public static readonly EngineIOException BAD_HANDSHAKE_METHOD = new EngineIOException("Bad handshake method");
            public static readonly EngineIOException BAD_REQUEST = new EngineIOException("Bad request");
            public static readonly EngineIOException FORBIDDEN = new EngineIOException("Forbidden");
            public static readonly EngineIOException UNSUPPORTED_PROTOCOL_VERSION = new EngineIOException("Unsupported protocol version");

            private static readonly EngineIOException[] VALUES = new EngineIOException[]
            {
                UNKNOWN_TRANSPORT,
                UNKNOWN_SID,
                BAD_HANDSHAKE_METHOD,
                BAD_REQUEST,
                FORBIDDEN,
                UNSUPPORTED_PROTOCOL_VERSION,
            };

            public static bool Contains(EngineIOException Exception)
            {
                return Array.Exists(VALUES, Element => Element.Equals(Exception));
            }

            public static int IndexOf(EngineIOException Exception)
            {
                return Array.IndexOf(VALUES, Exception as EngineIOException);
            }
        }
    }
}
