using RedisSessionProvider.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Web;
using SysTimer = System.Timers.Timer;

namespace RedisSessionProvider
{
    /// <summary>
    /// Class that keeps a count of the number of currently executing web requests that need 
    ///     a particular Session. When the count reaches 0, clears it from the shared in-memory
    ///     storage so that the next request goes back to Redis.
    /// </summary>
    public class LocalSharedSessionDictionary
    {
        private static ConcurrentDictionary<string, LocalSessionInfo> localCache = new ConcurrentDictionary<string, LocalSessionInfo>();
        private static SysTimer cacheFreshnessTimer;

        static LocalSharedSessionDictionary()
        {
            cacheFreshnessTimer = new SysTimer(TimeSpan.FromSeconds(5).TotalMilliseconds);
            cacheFreshnessTimer.Elapsed += EnsureLocalCacheFreshness;
            cacheFreshnessTimer.Start();
        }

        static void EnsureLocalCacheFreshness(object sender, ElapsedEventArgs e)
        {
            try
            {
                LocalSessionInfo removed;
                foreach (string expKey in GetExpiredKeys())
                    localCache.TryRemove(expKey, out removed);
            }
            catch (Exception sharedDictExc)
            {
                RedisSessionConfig.LogSessionException(sharedDictExc);
            }
        }

        private static List<string> GetExpiredKeys()
        {
            var expiredKeys = new List<string>();

            var expiredCutOff = DateTime.Now - RedisSessionConfig.SessionTimeout;

            foreach (var kv in localCache)
                if (kv.Value.LastAccess < expiredCutOff || kv.Value.IsNotCurrentlyUsed())
                    expiredKeys.Add(kv.Key);

            return expiredKeys;
        }

        /// <summary>
        /// Gets a session for a given redis ID, and increments the count of the number of requests
        ///     that have accessed this redis ID
        /// </summary>
        /// <param name="redisHashId">The id of the session in Redis</param>
        /// <param name="getDel">The delegate to run to fetch the session from Redis</param>
        /// <returns>A RedisSessionStateItemCollection for the session</returns>
 
        public RedisSessionStateItemCollection GetSessionItems(HttpContextBase context, string redisHashKey)
        {
            return localCache.AddOrUpdate(redisHashKey, redisKey => CreateNewLocalSessionInfo(context, redisKey), UpdateSessionItem).SessionData;
        }

        private static LocalSessionInfo CreateNewLocalSessionInfo(HttpContextBase context, string redisKey)
        {
            return new LocalSessionInfo(RedisSessionStateStoreProvider.GetItemFromRedis(redisKey, context, RedisSessionConfig.SessionTimeout));
        }

        private static LocalSessionInfo UpdateSessionItem(string redisKey, LocalSessionInfo existingItem)
        {
            existingItem.IncrementOutstandingRequest();
            existingItem.LastAccess = DateTime.Now;

            return existingItem;
        }

        /// <summary>
        /// Gets a Session collection, but decrements the number of requests that need it. When
        ///     the count gets to 0, the object is cleared from the local in-memory cache of
        ///     all Sessions so that the next request that needs a session will go to Redis for
        ///     the data
        /// </summary>
        /// <param name="redisHashId">The Id of the session in Redis</param>
        /// <returns>A RedisSessionStateItemCollection for the session</returns>
        public RedisSessionStateItemCollection GetSessionForEndRequest(string redisHashId)
        {
            LocalSessionInfo sessionInfo;
            if (localCache.TryGetValue(redisHashId, out sessionInfo))
            {
                // atomically decrease ref count, and check to see if any requests outstanding
                sessionInfo.DecrementOutstandingRequest();
                // the timer will clear it out within the next 5 seconds if the count goes to 0

                return sessionInfo.SessionData;
            }

            return null;
        }

        /// <summary>
        /// Internal class for holding a session item collection and the count of requests referecing it
        /// </summary>
        class LocalSessionInfo
        {
            /// <summary>
            /// Initializes a new instance of the SessionAndRefCount class with a given item collection
            /// </summary>
            /// <param name="itms">The items in a session</param>
            public LocalSessionInfo(RedisSessionStateItemCollection itms)
                : this(itms, 1)
            {
            }

            /// <summary>
            /// Initializes a new instance of the SessionAndRefCount class with a given item collection
            ///     and count of requests using it
            /// </summary>
            /// <param name="itms">The items in a session</param>
            /// <param name="count">The number of requests accessing this session</param>
            public LocalSessionInfo(RedisSessionStateItemCollection itms, int count)
            {
                this.SessionData = itms;
                this.outstandingRequests = count;
                this.LastAccess = DateTime.Now;
            }

            public void IncrementOutstandingRequest()
            {
                Interlocked.Increment(ref outstandingRequests);
            }

            public void DecrementOutstandingRequest()
            {
                Interlocked.Decrement(ref outstandingRequests);
            }

            /// <summary>
            /// Gets or sets the item collection
            /// </summary>
            public RedisSessionStateItemCollection SessionData { get; set; }

            /// <summary>
            /// The number of requests that have a reference to this session
            /// </summary>
            private int outstandingRequests;

            public bool IsNotCurrentlyUsed()
            {
                return outstandingRequests <= 0;
            }

            /// <summary>
            /// The last time this session was accessed.
            /// </summary>
            public DateTime LastAccess;
        }
    }
}
