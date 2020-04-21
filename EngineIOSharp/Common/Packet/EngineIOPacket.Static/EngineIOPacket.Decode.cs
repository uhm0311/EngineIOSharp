using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using WebSocketSharp;

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
            string Temp = string.Empty;

            try
            {
                List<EngineIOPacket> Result = new List<EngineIOPacket>();

                if (Response != null && Response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader Reader = new StreamReader(Response.GetResponseStream()))
                    {
                        string Content = Reader.ReadToEnd();
                        Temp = Content;

                        if (Content.Contains(':'))
                        {
                            string Buffer;
                            int Size;

                            while (Content.Length > 0)
                            {
                                Buffer = string.Empty;
                                Size = 0;

                                for (int i = 0; i < Content.Length; i++)
                                {
                                    if (Content[i] != ':')
                                    {
                                        Buffer += Content[i];
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                Size = int.Parse(Buffer);
                                Content = Content.Substring(Buffer.Length + 1);
                                Buffer = string.Empty;

                                for (int i = 0; i < Size; i++)
                                {
                                    Buffer += Content[i];
                                }

                                Content = Content.Substring(Buffer.Length);

                                if (Buffer.StartsWith("b"))
                                {
                                    List<byte> RawBuffer = new List<byte>() { byte.Parse(Buffer[1].ToString()) };
                                    Buffer = Buffer.Substring(2);

                                    RawBuffer.AddRange(Convert.FromBase64String(Buffer));
                                    Result.Add(Decode(RawBuffer.ToArray()));
                                }
                                else
                                {
                                    Result.Add(Decode(Buffer));
                                }
                            }
                        }
                    }
                }

                return Result.ToArray();
            } 
            catch (Exception Exception)
            {
                EngineIOLogger.Error("Packet decoding failed. " + Temp, Exception);

                return new EngineIOPacket[] { CreateErrorPacket(Exception) };
            }
        }

        internal static EngineIOPacket Decode(MessageEventArgs EventArgs)
        {
            return EventArgs.IsText ? Decode(EventArgs.Data) : (EventArgs.IsBinary ? Decode(EventArgs.RawData) : null);
        }
    }
}
