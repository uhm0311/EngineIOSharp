using System;

namespace EngineIOSharp.Common
{
    public class EngineIOException : Exception
    {
        public EngineIOException(string message) : base(message) { }

        public EngineIOException(string message, Exception innerException) : base(message, innerException) { }

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
