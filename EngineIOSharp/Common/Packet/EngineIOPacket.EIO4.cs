using EngineIOSharp.Common.Enum.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        private object EncodeEIO4(EngineIOTransportType TransportType, bool ForceBase64, bool ForceBinary = false)
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
