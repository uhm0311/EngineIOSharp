using System;

namespace EngineIOSharp.Common
{
    public class EngineIOException : Exception
    {
        internal EngineIOException(string message) : base(message) { }

        internal EngineIOException(string message, Exception innerException) : base(message, innerException) { }

        public override bool Equals(object obj)
        {
            return ToString().Equals(obj?.ToString() ?? string.Empty);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
