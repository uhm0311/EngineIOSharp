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
                    Type = (EngineIOPacketType)byte.Parse(Convert.ToChar(BufferQueue.Dequeue()).ToString()),
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
            string Temp = string.Empty;

            try
            {
                if (IsBinary)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    using (StreamReader Reader = new StreamReader(Stream))
                    {
                        string Content = Reader.ReadToEnd();
                        Temp = Content;

                        if (Content.Contains(':'))
                        {
                            StringBuilder Buffer = new StringBuilder();
                            int Size;

                            while (Content.Length > 0)
                            {
                                Buffer.Clear();
                                Size = 0;

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
                                    List<byte> RawBuffer = new List<byte>() { Convert.ToByte(Data[1]) };
                                    Data = Data.Substring(2);

                                    RawBuffer.AddRange(Convert.FromBase64String(Data));
                                    Result.Add(Decode(RawBuffer.ToArray()));
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
            return EventArgs.IsText ? Decode(EventArgs.Data) : (EventArgs.IsBinary ? Decode(EventArgs.RawData) : null);
        }
    }
}
