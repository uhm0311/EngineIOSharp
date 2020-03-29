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

        internal object Encode()
        {
            try
            {
                if (IsText)
                {
                    StringBuilder Builder = new StringBuilder();
                    Builder.Append((int)Type);
                    Builder.Append(Data);

                    return Builder.ToString();
                }
                else if (IsBinary)
                {
                    List<byte> RawData = new List<byte>() { (byte)Type };
                    RawData.AddRange(RawData);

                    return RawData.ToArray();
                }
                else
                {
                    throw new EngineIOException("Packet encoding failed. " + this);
                }
            }
            catch (Exception Exception)
            {
                throw new EngineIOException("Packet encoding failed. " + this, Exception);
            }
        }
    }
}
