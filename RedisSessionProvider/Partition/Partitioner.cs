using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisSessionProvider
{
    public interface IPartitioner
    {
        string GetNode(byte[] key);
    }

    public class KetamaPartitioner : IPartitioner
    {
        private Dictionary<UInt32, string> ring = new Dictionary<uint, string>();
        private UInt32[] tokens;
        private Func<byte[], uint> HashFunc;

        public KetamaPartitioner(IEnumerable<string> nodes, Func<byte[], uint> hashFunc)
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
            var hash = HashFunc(key);
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
                    end = half;
                else
                    start = half;
            }
            return start;
        }
    }

    public class VNodePartitioner : IPartitioner
    {
        private readonly string[] nodes;
        private readonly Func<byte[], uint> hashFunc;
        private uint size;
        private uint replication;

        public VNodePartitioner(IEnumerable<string> nodes, Func<byte[], uint> hashFunc)
        {
            this.nodes = nodes.ToArray();
            replication = 10;
            size = uint.MaxValue / (replication * (uint)this.nodes.Length) + 1;
            this.hashFunc = hashFunc;
        }

        public string GetNode(byte[] key)
        {
            var hash = hashFunc(key);
            return nodes[(hash / size) % replication];
        }
    }
}
