using System;
using System.Collections.Generic;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    public partial class EngineIOPacket
    {
        public EngineIOPacketType EnginePacketType { get; private set; }

        public bool IsText { get; private set; }
        public bool IsBinary { get; private set; }

        public string Data { get; private set; }
        public byte[] RawData { get; private set; }

        private EngineIOPacket()
        {
            EnginePacketType = EngineIOPacketType.UNKNOWN;

            IsText = false;
            IsBinary = false;

            Data = string.Empty;
            RawData = new byte[0];
        }

        public override string ToString()
        {
            StringBuilder Builder = new StringBuilder();
            Builder.Append(string.Format("Packet: EnginePacketType={0}", EnginePacketType));

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

        internal object Encode()
        {
            try
            {
                if (IsText)
                {
                    StringBuilder Builder = new StringBuilder();
                    Builder.Append((int)EnginePacketType);
                    Builder.Append(Data);

                    return Builder.ToString();
                }
                else if (IsBinary)
                {
                    List<byte> RawData = new List<byte>() { (byte)EnginePacketType };
                    RawData.AddRange(RawData);

                    return RawData.ToArray();
                }
                else
                {
                    throw new EngineIOException("Packet encoding failed. " + this);
                }
            }
            catch (Exception ex)
            {
                throw new EngineIOException("Packet encoding failed. " + this, ex);
            }
        }
    }
}
