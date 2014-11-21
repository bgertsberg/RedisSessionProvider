
using NUnit.Framework;
using RedisSessionProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisSessionProviderUnitTests
{
    [TestFixture]
    public class HashTests
    {
        private static byte[] GetBytes(string s)
        {
            return Encoding.ASCII.GetBytes(s);
        }
        [Test]
        public void Murmur2Tests()
        {
            Assert.AreEqual(2864375483U, MurmurHash2.Hash(GetBytes("asdf")));
            Assert.AreEqual(1594468574U, MurmurHash2.Hash(GetBytes("abcde")));
            Assert.AreEqual(1271458169U, MurmurHash2.Hash(GetBytes("abcdef")));
            Assert.AreEqual(4188131059U, MurmurHash2.Hash(GetBytes("abcdefg")));
            Assert.AreEqual(1685739664U, MurmurHash2.Hash(GetBytes("hello world!")));
        }

        [Test]
        public void Murmur3Tests()
        {
            Assert.AreEqual(455139366U, MurMurHash3.Hash(GetBytes("asdf")));
            Assert.AreEqual(3902511862U, MurMurHash3.Hash(GetBytes("abcde")));
            Assert.AreEqual(1635893381U, MurMurHash3.Hash(GetBytes("abcdef")));
            Assert.AreEqual(2285673222U, MurMurHash3.Hash(GetBytes("abcdefg")));
            Assert.AreEqual(774705101U, MurMurHash3.Hash(GetBytes("hello world!")));
        }

        //[Test]
        //public void Murmur2Timing()
        //{
        //    uint total;
        //    for (int i = 0; i < 2000000; i++)
        //        total = MurmurHash2.Hash(GetBytes("hello world!"));
        //}
        //[Test]
        //public void Murmur3Timing()
        //{
        //    uint total;
        //    for (int i = 0; i < 2000000; i++)
        //        total = MurMurHash3.Hash(GetBytes("hello world!"));
        //}
    }
}
