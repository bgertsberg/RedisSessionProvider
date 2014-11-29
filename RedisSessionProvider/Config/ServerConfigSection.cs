using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RedisSessionProvider.Config
{
    public class ServerConfigSection: ConfigurationSection
    {
        public static string Name
        {
            get { return "dataCacheClient"; }
        }

        [ConfigurationProperty("hosts",IsDefaultCollection=false,IsRequired=false)]
        [ConfigurationCollection(typeof(RedisHostCollection),AddItemName="host")]
        public RedisHostCollection Hosts
        {
            get { return (RedisHostCollection)this["hosts"]; }
            set { this["hosts"] = value; }
        }
    }

    public class RedisHostCollection: ConfigurationElementCollection
    {
        public static string Name { get { return "hosts"; } }

        protected override ConfigurationElement CreateNewElement()
        {
            return new RedisHostConfiguration();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return element.ToString();
        }
    }

    [Serializable]
    public class RedisHostConfiguration: ConfigurationElement, ISerializable
    {
        [ConfigurationProperty("name",IsRequired=true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("address", IsRequired = true)]
        public string ConnectionString
        {
            get { return (string)this["connectionString"]; }
            set { this["connectionString"] = value; }
        }


        public RedisHostConfiguration()
        {
        }

        public RedisHostConfiguration(SerializationInfo info, StreamingContext context)
        {
            this.Name = info.GetString("name");
            this.ConnectionString = info.GetString("connectionString");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", (object)Name);
            info.AddValue("connectionString", (object)ConnectionString);
        }

        public KeyValuePair<string, ConfigurationOptions> GetConnectionInformation()
        {
            return new KeyValuePair<string, ConfigurationOptions>(Name, ConfigurationOptions.Parse(ConnectionString));
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
