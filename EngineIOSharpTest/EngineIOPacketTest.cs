using EngineIOSharp.Common.Enum.Internal;
using EngineIOSharp.Common.Packet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EngineIOSharp.Test
{
    public static class ArrayExtension
    {
        public static T[] Concat<T>(this T[] x, T[] y)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }
            else if (y == null)
            {
                throw new ArgumentNullException("y");
            }
            else
            {
                List<T> Buffer = new List<T>(x.Length + y.Length);
                Buffer.AddRange(x);
                Buffer.AddRange(y);

                return Buffer.ToArray();
            }
        }
    }

    [TestClass]
    public class EngineIOPacketTest
    {
        private readonly string Data = "¢æ¢æ¢æ¢æ¢æ¢æ";
        private readonly byte[] RawData = { 1, 2, 4, 5, 5 };

        [TestMethod]
        public void TestStringEncode()
        {
            EngineIOPacket Packet = EngineIOPacket.CreateMessagePacket(Data);
            object EncodedPacketEIO3 = Packet.Encode(EngineIOTransportType.polling, false, Protocol: 3);
            object EncodedPacketEIO4 = Packet.Encode(EngineIOTransportType.polling, false, Protocol: 4);

            Assert.AreEqual(EncodedPacketEIO3, string.Format("{0}:{1}{2}", Data.Length + 1, (int)EngineIOPacketType.MESSAGE, Data));
            Assert.AreEqual(EncodedPacketEIO4, string.Format("{0}{1}", (int)EngineIOPacketType.MESSAGE, Data));
        }

        [TestMethod]
        public void TestBase64Encode()
        {
            string Base64Data = Convert.ToBase64String(RawData);

            EngineIOPacket Packet = EngineIOPacket.CreateMessagePacket(RawData);

            object EncodedPacketPollingEIO3 = Packet.Encode(EngineIOTransportType.polling, true, false, 3);
            object EncodedPacketWebSocketEIO3 = Packet.Encode(EngineIOTransportType.websocket, true, false, 3);

            object EncodedPacketPollingEIO4 = Packet.Encode(EngineIOTransportType.polling, true, false, 4);
            object EncodedPacketWebSocketEIO4 = Packet.Encode(EngineIOTransportType.websocket, true, false, 4);

            Assert.AreEqual(EncodedPacketPollingEIO3, string.Format("{0}:b{1}{2}", Base64Data.Length + 2, (int)EngineIOPacketType.MESSAGE, Base64Data));
            Assert.AreEqual(EncodedPacketWebSocketEIO3, string.Format("b{0}{1}", (int)EngineIOPacketType.MESSAGE, Base64Data));

            Assert.AreEqual(EncodedPacketPollingEIO4, string.Format("b{0}", Base64Data));
            Assert.AreEqual(EncodedPacketWebSocketEIO4, string.Format("b{0}", Base64Data));
        }

        [TestMethod]
        public void TestByteEncode()
        {
            EngineIOPacket Packet = EngineIOPacket.CreateMessagePacket(RawData);

            object EncodedPacketPollingEIO3 = Packet.Encode(EngineIOTransportType.polling, false, true, 3);
            object EncodedPacketWebSocketEIO3 = Packet.Encode(EngineIOTransportType.websocket, false, true, 3);

            object EncodedPacketPollingEIO4 = Packet.Encode(EngineIOTransportType.polling, false, true, 4);
            object EncodedPacketWebSocketEIO4 = Packet.Encode(EngineIOTransportType.websocket, false, true, 4);

            Assert.IsTrue(EncodedPacketPollingEIO3 is byte[]);
            Assert.IsTrue(EncodedPacketWebSocketEIO3 is byte[]);
            Assert.IsTrue(EncodedPacketPollingEIO4 is byte[]);
            Assert.IsTrue(EncodedPacketWebSocketEIO4 is byte[]);

            Assert.IsTrue(Enumerable.SequenceEqual(EncodedPacketPollingEIO3 as byte[], new byte[] { 0x01, (byte)(RawData.Length + 1), 0xff, (byte)EngineIOPacketType.MESSAGE }.Concat(RawData)));
            Assert.IsTrue(Enumerable.SequenceEqual(EncodedPacketWebSocketEIO3 as byte[], new byte[] { (byte)EngineIOPacketType.MESSAGE }.Concat(RawData)));
            Assert.IsTrue(Enumerable.SequenceEqual(EncodedPacketPollingEIO4 as byte[], new byte[] { (byte)EngineIOPacketType.MESSAGE }.Concat(RawData)));
            Assert.IsTrue(Enumerable.SequenceEqual(EncodedPacketWebSocketEIO4 as byte[], new byte[] { (byte)EngineIOPacketType.MESSAGE }.Concat(RawData)));
        }

        [TestMethod]
        public void TestStringDecode()
        {
            EngineIOPacket Packet = EngineIOPacket.Decode(string.Format("{0}{1}", (int)EngineIOPacketType.MESSAGE, Data));

            Assert.AreEqual(Packet.Data, Data);
        }

        [TestMethod]
        public void TestBase64Decode()
        {
            string Base64Data = Convert.ToBase64String(RawData);

            EngineIOPacket PacketEIO3 = EngineIOPacket.DecodeBase64String(string.Format("b{0}{1}", (int)EngineIOPacketType.MESSAGE, Base64Data), 3);
            EngineIOPacket PacketEIO4 = EngineIOPacket.DecodeBase64String(string.Format("b{0}", Base64Data), 4);

            Assert.IsTrue(Enumerable.SequenceEqual(PacketEIO3.RawData, RawData));
            Assert.IsTrue(Enumerable.SequenceEqual(PacketEIO4.RawData, RawData));
        }

        [TestMethod]
        public void TestByteDecode()
        {
            EngineIOPacket Packet = EngineIOPacket.Decode(new byte[] { (byte)EngineIOPacketType.MESSAGE }.Concat(RawData));

            Assert.IsTrue(Enumerable.SequenceEqual(Packet.RawData, RawData));
        }

        [TestMethod]
        public void TestStreamDecode1()
        {
            byte[] UTF8Data = Encoding.UTF8.GetBytes(Data);
            List<byte> RawBuffer = new List<byte>();

            {
                RawBuffer.Add(0x00);
                RawBuffer.Add((byte)(UTF8Data.Length + 1));
                RawBuffer.Add(0xff);
                RawBuffer.Add('0' + (byte)EngineIOPacketType.MESSAGE);
                RawBuffer.AddRange(UTF8Data);
            }

            {
                RawBuffer.Add(0x01);
                RawBuffer.Add((byte)(RawData.Length + 1));
                RawBuffer.Add(0xff);
                RawBuffer.Add((byte)EngineIOPacketType.MESSAGE);
                RawBuffer.AddRange(RawData);
            }

            MemoryStream Stream = new MemoryStream(RawBuffer.ToArray());
            EngineIOPacket[] Packets = EngineIOPacket.Decode(Stream, true, 3);

            Assert.IsNotNull(Packets);
            Assert.IsTrue(Packets.Length == 2);

            Assert.AreEqual(Packets[0].Data, Data);
            Assert.IsTrue(Enumerable.SequenceEqual(Packets[1].RawData, RawData));
        }

        [TestMethod]
        public void TestStreamDecode2()
        {
            MemoryStream Stream = new MemoryStream(Encoding.UTF8.GetBytes("6:4hello2:4¢æ"));
            EngineIOPacket[] Packets = EngineIOPacket.Decode(Stream, false, 3);

            Assert.IsNotNull(Packets);
            Assert.IsTrue(Packets.Length == 2);

            Assert.AreEqual(Packets[0].Data, "hello");
            Assert.AreEqual(Packets[1].Data, "¢æ");
        }

        [TestMethod]
        public void TestStreamDecode3()
        {
            StringBuilder Buffer = new StringBuilder();
            string Base64Data = Convert.ToBase64String(RawData);

            {
                Buffer.Append((int)EngineIOPacketType.MESSAGE);
                Buffer.Append(Data);
            }
            Buffer.Append(EngineIOPacket.Seperator);
            {
                Buffer.Append('b');
                Buffer.Append(Base64Data);
            }

            MemoryStream Stream = new MemoryStream(Encoding.UTF8.GetBytes(Buffer.ToString()));
            EngineIOPacket[] Packets = EngineIOPacket.Decode(Stream, false, 4);

            Assert.IsNotNull(Packets);
            Assert.IsTrue(Packets.Length == 2);

            Assert.AreEqual(Packets[0].Data, Data);
            Assert.IsTrue(Enumerable.SequenceEqual(Packets[1].RawData, RawData));
        }

        [TestMethod]
        public void TestStreamDecode4()
        {
            MemoryStream Stream = new MemoryStream(Encoding.UTF8.GetBytes("4hello\u001e4¢æ"));
            EngineIOPacket[] Packets = EngineIOPacket.Decode(Stream, false, 4);

            Assert.IsNotNull(Packets);
            Assert.IsTrue(Packets.Length == 2);

            Assert.AreEqual(Packets[0].Data, "hello");
            Assert.AreEqual(Packets[1].Data, "¢æ");
        }
    }
}
