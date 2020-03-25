using EngineIOSharp.Common.Type.Abstract;

namespace EngineIOSharp.Common.Type
{
    internal class EngineIOErrorType : EngineIOType<EngineIOErrorType, int>
    {
        private EngineIOErrorType(int Data) : base(Data) { }

        public static readonly EngineIOErrorType UNKNOWN_TRANSPORT = new EngineIOErrorType(0);
        public static readonly EngineIOErrorType UNKNOWN_SID = new EngineIOErrorType(1);
        public static readonly EngineIOErrorType BAD_HANDSHAKE_METHOD = new EngineIOErrorType(2);
        public static readonly EngineIOErrorType BAD_REQUEST = new EngineIOErrorType(3);
        public static readonly EngineIOErrorType FORBIDDEN = new EngineIOErrorType(4);

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
