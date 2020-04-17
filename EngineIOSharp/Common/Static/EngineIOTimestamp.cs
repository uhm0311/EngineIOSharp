using SimpleThreadMonitor;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineIOSharp.Common.Static
{
    /// <summary>
    /// C# implementation of <see href="https://github.com/unshiftio/yeast">Yeast</see>.
    /// </summary>
    internal static class EngineIOTimestamp
    {
        private static readonly object GeneratorMutex = new object();

        private static readonly string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_";
        private static readonly Dictionary<char, int> AlphabetIndex = new Dictionary<char, int>();
        private static readonly int AlphabetLength = Alphabet.Length;

        private static string PreviousKey = string.Empty;
        private static int Seed = 0;

        public static string Encode(long Value)
        {
            StringBuilder Encoded = new StringBuilder();

            do
            {
                Encoded.Insert(0, Alphabet[(int)(Value % AlphabetLength)]);
                Value /= AlphabetLength;
            } while (Value > 0);

            return Encoded.ToString();
        }

        public static long Decode(string Value)
        {
            long Decoded = 0;

            for (int i = 0; i < Value.Length; i++)
            {
                Decoded *= AlphabetLength;
                Decoded += AlphabetIndex[Value[i]];
            }

            return Decoded;
        }

        public static string Generate()
        {
            string Key = null;

            SimpleMutex.Lock(GeneratorMutex, () =>
            {
                Key = Encode((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);

                if (!Key.Equals(PreviousKey))
                {
                    Seed = 0;
                    PreviousKey = Key;
                }
                else
                {
                    Key = string.Format("{0}.{1}", Key, Encode(Seed++));
                }
            });

            return Key;
        }

        static EngineIOTimestamp()
        {
            if (AlphabetIndex.Count == 0)
            {
                for (int i = 0; i < AlphabetLength; i++)
                {
                    AlphabetIndex[Alphabet[i]] = i;
                }
            }
        }
    }
}