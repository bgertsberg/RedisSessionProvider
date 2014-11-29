using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Web;

namespace RedisSessionProvider.Config
{
    public static class RedisConnectionConfig
    {
        static RedisConnectionConfig()
        {
            RedisConnectionConfig.MaxSessionByteSize = 30000;
            RedisConnectionConfig.RedisSessionSizeExceededHandler = RedisConnectionConfig.ClearRedisItems;
        }

        /// <summary>
        /// A delegate for returning a Tuple of the connection name (for setups with multiple connections),
        ///     a database index (for redis servers with multiple databases), and a StackExchange.Redis
        ///     ConfigurationOptions object. This delegate takes precedence over GetSERedisServerConfig
        /// </summary>
        public static Func<HttpContextBase, Tuple<string, int, ConfigurationOptions>> GetSERedisServerConfigDbIndex = null;

        /// <summary>
        /// A delegate for returning a StackExchange.Redis.ConfigurationOptions instance which will dictate
        ///     to the StackExchange.Redis client what Redis instance to connect to for persisting session data. 
        ///     Please assign a string key to your connection as well, in case you want to connect to multiple
        ///     sets of Redis instances.
        /// </summary>
        public static Func<HttpContextBase, KeyValuePair<string, ConfigurationOptions>> GetSERedisServerConfig = null;

        /// <summary>
        /// Gets or sets a logging delegate that takes as input the server ip and port of the connection used as
        ///     a string and the number of total redis messages to it as a long
        /// </summary>
        public static Action<string, long> LogConnectionActionsCountDel { get; set; }
        
        /// <summary>
        /// Gets or sets a function to call every time data is pulled from Redis, where the first
        ///     parameter is the connection name and the second parameter is the size in bytes
        ///     of the data retrieved.
        /// </summary>
        public static Action<string, int> LogRedisSessionSize { get; set; }

        /// <summary>
        /// Gets or sets the delegate that handles when the Session goes over the max allowed size. Defaults
        ///     to clearing the items.
        /// </summary>
        public static Action<RedisSessionStateItemCollection, int> RedisSessionSizeExceededHandler { get; set; }

        private static void ClearRedisItems(RedisSessionStateItemCollection items, int size)
        {
            items.Clear();
        }

        /// <summary>
        /// Gets or sets the maximum supported session size, in bytes. Defaults to 30000, or 30kb
        /// </summary>
        public static int MaxSessionByteSize { get; set; }

    }
}
