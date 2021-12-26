using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        private static EngineIOPacket[] DecodeEIO3(Stream Stream, bool IsBinary)
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
                                    Result.Add(DecodeBase64String(Data, 3));
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

        private static byte[] ConvertBase64StringToRawBufferEIO3(string Data)
        {
            List<byte> RawBuffer = new List<byte>() { byte.Parse(Data[1].ToString()) };
            RawBuffer.AddRange(Convert.FromBase64String(Data.Substring(2)));

            return RawBuffer.ToArray();
        }
    }
}
