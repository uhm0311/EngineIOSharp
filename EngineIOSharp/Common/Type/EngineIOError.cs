using EngineIOSharp.Common.Type.Abstract;

namespace EngineIOSharp.Common.Type
{
    internal class EngineIOError : EngineIOType<EngineIOError, int>
    {
        private EngineIOError(int Data) : base(Data) { }

        public static readonly EngineIOError UNKNOWN_TRANSPORT = new EngineIOError(0);
        public static readonly EngineIOError UNKNOWN_SID = new EngineIOError(1);
        public static readonly EngineIOError BAD_HANDSHAKE_METHOD = new EngineIOError(2);
        public static readonly EngineIOError BAD_REQUEST = new EngineIOError(3);
        public static readonly EngineIOError FORBIDDEN = new EngineIOError(4);

        public override string ToString()
        {
            switch (Data)
            {
                case 0:
                    return "Transport unknown";

                case 1:
                    return "Session ID unknown";

                case 2:
                    return "Bad handshake method";

                case 3:
                    return "Bad request";

                default:
                    return "Forbidden";
            }
        }
    }
}
