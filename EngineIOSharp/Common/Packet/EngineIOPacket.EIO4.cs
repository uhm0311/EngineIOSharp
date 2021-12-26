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
            try
            {
                if (IsText || IsBinary)
                {
                    if (!ForceBinary && (IsText || ForceBase64))
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
