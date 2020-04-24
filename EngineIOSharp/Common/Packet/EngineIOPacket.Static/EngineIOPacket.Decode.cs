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
                    return Decode(Response.GetResponseStream(), Response.ContentType.Equals("application/octet-stream"));
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
                return Decode(Request.InputStream, Request.ContentType.Equals("application/octet-stream"));
            }

            return new EngineIOPacket[0];
        }

        private static EngineIOPacket[] Decode(Stream Stream, bool IsBinary)
        {
            List<EngineIOPacket> Result = new List<EngineIOPacket>();
            object Temp = string.Empty;

            try
            {
                if (IsBinary)
                {
                    using (MemoryStream MemoryStream = new MemoryStream())
                    {
                        Stream.CopyTo(MemoryStream);
                        Queue<byte> BufferQueue = new Queue<byte>(MemoryStream.ToArray());

                        if (BufferQueue.Contains(0xff))
                        {
                            while (BufferQueue.Count > 0)
                            {
                                List<byte> RawBuffer = new List<byte>();
                                bool IsText = BufferQueue.Dequeue() == 0;

                                StringBuilder Buffer = new StringBuilder();
                                int Size = 0;

                                while (BufferQueue.Count > 0)
                                {
                                    byte TempSize = BufferQueue.Dequeue();

                                    if (TempSize < 0xff)
                                    {
                                        Buffer.Append(TempSize);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                Size = int.Parse(Buffer.ToString());
                                Buffer.Clear();

                                for (int i = 0; i < Size; i++)
                                {
                                    RawBuffer.Add(BufferQueue.Dequeue());
                                }

                                if (IsText)
                                {
                                    Result.Add(Decode(Encoding.UTF8.GetString(RawBuffer.ToArray())));
                                }
                                else
                                {
                                    Result.Add(Decode(RawBuffer.ToArray()));
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (StreamReader Reader = new StreamReader(Stream))
                    {
                        string Content = (Temp = Reader.ReadToEnd()).ToString();

                        if (Content.Contains(':'))
                        {
                            while (Content.Length > 0)
                            {
                                StringBuilder Buffer = new StringBuilder();
                                int Size = 0;

                                for (int i = 0; i < Content.Length; i++)
                                {
                                    if (Content[i] != ':')
                                    {
                                        Buffer.Append(Content[i]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                Size = int.Parse(Buffer.ToString());
                                Content = Content.Substring(Buffer.Length + 1);
                                Buffer.Clear();

                                for (int i = 0; i < Size; i++)
                                {
                                    Buffer.Append(Content[i]);
                                }

                                Content = Content.Substring(Buffer.Length);
                                string Data = Buffer.ToString();

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
            List<byte> RawBuffer = new List<byte>() { byte.Parse(Data[1].ToString()) };

            RawBuffer.AddRange(Convert.FromBase64String(Data.Substring(2)));
            return Decode(RawBuffer.ToArray());
        }
    }
}
