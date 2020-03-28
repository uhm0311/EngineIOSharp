using System;

namespace EngineIOSharp.Common
{
    public class EngineIOException : Exception
    {
        internal EngineIOException(string message) : base(message) { }

        internal EngineIOException(string message, Exception innerException) : base(message, innerException) { }
    }
}
