using EngineIOSharp.Common.Enum.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineIOSharp.Common.Packet
{
    partial class EngineIOPacket
    {
        private object EncodeEIO3(EngineIOTransportType TransportType, bool ForceBase64, bool ForceBinary = false)
        {
            try
            {
                if (IsText || IsBinary)
                {
                    if (TransportType == EngineIOTransportType.polling)
                    {
                        if (!ForceBinary && (IsText || ForceBase64))
                        {
                            StringBuilder Builder = new StringBuilder();
                            Builder.Append((int)Type);
                            Builder.Append(IsText ? Data : Convert.ToBase64String(RawData));

                            int Length = Builder.Length + (IsText ? 0 : 1);
                            Builder.Insert(0, string.Format("{0}:" + (IsText ? "" : "b"), Length));

                            return Builder.ToString();
                        }
                        else
                        {
                            List<byte> Buffer = new List<byte>() { (byte)(IsText ? 0 : 1) };
                            byte[] RawData = IsText ? Encoding.UTF8.GetBytes(Data) : this.RawData;

                            foreach (char Character in (RawData.Length + 1).ToString())
                            {
                                Buffer.Add(byte.Parse(Character.ToString()));
                            }

                            Buffer.Add(0xff);

                            if (IsText)
                            {
                                Buffer.Add(Convert.ToByte((char)(Type + 48)));
                            }
                            else
                            {
                                Buffer.Add((byte)Type);
                            }

                            Buffer.AddRange(RawData);
                            return Buffer.ToArray();
                        }
                    }
                    else
                    {
                        if (!ForceBinary && (IsText || ForceBase64))
                        {
                            StringBuilder Builder = new StringBuilder();
                            Builder.Append((IsText ? "" : "b") + (int)Type);
                            Builder.Append(IsText ? Data : Convert.ToBase64String(RawData));

                            return Builder.ToString();
                        }
                        else
                        {
                            List<byte> Buffer = new List<byte>() { (byte)Type };
                            Buffer.AddRange(RawData);

                            return Buffer.ToArray();
                        }
                    }
                }

                throw new EngineIOException("Packet encoding failed. " + this);
            }
            catch (Exception Exception)
            {
                return CreateErrorPacket(Exception);
            }
        }
    }
}
