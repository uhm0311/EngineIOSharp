using EngineIOSharp.Client.Transport;
using EngineIOSharp.Common.Enum.Internal;
using System;
using System.Collections.Generic;
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

        internal object Encode(EngineIOTransportType TransportType, bool ForceBase64, bool ForceBinary = false)
        {
            if (ForceBase64 && ForceBinary)
            {
                throw new ArgumentException("ForceBase64 && ForceBinary cannot be true.", "ForceBase64, ForceBinary");
            }

            try
            {
                if (IsText || IsBinary)
                {
                    if ((TransportType == EngineIOTransportType.polling) || (!ForceBinary && (IsText || ForceBase64)))
                    {
                        StringBuilder Builder = new StringBuilder();
                        Builder.Append(IsText ? ((int)Type).ToString() : "b");
                        Builder.Append(IsText ? Data : Convert.ToBase64String(RawData));

                        return Builder.ToString();
                    }
                    else
                    {
                        List<byte> Buffer = new List<byte>() { (byte)Type };
                        Buffer.AddRange(RawData);

                        return Buffer.ToArray();
                    }
                }

                throw new EngineIOException("Packet encoding failed. " + this);
            }
            catch (Exception Exception)
            {
                return CreateErrorPacket(Exception);
            }
        }
    }
}
