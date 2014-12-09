using StackExchange.Redis;
using System;
using System.Web;

namespace RedisSessionProvider.Config
{

    /// <summary>
    /// This class contains settings for how the classes that hold the Session behave, after data
    ///     is retrieved from Redis but before it is written back.
    /// </summary>
    public static class RedisSessionConfig
    {
        
        /// <summary>
        /// A delegate that is called when RedisSessionProvider.RedisSessionStateStoreProvider encounters
        ///     an error during the retrieval or setting of a Session
        /// </summary>
        public static Action<Exception> SessionExceptionLoggingDel { get; set; }
        public static void LogSessionException(Exception ex)
        {
            if (SessionExceptionLoggingDel != null)
                SessionExceptionLoggingDel(ex);

            System.Diagnostics.Trace.TraceError(ex.ToString());
        }

        /// <summary>
        /// A delegate that is fired when RedisSessionAccessor encounters an exception getting a Session
        /// </summary>
        public static Action<HttpContextBase, string, Exception> RedisSessionAccessorExceptionLoggingDel { get; set; }

        /// <summary>
        /// A delegate that returns a Redis keyname given an HttpContext and the Session Id cookie value.
        ///     If this is null, the Session Id value will be used directly as the Redis keyname. This may
        ///     be fine if your Redis server is specifically used only for web Sessions within one app.
        /// </summary>
        public static Func<HttpContextBase, string, string> RedisKeyFromSessionIdDel { get; set; }

        /// <summary>
        /// A delegate called whenever RedisSessionProvider or RedisSessionAccessor writes to a field 
        /// in the session's Redis hash. The delegate is given the current context, 
        /// the StackExchange.Redis.HashEntry[] values to be changed, and the redis hash key as parameters
        /// </summary>
        public static Action<HttpContextBase, HashEntry[], string> RedisWriteFieldDel { get; set; }

        /// <summary>
        /// A delegate called whenever RedisSessionProvider or RedisSessionAccessor removes a field
        /// from the session's Redis hash. The delegate is riven the current context, the
        /// StackExchange.Redis.RedisValue[] keys to be deleted, and the redis hash key as parameters
        /// </summary>
        public static Action<HttpContextBase, RedisValue[], string> RedisRemoveFieldDel { get; set; }

        /// <summary>
        /// Gets or sets the expected number of threads that will simultaneously try to access a session,
        ///     defaults to 1
        /// </summary>
        public static int SessionAccessConcurrencyLevel { get; set; }

        /// <summary>
        /// Gets or sets the cookie name that stores the ASP.NET session ID
        /// </summary>
        public static string SessionHttpCookieName { get; set; }

        /// <summary>
        /// Gets or sets the session expiration duration
        /// </summary>
        public static TimeSpan SessionTimeout { get; set; }
    }
}
