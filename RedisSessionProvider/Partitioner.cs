using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RedisSessionProvider
{
    public class Partitioner
    {
        private Dictionary<UInt32, string> ring = new Dictionary<uint, string>();
        private UInt32[] tokens;
        private Func<byte[], uint> HashFunc;

        public Partitioner(IEnumerable<string> nodes, Func<byte[],uint> hashFunc)
        {
            HashFunc = hashFunc;
            foreach (var node in nodes)
                for (int i = 0; i < 10; i++)
                    ring.Add(HashFunc(Encoding.UTF8.GetBytes(string.Concat(node, "_", i.ToString()))), node);

            tokens = ring.Keys.ToArray();
            Array.Sort(tokens);
        }

        public string GetNode(byte[] key)
        {
            var hash=HashFunc(key);
            return ring[tokens[FindIndex(hash)]];
        }

        private int FindIndex(uint hash)
        {
            int start = 0;
            int end = tokens.Length - 1;
            if (hash < tokens[0] || hash >= tokens[tokens.Length - 1])
                return tokens.Length - 1;

            while (end - start > 1)
            {
                var half = (start + end) / 2;
                if (hash < half)
                    end=half;
                else
                    start=half;
            }
            return start;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct BytetoUInt32Converter
    {
        [FieldOffset(0)]
        public Byte[] Bytes;

        [FieldOffset(0)]
        public UInt32[] UInts;
    }


    public class MurmurHash2
    {
        public static UInt32 Hash(Byte[] data)
        {
            return Hash(data, 0xc58f1a7b);
        }
        const UInt32 m = 0x5bd1e995;
        const Int32 r = 24;

        public static UInt32 Hash(Byte[] data, UInt32 seed)
        {
            Int32 length = data.Length;
            if (length == 0)
                return 0;
            UInt32 h = seed ^ (UInt32)length;
            Int32 currentIndex = 0;
            // array will be length of Bytes but contains Uints
            // therefore the currentIndex will jump with +1 while length will jump with +4
            UInt32[] hackArray = new BytetoUInt32Converter { Bytes = data }.UInts;
            while (length >= 4)
            {
                UInt32 k = hackArray[currentIndex++];
                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;
                length -= 4;
            }
            currentIndex *= 4; // fix the length
            switch (length)
            {
                case 3:
                    h ^= (UInt16)(data[currentIndex++] | data[currentIndex++] << 8);
                    h ^= (UInt32)data[currentIndex] << 16;
                    h *= m;
                    break;
                case 2:
                    h ^= (UInt16)(data[currentIndex++] | data[currentIndex] << 8);
                    h *= m;
                    break;
                case 1:
                    h ^= data[currentIndex];
                    h *= m;
                    break;
                default:
                    break;
            }

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }
    }

    public static class MurMurHash3
    {
        //Change to suit your needs
        const uint seed = 144;

        public static uint Hash(byte[] data)
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            uint h1 = seed;
            uint k1 = 0;
            
            Int32 currentIndex = 0;
            UInt32[] hackArray = new BytetoUInt32Converter { Bytes = data }.UInts;
            Int32 length = data.Length;

            while (length>= 4)
            {
                /* Get four bytes from the input into an uint */
                k1 = hackArray[currentIndex++];

                /* bitmagic hash */
                k1 *= c1;
                k1 = rotl32(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = rotl32(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
                
                length -= 4;
            }
            currentIndex *= 4;
            
            switch (length)
            {
                case 3:
                    k1 = (uint) (data[currentIndex++] | data[currentIndex++] << 8 | data[currentIndex] << 16);
                    k1 *= c1;
                    k1 = rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
                case 2:
                    k1 = (uint) (data[currentIndex++] | data[currentIndex] << 8);
                    k1 *= c1;
                    k1 = rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
                case 1:
                    k1 = (uint)(data[currentIndex]);
                    k1 *= c1;
                    k1 = rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
            }

            // finalization, magic chants to wrap it all up
            h1 ^= (uint)currentIndex;
            return fmix(h1);
        }

        private static uint rotl32(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        private static uint fmix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
    }
}
