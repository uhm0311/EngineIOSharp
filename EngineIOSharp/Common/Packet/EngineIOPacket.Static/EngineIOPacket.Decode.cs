using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        internal static readonly string Seperator = "\u001e";

        internal static EngineIOPacket Decode(string Data)
        {
            try
            {
                EngineIOPacket Packet = new EngineIOPacket()
                {
                    Type = (EngineIOPacketType)Data[0] - '0',
                    IsText = true,
                    IsBinary = false,
                };

                if (Data.Length > 1)
                {
                    Packet.Data = Data.Substring(1);
                    Packet.RawData = Encoding.UTF8.GetBytes(Packet.Data);
                }

                return Packet;
            } 
            catch (Exception Exception)
            {
                EngineIOLogger.Error("Packet decoding failed. " + Data, Exception);

                return CreateErrorPacket(Exception);
            }
        }

        internal static EngineIOPacket Decode(byte[] RawData)
        {
            try
            {
                Queue<byte> BufferQueue = new Queue<byte>(RawData);
                EngineIOPacket Packet = new EngineIOPacket()
                {
                    Type = (EngineIOPacketType)BufferQueue.Dequeue(),
                    IsText = false,
                    IsBinary = true,
                };

                if (BufferQueue.Count > 0)
                {
                    Packet.RawData = BufferQueue.ToArray();
                    Packet.Data = BitConverter.ToString(Packet.RawData);
                }

                return Packet;
            }
            catch (Exception Exception)
            {
                EngineIOLogger.Error("Packet decoding failed. " + RawData != null ? BitConverter.ToString(RawData) : string.Empty, Exception);

                return CreateErrorPacket(Exception);
            }
        }

        internal static EngineIOPacket DecodeBase64String(string Data, int Protocol)
        {
            return Decode(ConvertBase64StringToByteBuffer(Data, Protocol));
        }

        internal static EngineIOPacket[] Decode(Stream Stream, bool IsBinary, int Protocol)
        {
            if (Protocol == 3)
            {
                return DecodeEIO3(Stream, IsBinary);
            }
            else if (Protocol == 4)
            {
                if (!IsBinary)
                {
                    return DecodeEIO4(Stream);
                }
                else
                {
                    throw new ArgumentException("IsBinary is true with Protocol 4.", "IsBinary, Protocol");
                }
            }
            else
            {
                throw CreateProtocolException(Protocol);
            }
        }

        private static byte[] ConvertBase64StringToByteBuffer(string Data, int Protocol)
        {
            if (Protocol == 3)
            {
                return ConvertBase64StringToRawBufferEIO3(Data);
            }
            else if (Protocol == 4)
            {
                return ConvertBase64StringToRawBufferEIO4(Data);
            }
            else
            {
                throw CreateProtocolException(Protocol);
            }
        }

        private static Exception CreateProtocolException(int Protocol)
        {
            return new ArgumentException(string.Format("Invalid Protocol {0}", Protocol), "Protocol");
        }
    }
}
