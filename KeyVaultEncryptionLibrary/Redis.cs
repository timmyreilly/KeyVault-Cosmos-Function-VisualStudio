using System;
using StackExchange.Redis; 

namespace KeyVaultEncryptionLibrary
{

    /// <summary>
    /// 
    /// Interface with a Redis Client
    /// 
    /// </summary>

    public class Redis
    {
        private static string _redisConnectionString; 

        public Redis(string redisConnectionString)
        {
            _redisConnectionString = redisConnectionString; 
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(_redisConnectionString + ",allowAdmin = true");
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public void PutInRedis(string key, string value)
        {
            // will place key value pair in redis if not already there. 
            IDatabase cache = Connection.GetDatabase();
            cache.StringSet(key, value);
        }

        public string GetValue(string key)
        {
            IDatabase cache = Connection.GetDatabase();
            string value = cache.StringGet(key);
            return value;
        }

        public bool FlushRedis()
        {
            try
            {
                var ep = Connection.GetEndPoints();

                // get the target server
                var server = Connection.GetServer(ep[0]); 

                // completely wipe ALL keys from database 0 
                server.FlushDatabase();

                return true; 
            }
            catch (Exception ex)
            {
                return false; 
            }
        }

    }
}
