using EngineIOSharp.Common.Enum.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        internal static EngineIOPacket CreateErrorPacket(Exception Exception = null)
        {
            return CreatePacket(EngineIOPacketType.UNKNOWN, Exception?.ToString() ?? string.Empty);
        }

        internal static EngineIOPacket CreateOpenPacket(string SocketID, ulong PingInterval, ulong PingTimeout, bool Upgrade)
        {
            JArray Upgrades = new JArray();

            if (Upgrade)
            {
                Upgrades.Add(EngineIOTransportType.websocket.ToString());
            }

            return CreatePacket(EngineIOPacketType.OPEN, new JObject()
            {
                ["sid"] = SocketID,
                ["pingInterval"] = PingInterval,
                ["pingTimeout"] = PingTimeout,
                ["upgrades"] = Upgrades
            }.ToString(Formatting.None));
        }

        internal static EngineIOPacket CreateClosePacket()
        {
            return CreatePacket(EngineIOPacketType.CLOSE);
        }

        internal static EngineIOPacket CreatePingPacket(string Data = null)
        {
            return CreatePacket(EngineIOPacketType.PING, Data ?? string.Empty);
        }

        internal static EngineIOPacket CreatePongPacket(string Data = null)
        {
            return CreatePacket(EngineIOPacketType.PONG, Data ?? string.Empty);
        }

        internal static EngineIOPacket CreateMessagePacket(string Data)
        {
            return CreatePacket(EngineIOPacketType.MESSAGE, Data);
        }

        internal static EngineIOPacket CreateMessagePacket(byte[] RawData)
        {
            return CreatePacket(EngineIOPacketType.MESSAGE, RawData);
        }

        internal static EngineIOPacket CreateUpgradePacket()
        {
            return CreatePacket(EngineIOPacketType.UPGRADE);
        }

        internal static EngineIOPacket CreateNoopPacket()
        {
            return CreatePacket(EngineIOPacketType.NOOP);
        }

        private static EngineIOPacket CreatePacket(EngineIOPacketType Type)
        {
            return new EngineIOPacket()
            {
                Type = Type,
                IsText = true,
            };
        }

        private static EngineIOPacket CreatePacket(EngineIOPacketType Type, string Data)
        {
            return new EngineIOPacket()
            {
                Type = Type,
                IsText = true,
                Data = Data,
                RawData = Encoding.UTF8.GetBytes(Data)
            };
        }

        private static EngineIOPacket CreatePacket(EngineIOPacketType Type, byte[] RawData)
        {
            return new EngineIOPacket()
            {
                Type = Type,
                IsBinary = true,
                Data = BitConverter.ToString(RawData),
                RawData = RawData
            };
        }
    }
}
