using System;
using System.Collections.Generic;
using System.Text;

namespace EngineIOSharp.Common
{
    /// <summary>
    /// C# implementation of <see href="https://github.com/unshiftio/yeast">Yeast</see>.
    /// </summary>
    internal static class Yeast
    {
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

        public static string Key()
        {
            string Key = Encode((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);

            if (!Key.Equals(PreviousKey))
            {
                Seed = 0;

                return PreviousKey = Key;
            }
            else
            {
                return string.Format("{0}.{1}", Key, Encode(Seed++));
            }
        }

        static Yeast()
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
