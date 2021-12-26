using System;
using System.Collections.Generic;
using System.IO;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        private static EngineIOPacket[] DecodeEIO4(Stream Stream)
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
                            Result.Add(DecodeBase64String(Data, 4));
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

        private static byte[] ConvertBase64StringToRawBufferEIO4(string Data)
        {
            List<byte> RawBuffer = new List<byte>() { (byte)EngineIOPacketType.MESSAGE };
            RawBuffer.AddRange(Convert.FromBase64String(Data.Substring(1)));

            return RawBuffer.ToArray();
        }
    }
}
