using System;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        internal static EngineIOPacket CreatePingPacket()
        {
            return new EngineIOPacket()
            {
                EnginePacketType = EngineIOPacketType.PING
            };
        }

        internal static EngineIOPacket CreatePongPacket()
        {
            return new EngineIOPacket()
            {
                EnginePacketType = EngineIOPacketType.PONG
            };
        }

        internal static EngineIOPacket CreateErrorPacket(string Data)
        {
            return new EngineIOPacket()
            {
                IsText = true,
                Data = Data
            };
        }

        internal static EngineIOPacket CreateMessagePacket(string Data)
        {
            return new EngineIOPacket()
            {
                EnginePacketType = EngineIOPacketType.MESSAGE,
                IsText = true,
                IsBinary = false,
                Data = Data,
                RawData = Encoding.UTF8.GetBytes(Data)
            };
        }

        internal static EngineIOPacket CreateMessagePacket(byte[] RawData)
        {
            return new EngineIOPacket()
            {
                EnginePacketType = EngineIOPacketType.MESSAGE,
                IsText = false,
                IsBinary = true,
                Data = BitConverter.ToString(RawData),
                RawData = RawData
            };
        }
    }
}
