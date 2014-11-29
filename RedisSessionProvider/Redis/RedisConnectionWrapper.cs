using RedisSessionProvider.Config;
using RedisSessionProvider.Partition;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Timers;

namespace RedisSessionProvider.Redis
{
    public sealed class RedisConnectionWrapper
    {
        private static Dictionary<string, ConnectionMultiplexer> RedisConnections =new Dictionary<string, ConnectionMultiplexer>();
        private static Dictionary<string, long> RedisStats =new Dictionary<string, long>();
        
        private static System.Timers.Timer connMessagesSentTimer;
        
        private static KetamaPartitioner partitioner;

        static RedisConnectionWrapper()
        {
            connMessagesSentTimer = new System.Timers.Timer(30000);
            connMessagesSentTimer.Elapsed += RedisConnectionWrapper.GetConnectionsMessagesSent;
            connMessagesSentTimer.Start();
        }

        
        /// <summary>
        /// The index of Database to store session.
        /// </summary>
        public int DatabaseIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the RedisConnectionWrapper class, which contains methods for accessing
        ///     a static concurrentdictionary of already created and open RedisConnection instances
        /// </summary>
        /// <param name="serverAddress">The ip address of the redis instance</param>
        /// <param name="serverPort">The port number of the redis instance</param>
        public RedisConnectionWrapper(string srvAddr, int srvPort)
            : this(GetConnectionID(srvAddr, srvPort), ConfigurationOptions.Parse(srvAddr + ":" + srvPort))
        {
        }

        private static string GetConnectionID(string srvAddr, int srvPort)
        {
            return string.Format("{0}_%_{1}", srvAddr, srvPort);
        }
        
        /// <summary>
        /// Initializes a new instance of the RedisConnectionWrapper class, which contains methods for accessing
        ///     a static concurrentdictionary of already created and open redisconnection instances
        /// </summary>
        /// <param name="redisParams">A configuration class containing the redis server hostname and port number</param>
        public RedisConnectionWrapper(RedisConnectionParameters redisParams)
            : this(GetConnectionID(redisParams.ServerAddress, redisParams.ServerPort), redisParams.DatabaseIndex, redisParams.TranslateToConfigOpts())
        {
        }

        

        /// <summary>
        /// Initializes a new instance of the RedisConnectionWrapper class, which contains methods for accessing
        ///     a static concurrentdictionary of already created and open redisconnection instances
        /// </summary>
        /// <param name="connIdentifier">Because it is possible to have connections to multiple redis instances, we store
        /// a dictionary of them to reuse. This parameter is used as the key to that dictionary.</param>
        /// <param name="connOpts">A StackExchange.Redis configuration class containing the redis connection info</param>
        public RedisConnectionWrapper(string connIdentifier, ConfigurationOptions connOpts)
            : this(connIdentifier, 0, connOpts)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RedisConnectionWrapper class, which contains methods for accessing
        ///     a static concurrentdictionary of already created and open redisconnection instances
        /// </summary>
        /// <param name="connIdentifier">Because it is possible to have connections to multiple redis instances, we store
        /// a dictionary of them to reuse. This parameter is used as the key to that dictionary.</param>
        /// <param name="dbIndex">The index of the redis database with session information</param>
        /// <param name="connOpts">A StackExchange.Redis configuration class containing the redis connection info</param>
        public RedisConnectionWrapper(string connIdentifier, int dbIndex, ConfigurationOptions connOpts)
            : this(new KeyValuePair<string, ConfigurationOptions>[] { new KeyValuePair<string, ConfigurationOptions>(connIdentifier, connOpts) }, dbIndex)
        {
        }

        public RedisConnectionWrapper(IEnumerable<KeyValuePair<string,ConfigurationOptions>> connOpts, int dbIndex=0)
        {
            if (connOpts == null)
                throw new ConfigurationErrorsException("RedisConnectionWrapper cannot be initialized with null ConfigurationOptions property");

            var list=connOpts.ToArray();

            partitioner = new KetamaPartitioner(list.Select(kv => kv.Key), data => MurmurHash2.Hash(data));
            foreach (var kv in list)
                RedisConnectionWrapper.RedisConnections.Add(kv.Key, ConnectionMultiplexer.Connect(kv.Value));

            this.DatabaseIndex = dbIndex;
            
        }

        /// <summary>
        /// Method that returns a StackExchange.Redis.IDatabase object with ip and port number matching
        ///     what was passed into the constructor for this instance of RedisConnectionWrapper
        /// </summary>
        /// <returns>An open and callable RedisConnection object, shared with other threads in this
        /// application domain that also called for a connection to the specified ip and port</returns>
        public IDatabase GetConnection(string redisKey)
        {
            return RedisConnectionWrapper.RedisConnections[GetConnectionId(redisKey)].GetDatabase(this.DatabaseIndex);
        }

        public static string GetConnectionId(string redisKey)
        {
            return partitioner.GetNode(Encoding.UTF8.GetBytes(redisKey));
        }
        
        /// <summary>
        /// Gets the number of redis commands sent and received, and sets the count to 0 so the next time
        ///     we will not see double counts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void GetConnectionsMessagesSent(object sender, ElapsedEventArgs e)
        {
            if (RedisConnectionConfig.LogConnectionActionsCountDel != null)
                foreach (string connName in RedisConnectionWrapper.RedisConnections.Keys.ToList())
                    try
                    {
                        LogConnectionStatistics(connName);
                    }
                    catch (Exception)
                    {
                    }
         }

        private static void LogConnectionStatistics(string connName)
        {
            ConnectionMultiplexer conn;
            if (RedisConnectionWrapper.RedisConnections.TryGetValue(connName, out conn))
            {
                long priorPeriodCount = RedisConnectionWrapper.RedisStats.ContainsKey(connName) ? RedisConnectionWrapper.RedisStats[connName] : 0;

                long curCount = conn.GetCounters().Interactive.OperationCount;

                // log the sent commands
                RedisConnectionConfig.LogConnectionActionsCountDel(connName, curCount - priorPeriodCount);

                RedisConnectionWrapper.RedisStats[connName] = curCount;
            }
        }
    }
}