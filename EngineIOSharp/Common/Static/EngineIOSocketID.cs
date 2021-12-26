using System;
using System.Linq;
using System.Security.Cryptography;

namespace EngineIOSharp.Common.Static
{
    /// <summary>
    /// C# implementaion of <see href="https://github.com/faeldt/base64id">base64id</see>
    /// </summary>
    internal static class EngineIOSocketID
    {
        private static readonly RNGCryptoServiceProvider Crypto = new RNGCryptoServiceProvider();

        private const int BUFFER_SIZE = 4096;
        private static byte[] BytesBuffer = new byte[BUFFER_SIZE];
        private static int BytesBufferIndex = -1;

        public static string Generate()
        {
            byte[] Result = new byte[15];
            GetRandomBytes(12).CopyTo(Result, 0);

            return Convert.ToBase64String(Result).Replace('/', '_').Replace('+', '-');
        }

        public static byte[] GetRandomBytes(int Bytes = 12)
        {
            byte[] Result = new byte[Bytes];

            if (Bytes <= BUFFER_SIZE)
            {
                int BytesInBuffer = BUFFER_SIZE / Bytes;
                double Threshold = BytesInBuffer * 0.85;

                if (BytesBufferIndex == BytesInBuffer)
                {
                    BytesBuffer = new byte[BUFFER_SIZE];
                    BytesBufferIndex = -1;
                }

                if (BytesBufferIndex == -1 || BytesBufferIndex > Threshold)
                {
                    Crypto.GetNonZeroBytes(BytesBuffer);
                    BytesBufferIndex = 0;
                }

                Result = BytesBuffer.Skip(Bytes * BytesBufferIndex).Take(Bytes).ToArray();
                BytesBufferIndex++;

                return Result;
            }

            Crypto.GetNonZeroBytes(Result);
            return Result;
        }
    }
}
