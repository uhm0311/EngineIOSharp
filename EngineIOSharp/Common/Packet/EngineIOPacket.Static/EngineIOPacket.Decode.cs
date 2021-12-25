using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using WebSocketSharp;
using HttpListenerRequest = WebSocketSharp.Net.HttpListenerRequest;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        private static readonly string Seperator = "\u001e";

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

        internal static EngineIOPacket[] Decode(HttpWebResponse Response, int Protocol)
        {
            if (Response != null)
            {
                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    if (Protocol == 3)
                    {
                        return DecodeEIO3(Response.GetResponseStream(), Response.ContentType.Equals("application/octet-stream"));
                    }
                    else if (Protocol == 4)
                    {
                        return DecodeEIO4(Response.GetResponseStream());
                    }
                    else
                    {
                        throw CreateProtocolException(Protocol);
                    }
                }
                else
                {
                    return new EngineIOPacket[] { CreateErrorPacket() };
                }
            }

            return new EngineIOPacket[0];
        }

        internal static EngineIOPacket[] Decode(HttpListenerRequest Request, int Protocol)
        {
            if (Request != null)
            {
                if (Protocol == 3)
                {
                    return DecodeEIO3(Request.InputStream, Request.ContentType.Equals("application/octet-stream"));
                }
                else if (Protocol == 4)
                {
                    return DecodeEIO4(Request.InputStream);
                }
                else
                {
                    throw CreateProtocolException(Protocol);
                }
            }

            return new EngineIOPacket[0];
        }

        internal static EngineIOPacket Decode(MessageEventArgs EventArgs, int Protocol)
        {
            if (EventArgs.IsText)
            {
                string Data = EventArgs.Data;

                if (Data.StartsWith("b"))
                {
                    return DecodeBase64String(Data, Protocol);
                }
                else
                {
                    return Decode(Data);
                }
            }
            else if (EventArgs.IsBinary)
            {
                return Decode(EventArgs.RawData);
            }
            else
            {
                return CreateNoopPacket();
            }
        }

        private static EngineIOPacket DecodeBase64String(string Data, int Protocol)
        {
            return Decode(ConvertBase64StringToByteBuffer(Data, Protocol).ToArray());
        }

        private static List<byte> ConvertBase64StringToByteBuffer(string Data, int Protocol)
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
