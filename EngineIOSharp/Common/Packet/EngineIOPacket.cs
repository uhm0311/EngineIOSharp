using EngineIOSharp.Common.Enum.Internal;
using System;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    public partial class EngineIOPacket
    {
        public EngineIOPacketType Type { get; private set; }

        public bool IsText { get; private set; }
        public bool IsBinary { get; private set; }

        public string Data { get; private set; }
        public byte[] RawData { get; private set; }

        private EngineIOPacket()
        {
            Type = EngineIOPacketType.UNKNOWN;

            IsText = false;
            IsBinary = false;

            Data = string.Empty;
            RawData = new byte[0];
        }

        public override string ToString()
        {
            StringBuilder Builder = new StringBuilder();
            Builder.Append(string.Format("Packet: EnginePacketType={0}", Type));

            if (IsText)
            {
                Builder.Append(string.Format(", Data={0}", Data));
            }
            else if (IsBinary)
            {
                Builder.Append(string.Format(", RawData={0}", BitConverter.ToString(RawData)));
            }

            return Builder.ToString();
        }

        internal object Encode(EngineIOTransportType TransportType, bool ForceBase64, bool ForceBinary = false, int Protocol = 3)
        {
            if (ForceBase64 && ForceBinary)
            {
                throw new ArgumentException("ForceBase64 && ForceBinary cannot be true.", "ForceBase64, ForceBinary");
            }
            else if (Protocol == 3)
            {
                return EncodeEIO3(TransportType, ForceBase64, ForceBinary);
            }
            else if (Protocol == 4)
            {
                return EncodeEIO4(TransportType, ForceBase64, ForceBinary);
            }
            else
            {
                throw CreateProtocolException(Protocol);
            }
        }
    }
}
