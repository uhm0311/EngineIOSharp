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

        internal static EngineIOPacket[] Decode(HttpWebResponse Response)
        {
            if (Response != null)
            {
                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    return Decode(Response.GetResponseStream());
                }
                else
                {
                    return new EngineIOPacket[] { CreateErrorPacket() };
                }
            }

            return new EngineIOPacket[0];
        }

        internal static EngineIOPacket[] Decode(HttpListenerRequest Request)
        {
            if (Request != null)
            {
                return Decode(Request.InputStream);
            }

            return new EngineIOPacket[0];
        }

        private static readonly string Seperator = "\u001e";

        private static EngineIOPacket[] Decode(Stream Stream)
        {
            List<EngineIOPacket> Result = new List<EngineIOPacket>();
            object Temp = string.Empty;

            try
            {
                using (StreamReader Reader = new StreamReader(Stream))
                {
                    string Content = (Temp = Reader.ReadToEnd()).ToString();
                    Queue<string> Contents = new Queue<string>();

                    while (Content.Contains(Seperator))
                    {
                        Contents.Enqueue(Content.Substring(0, Content.IndexOf(Seperator)));
                        Content = Content.Substring(Content.IndexOf(Seperator) + Seperator.Length);
                    }

                    Contents.Enqueue(Content);

                    while (Contents.Count > 0)
                    {
                        string Data = Contents.Dequeue();

                        if (Data.StartsWith("b"))
                        {
                            Result.Add(DecodeBase64String(Data));
                        }
                        else
                        {
                            Result.Add(Decode(Data));
                        }
                    }
                }
            }
            catch (Exception Exception)
            {
                EngineIOLogger.Error("Packet decoding failed. " + Temp, Exception);

                Result.Add(CreateErrorPacket(Exception));
            }

            return Result.ToArray();
        }

        internal static EngineIOPacket Decode(MessageEventArgs EventArgs)
        {
            if (EventArgs.IsText)
            {
                string Data = EventArgs.Data;

                if (Data.StartsWith("b"))
                {
                    return DecodeBase64String(Data);
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

        private static EngineIOPacket DecodeBase64String(string Data)
        {
            List<byte> RawBuffer = new List<byte>() { (byte)EngineIOPacketType.MESSAGE };

            RawBuffer.AddRange(Convert.FromBase64String(Data.Substring(1)));
            return Decode(RawBuffer.ToArray());
        }
    }
}
